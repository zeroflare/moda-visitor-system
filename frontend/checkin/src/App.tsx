import { useState, useEffect, useRef, useCallback } from 'react'
import { v4 as uuidv4 } from 'uuid'
import './App.css'

const API_BASE_URL = 'https://vistor.zeroflare.tw/api'

interface QRCodeResponse {
  qrcodeImage: string
  authUri: string
}

interface CheckinResult {
  inviterEmail: string
  inviterName: string
  inviterDept: string
  inviterTitle: string
  vistorEmail: string
  vistorName: string
  vistorDept: string
  vistorPhone: string
  meetingTime: string
  meetingRoom: string
}

function App() {
  const [transactionId, setTransactionId] = useState<string>('')
  const [qrcodeImage, setQrcodeImage] = useState<string>('')
  const [checkinResult, setCheckinResult] = useState<CheckinResult | null>(null)
  const [loading, setLoading] = useState<boolean>(false)
  const [error, setError] = useState<string>('')
  const [countdown, setCountdown] = useState<number>(300) // 5分鐘 = 300秒
  const pollingIntervalRef = useRef<number | null>(null)
  const countdownIntervalRef = useRef<number | null>(null)

  // 生成 QRCode 的函數
  const generateQRCode = useCallback(async () => {
    const newTransactionId = uuidv4()
    setTransactionId(newTransactionId)
    setLoading(true)
    setError('')
    setCountdown(300) // 重置倒計時為 5 分鐘

    try {
      const response = await fetch(`${API_BASE_URL}/checkin/qrcode?transactionId=${newTransactionId}`)
      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.message || '取得 QRCode 失敗')
      }
      const data: QRCodeResponse = await response.json()
      setQrcodeImage(data.qrcodeImage)
    } catch (err) {
      setError(err instanceof Error ? err.message : '發生錯誤')
    } finally {
      setLoading(false)
    }
  }, [])

  // 初始化：生成 transactionId 並獲取 QRCode
  useEffect(() => {
    generateQRCode()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // 重置簽到狀態
  const resetCheckin = async () => {
    // 停止輪詢
    if (pollingIntervalRef.current) {
      clearInterval(pollingIntervalRef.current)
      pollingIntervalRef.current = null
    }

    // 重置狀態
    setCheckinResult(null)
    setQrcodeImage('')
    setError('')

    // 重新生成 QRCode
    await generateQRCode()
  }

  // 倒計時更新
  useEffect(() => {
    if (!qrcodeImage || checkinResult) {
      return
    }

    // 清除舊的定時器
    if (countdownIntervalRef.current) {
      clearInterval(countdownIntervalRef.current)
    }

    // 每秒更新倒計時
    countdownIntervalRef.current = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          // 倒計時結束，觸發 QRCode 刷新
          generateQRCode()
          return 300 // 重置為 5 分鐘
        }
        return prev - 1
      })
    }, 1000)

    return () => {
      if (countdownIntervalRef.current) {
        clearInterval(countdownIntervalRef.current)
      }
    }
  }, [qrcodeImage, checkinResult, generateQRCode])

  // 輪詢檢查簽到狀態
  useEffect(() => {
    if (!transactionId || checkinResult) {
      return
    }

    const checkStatus = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/checkin/result?transactionId=${transactionId}`)
        if (response.ok) {
          const data: CheckinResult = await response.json()
          setCheckinResult(data)
          // 停止輪詢
          if (pollingIntervalRef.current) {
            clearInterval(pollingIntervalRef.current)
            pollingIntervalRef.current = null
          }
        } else if (response.status === 400) {
          // 還在等待驗證，繼續輪詢
          const data = await response.json()
          console.log('等待驗證:', data.message)
        } else {
          throw new Error('檢查簽到狀態失敗')
        }
      } catch (err) {
        console.error('檢查簽到狀態錯誤:', err)
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
  }, [transactionId, checkinResult])

  return (
    <div className="app-container">
      <h1>訪客簽到</h1>
      
      {loading && <p className="loading-message">載入中...</p>}
      
      {error && (
        <div className="error-message">
          <p>錯誤: {error}</p>
        </div>
      )}

      {qrcodeImage && !checkinResult && (
        <div className="qrcode-section">
          <div className="qrcode-container">
            <div className="qrcode-left">
              <img src={qrcodeImage} alt="簽到 QRCode" />
              <div className="countdown-display">
                QRCode 將在 {Math.floor(countdown / 60)}:{(countdown % 60).toString().padStart(2, '0')} 後更新
              </div>
            </div>
            <div className="qrcode-divider"></div>
            <div className="qrcode-right">
              <div className="reminder-section">
                <h3>簽到提醒與須知</h3>
                <div className="reminder-items">
                  <div className="reminder-item">
                    <p>請務必提前下載「數位憑證皮夾 App」，以便順利完成現場簽到手續。</p>
                  </div>
                  <div className="reminder-item">
                    <p>簽到用的 QRCode 同一時間僅限一人掃描。請依序排隊，待前一位人員成功完成簽到後，下一位人員方可進行掃描。</p>
                  </div>
                  <div className="reminder-item">
                    <p>若您本次拜訪地點為「5 長室」，在完成簽到後，請主動與現場保全人員確認，由保全為您帶領前往。</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {checkinResult && (
        <div className="qrcode-section">
          <h2 className="result-title">簽到成功，歡迎您的蒞臨！</h2>
          <div className="result-info">
            <div className="info-item">
              <h3>邀請者資訊</h3>
              <p><strong>姓名:</strong> {checkinResult.inviterName}</p>
              <p><strong>Email:</strong> {checkinResult.inviterEmail}</p>
              <p><strong>單位:</strong> {checkinResult.inviterDept}</p>
              <p><strong>職稱:</strong> {checkinResult.inviterTitle}</p>
            </div>
            <div className="info-item">
              <h3>訪客資訊</h3>
              <p><strong>姓名:</strong> {checkinResult.vistorName}</p>
              <p><strong>Email:</strong> {checkinResult.vistorEmail}</p>
              <p><strong>單位:</strong> {checkinResult.vistorDept}</p>
              <p><strong>電話:</strong> {checkinResult.vistorPhone}</p>
            </div>
            <div className="info-item">
              <h3>會議資訊</h3>
              <p><strong>時間:</strong> {checkinResult.meetingTime}</p>
              <p><strong>地點:</strong> {checkinResult.meetingRoom}</p>
            </div>
          </div>
          <button className="back-button" onClick={resetCheckin}>
            返回簽到
          </button>
      </div>
      )}
      </div>
  )
}

export default App
