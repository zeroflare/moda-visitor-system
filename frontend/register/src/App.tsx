import { useState, useEffect, useRef } from 'react'
import './App.css'

const API_BASE_URL = 'https://vistor.zeroflare.tw/api'

interface RegistrationForm {
  name: string
  email: string
  phone: string
  company: string
  otp: string
}

interface QRCodeResponse {
  message: string
  transactionId: string
  qrcodeImage: string
  authUri: string
}

interface RegistrationResult {
  message: string
  [key: string]: any
}

function App() {
  const [formData, setFormData] = useState<RegistrationForm>({
    name: '',
    email: '',
    phone: '',
    company: '',
    otp: '',
  })
  const [transactionId, setTransactionId] = useState<string>('')
  const [qrcodeImage, setQrcodeImage] = useState<string>('')
  const [registrationResult, setRegistrationResult] = useState<RegistrationResult | null>(null)
  const [loading, setLoading] = useState<boolean>(false)
  const [sendingOTP, setSendingOTP] = useState<boolean>(false)
  const [error, setError] = useState<string>('')
  const [otpSent, setOtpSent] = useState<boolean>(false)
  const [otpCooldown, setOtpCooldown] = useState<number>(0) // OTP 發送冷卻時間（秒）
  const [otpExpiry, setOtpExpiry] = useState<number>(0) // OTP 有效期倒計時（秒）
  const pollingIntervalRef = useRef<number | null>(null)
  const cooldownIntervalRef = useRef<number | null>(null)
  const expiryIntervalRef = useRef<number | null>(null)

  // 發送 OTP (POST /register/otp)
  const sendOTP = async () => {
    if (!formData.email) {
      setError('請輸入電子信箱')
      return
    }

    if (otpCooldown > 0) {
      setError(`請稍後再試，每分鐘僅能寄送一次驗證碼（剩餘 ${otpCooldown} 秒）`)
      return
    }

    setSendingOTP(true)
    setError('')

    try {
      const response = await fetch(`${API_BASE_URL}/register/otp`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email: formData.email }),
      })

      if (!response.ok) {
        const errorData = await response.json()
        if (response.status === 429) {
          // 冷卻期錯誤
          setOtpCooldown(60)
          throw new Error(errorData.error || '請稍後再試，每分鐘僅能寄送一次驗證碼')
        }
        throw new Error(errorData.error || '發送驗證碼失敗')
      }

      const data = await response.json()
      setOtpSent(true)
      setOtpCooldown(60) // 設置 60 秒冷卻期
      setOtpExpiry(600) // 設置 10 分鐘有效期
      setError('')
    } catch (err) {
      setError(err instanceof Error ? err.message : '發生錯誤')
    } finally {
      setSendingOTP(false)
    }
  }

  // 提交註冊表單 (POST /register/qrcode)
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    // 驗證必填欄位
    if (!formData.name || !formData.email || !formData.phone || !formData.company || !formData.otp) {
      setError('請填寫所有必填欄位')
      return
    }

    if (!otpSent) {
      setError('請先發送並輸入驗證碼')
      return
    }

    if (otpExpiry <= 0) {
      setError('驗證碼已過期，請重新發送')
      return
    }

    setLoading(true)
    setError('')

    try {
      const response = await fetch(`${API_BASE_URL}/register/qrcode`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: formData.name,
          email: formData.email,
          phone: formData.phone,
          company: formData.company,
          otp: formData.otp,
        }),
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || '註冊失敗')
      }

      const data: QRCodeResponse = await response.json()
      setQrcodeImage(data.qrcodeImage)
      setTransactionId(data.transactionId)
      setError('')
    } catch (err) {
      setError(err instanceof Error ? err.message : '發生錯誤')
    } finally {
      setLoading(false)
    }
  }

  // OTP 冷卻期倒計時
  useEffect(() => {
    if (otpCooldown <= 0) {
      if (cooldownIntervalRef.current) {
        clearInterval(cooldownIntervalRef.current)
        cooldownIntervalRef.current = null
      }
      return
    }

    cooldownIntervalRef.current = setInterval(() => {
      setOtpCooldown((prev) => {
        if (prev <= 1) {
          return 0
        }
        return prev - 1
      })
    }, 1000)

    return () => {
      if (cooldownIntervalRef.current) {
        clearInterval(cooldownIntervalRef.current)
      }
    }
  }, [otpCooldown])

  // OTP 有效期倒計時
  useEffect(() => {
    if (otpExpiry <= 0 || !otpSent) {
      if (expiryIntervalRef.current) {
        clearInterval(expiryIntervalRef.current)
        expiryIntervalRef.current = null
      }
      return
    }

    expiryIntervalRef.current = setInterval(() => {
      setOtpExpiry((prev) => {
        if (prev <= 1) {
          setOtpSent(false)
          return 0
        }
        return prev - 1
      })
    }, 1000)

    return () => {
      if (expiryIntervalRef.current) {
        clearInterval(expiryIntervalRef.current)
      }
    }
  }, [otpExpiry, otpSent])

  // 輪詢檢查註冊狀態 (GET /register/result)
  useEffect(() => {
    if (!transactionId || registrationResult) {
      return
    }

    const checkStatus = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/register/result?transactionId=${transactionId}`)
        if (response.ok) {
          const data: RegistrationResult = await response.json()
          setRegistrationResult(data)
          // 停止輪詢
          if (pollingIntervalRef.current) {
            clearInterval(pollingIntervalRef.current)
            pollingIntervalRef.current = null
          }
        } else if (response.status === 400) {
          // 還在等待驗證，繼續輪詢
          const data = await response.json()
          console.log('等待註冊:', data.message)
        } else {
          const errorData = await response.json()
          console.error('檢查註冊狀態失敗:', errorData)
        }
      } catch (err) {
        console.error('檢查註冊狀態錯誤:', err)
      }
    }

    // 立即檢查一次
    checkStatus()

    // 每 2 秒輪詢一次
    pollingIntervalRef.current = setInterval(checkStatus, 2000)

    return () => {
      if (pollingIntervalRef.current) {
        clearInterval(pollingIntervalRef.current)
      }
    }
  }, [transactionId, registrationResult])

  // 重置註冊狀態
  const resetRegistration = () => {
    // 停止所有定時器
    if (pollingIntervalRef.current) {
      clearInterval(pollingIntervalRef.current)
      pollingIntervalRef.current = null
    }
    if (cooldownIntervalRef.current) {
      clearInterval(cooldownIntervalRef.current)
      cooldownIntervalRef.current = null
    }
    if (expiryIntervalRef.current) {
      clearInterval(expiryIntervalRef.current)
      expiryIntervalRef.current = null
    }

    // 重置狀態
    setFormData({
      name: '',
      email: '',
      phone: '',
      company: '',
      otp: '',
    })
    setTransactionId('')
    setQrcodeImage('')
    setRegistrationResult(null)
    setError('')
    setOtpSent(false)
    setOtpCooldown(0)
    setOtpExpiry(0)
  }

  return (
    <div className="app-container">
      <h1>訪客註冊</h1>

      {loading && <p className="loading-message">載入中...</p>}

      {error && (
        <div className="error-message">
          <p>錯誤: {error}</p>
        </div>
      )}

      {!qrcodeImage && !registrationResult && (
        <form onSubmit={handleSubmit} className="registration-form">
          <div className="form-group">
            <label htmlFor="name">姓名 *</label>
            <input
              type="text"
              id="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="email">電子信箱 *</label>
            <div className="email-input-group">
              <input
                type="email"
                id="email"
                value={formData.email}
                onChange={(e) => {
                  setFormData({ ...formData, email: e.target.value })
                  setOtpSent(false)
                  setOtpExpiry(0)
                }}
                required
              />
              <button
                type="button"
                onClick={sendOTP}
                disabled={sendingOTP || !formData.email || otpCooldown > 0}
                className="otp-button"
              >
                {sendingOTP
                  ? '發送中...'
                  : otpCooldown > 0
                    ? `重新發送 (${otpCooldown}s)`
                    : '發送驗證碼'}
              </button>
            </div>
            {otpSent && (
              <p className="otp-sent-message">
                驗證碼已發送至您的信箱
                {otpExpiry > 0 && (
                  <span className="otp-expiry">
                    （有效期剩餘 {Math.floor(otpExpiry / 60)}:{(otpExpiry % 60).toString().padStart(2, '0')}）
                  </span>
                )}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="otp">驗證碼 *</label>
            <input
              type="text"
              id="otp"
              value={formData.otp}
              onChange={(e) => {
                const value = e.target.value.replace(/\D/g, '') // 只允許數字
                setFormData({ ...formData, otp: value })
              }}
              placeholder="請輸入 6 位數驗證碼"
              maxLength={6}
              required
              disabled={!otpSent}
            />
            {!otpSent && (
              <p className="form-hint">請先發送驗證碼至您的電子信箱</p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="phone">電話 *</label>
            <input
              type="tel"
              id="phone"
              value={formData.phone}
              onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="company">公司/單位 *</label>
            <input
              type="text"
              id="company"
              value={formData.company}
              onChange={(e) => setFormData({ ...formData, company: e.target.value })}
              required
            />
          </div>

          <button type="submit" className="submit-button" disabled={loading || !otpSent}>
            提交註冊
          </button>
        </form>
      )}

      {qrcodeImage && !registrationResult && (
        <div className="qrcode-section">
          <div className="qrcode-container">
            <div className="qrcode-left">
              <img src={qrcodeImage} alt="註冊 QRCode" />
              <p className="qrcode-instruction">請使用數位憑證皮夾 App 掃描此 QRCode 完成註冊</p>
            </div>
            <div className="qrcode-divider"></div>
            <div className="qrcode-right">
              <div className="reminder-section">
                <h3>註冊提醒與須知</h3>
                <div className="reminder-items">
                  <div className="reminder-item">
                    <p>請務必提前下載「數位憑證皮夾 App」，以便順利完成註冊手續。</p>
                  </div>
                  <div className="reminder-item">
                    <p>請使用數位憑證皮夾 App 掃描上方 QRCode 完成註冊。</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {registrationResult && (
        <div className="qrcode-section">
          <h2 className="result-title">註冊成功！</h2>
          <p className="success-message">您的註冊已完成，歡迎使用訪客系統。</p>
          <button className="back-button" onClick={resetRegistration}>
            返回註冊
          </button>
        </div>
      )}
    </div>
  )
}

export default App
