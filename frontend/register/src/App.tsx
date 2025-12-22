import { useState, useEffect, useRef, useMemo } from 'react'
import {
  Card,
  CardContent,
  CardDescription,
  CardTitle,
} from '@/components/ui/card'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { CheckCircle2, AlertTriangle } from 'lucide-react'
import { RegistrationNotice } from '@/components/RegistrationNotice'
import { QRCodeSelection } from '@/components/QRCodeSelection'
import { RegistrationForm } from '@/components/RegistrationForm'

const API_BASE_URL = '/api'

interface QRCodeResponse {
  message: string
  transactionId: string
  qrcodeImage: string
  authUri: string
}

interface RegistrationResult {
  message: string
  [key: string]: unknown
}

function App() {
  const [transactionId, setTransactionId] = useState<string>('')
  const [qrcodeImage, setQrcodeImage] = useState<string>('')
  const [authUri, setAuthUri] = useState<string>('')
  const [registrationResult, setRegistrationResult] =
    useState<RegistrationResult | null>(null)
  const [error, setError] = useState<string>('')
  const [qrcodeExpiry, setQrcodeExpiry] = useState<number>(0) // QRCode 有效期倒計時（秒）
  const [email, setEmail] = useState<string>('')
  const [loadingEmail, setLoadingEmail] = useState<boolean>(false)
  const [tokenExpired, setTokenExpired] = useState<boolean>(false) // Token 是否已過期
  const pollingIntervalRef = useRef<number | null>(null)
  const qrcodeExpiryIntervalRef = useRef<number | null>(null)

  // 從 URL 參數獲取 token
  const token = useMemo(() => {
    const urlParams = new URLSearchParams(window.location.search)
    return urlParams.get('token') || ''
  }, [])

  // 從 token 取得 email
  useEffect(() => {
    // 如果沒有 token，設置為已過期
    if (!token) {
      setTokenExpired(true)
      setLoadingEmail(false)
      return
    }

    let cancelled = false

    const fetchEmail = async () => {
      setLoadingEmail(true)
      
      try {
        const response = await fetch(`${API_BASE_URL}/Register/info?token=${encodeURIComponent(token)}`)
        
        if (cancelled) {
          return
        }

        if (response.ok) {
          let data: { email?: string } = {}
          try {
            data = await response.json()
          } catch {
            setError('伺服器回應格式錯誤')
            return
          }
          if (data.email) {
            setEmail(data.email)
            setTokenExpired(false)
          } else {
            setError('無法取得註冊資訊')
          }
        } else {
          let errorData: { error?: string } = {}
          try {
            errorData = await response.json()
          } catch {
            // 回應不是 JSON 格式，忽略解析錯誤
          }
          // 檢查是否為 token 不存在或已過期的錯誤
          if (response.status === 404 && errorData.error === 'token 不存在或已過期') {
            setTokenExpired(true)
            setError('')
          } else {
            setError(errorData.error || '無法取得註冊資訊')
            setTokenExpired(false)
          }
        }
      } catch (err) {
        if (!cancelled) {
          console.error('取得註冊資訊錯誤:', err)
          setError('取得註冊資訊時發生錯誤')
        }
      } finally {
        if (!cancelled) {
          setLoadingEmail(false)
        }
      }
    }

    fetchEmail()

    return () => {
      cancelled = true
    }
  }, [token])

  // 處理註冊表單錯誤
  const handleRegistrationError = (errorMsg: string) => {
    // 檢查是否為 token 不存在或已過期的錯誤
    if (errorMsg === 'token 不存在或已過期') {
      setTokenExpired(true)
      setError('')
    } else {
      setError(errorMsg)
      setTokenExpired(false)
    }
  }

  // 處理註冊表單提交成功
  const handleRegistrationSubmit = (data: QRCodeResponse) => {
    setQrcodeImage(data.qrcodeImage)
    setTransactionId(data.transactionId)
    setAuthUri(data.authUri)
    setQrcodeExpiry(300) // 設置 5 分鐘有效期
    setError('')
  }

  // QRCode 有效期倒計時
  useEffect(() => {
    if (!qrcodeImage) {
      return
    }

    // 設定目標時間：當前時間 + 300秒（5分鐘）
    // 使用 Date.now() 計算，避免瀏覽器休眠導致倒數暫停
    const endTime = Date.now() + 300 * 1000

    if (qrcodeExpiryIntervalRef.current) {
      clearInterval(qrcodeExpiryIntervalRef.current)
    }

    qrcodeExpiryIntervalRef.current = setInterval(() => {
      const remaining = Math.ceil((endTime - Date.now()) / 1000)

      if (remaining <= 0) {
        setQrcodeExpiry(0)
        if (qrcodeExpiryIntervalRef.current) {
          clearInterval(qrcodeExpiryIntervalRef.current)
          qrcodeExpiryIntervalRef.current = null
        }
      } else {
        setQrcodeExpiry(remaining)
      }
    }, 1000)

    return () => {
      if (qrcodeExpiryIntervalRef.current) {
        clearInterval(qrcodeExpiryIntervalRef.current)
      }
    }
  }, [qrcodeImage])

  // 輪詢檢查註冊狀態 (GET /register/result)
  useEffect(() => {
    if (!transactionId || registrationResult) {
      return
    }

    const checkStatus = async () => {
      try {
        const response = await fetch(
          `${API_BASE_URL}/register/result?transactionId=${transactionId}`
        )
        if (response.ok) {
          let data: RegistrationResult
          try {
            data = await response.json()
          } catch {
            console.error('檢查註冊狀態失敗: 伺服器回應格式錯誤')
            return
          }
          // 如果是 "Waiting for registration" 訊息，繼續輪詢
          if (data.message === 'Waiting for registration') {
            console.log('等待註冊:', data.message)
            return
          }
          // 否則設置結果並停止輪詢
          setRegistrationResult(data)
          // 停止輪詢
          if (pollingIntervalRef.current) {
            clearInterval(pollingIntervalRef.current)
            pollingIntervalRef.current = null
          }
        } else if (response.status === 400) {
          // 還在等待驗證，繼續輪詢
          let data: { message?: string } = {}
          try {
            data = await response.json()
          } catch {
            // 回應不是 JSON 格式，忽略
          }
          console.log('等待註冊:', data.message)
        } else {
          let errorData: { error?: string } = {}
          try {
            errorData = await response.json()
          } catch {
            // 回應不是 JSON 格式，忽略
          }
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

  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-background to-muted/20 py-8 px-4">
      <div className="mx-auto max-w-2xl space-y-6">
        <div className="text-center space-y-2">
          <h1 className="text-4xl font-bold tracking-tight">訪客註冊</h1>
        </div>

        {tokenExpired && (
          <Card>
            <CardContent className="pt-6">
              <Alert variant="destructive" className="border-2">
                <AlertTriangle className="h-5 w-5" />
                <AlertTitle className="text-lg font-semibold">連結已過期</AlertTitle>
                <AlertDescription className="mt-2 text-base">
                  此註冊連結已過期或不存在，請聯繫主辦單位取得新的註冊連結。
                </AlertDescription>
              </Alert>
            </CardContent>
          </Card>
        )}

        {error && !tokenExpired && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertTitle>錯誤</AlertTitle>
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {!qrcodeImage && !registrationResult && !tokenExpired && (
          <>
            <RegistrationNotice />
            {loadingEmail ? (
              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center justify-center py-8">
                    <div className="text-center space-y-2">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div>
                      <p className="text-sm text-muted-foreground">載入中...</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ) : (
              <RegistrationForm
                onSubmit={handleRegistrationSubmit}
                onError={handleRegistrationError}
                initialEmail={email}
                token={token}
              />
            )}
          </>
        )}

        {qrcodeImage && !registrationResult && (
          <QRCodeSelection
            qrcodeImage={qrcodeImage}
            qrcodeExpiry={qrcodeExpiry}
            authUri={authUri}
          />
        )}

        {registrationResult && (
          <Card>
            <CardContent className="pt-6">
              <div className="flex flex-col items-center justify-center space-y-4 py-8">
                <div className="rounded-full bg-green-100 dark:bg-green-900/20 p-4">
                  <CheckCircle2 className="h-12 w-12 text-green-600 dark:text-green-400" />
                </div>
                <div className="text-center space-y-2">
                  <CardTitle className="text-2xl text-green-600 dark:text-green-400">
                    註冊成功！
                  </CardTitle>
                  <CardDescription className="text-base">
                    您的註冊已完成，期待您的到來！
                  </CardDescription>
                </div>
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  )
}

export default App
