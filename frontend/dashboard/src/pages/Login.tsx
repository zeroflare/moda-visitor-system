import { useState, useRef, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { sendOTP, verifyOTP, getMe, type User } from '@/services/loginApi'

interface LoginProps {
  onLogin: (user: User) => void
}

export function Login({ onLogin }: LoginProps) {
  const [email, setEmail] = useState('')
  const [otp, setOtp] = useState('')
  const [otpSent, setOtpSent] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [isSendingOtp, setIsSendingOtp] = useState(false)
  const [countdown, setCountdown] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  // 清理計時器
  useEffect(() => {
    return () => {
      if (timerRef.current) {
        clearInterval(timerRef.current)
      }
    }
  }, [])

  const startCountdown = () => {
    // 清除舊的計時器
    if (timerRef.current) {
      clearInterval(timerRef.current)
    }
    
    setCountdown(60)
    timerRef.current = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          if (timerRef.current) {
            clearInterval(timerRef.current)
            timerRef.current = null
          }
          return 0
        }
        return prev - 1
      })
    }, 1000)
  }

  const handleSendOtp = async (e?: React.FormEvent) => {
    e?.preventDefault()
    if (!email) return

    setIsSendingOtp(true)
    setError(null)
    
    try {
      await sendOTP({ email })
      setOtpSent(true)
      startCountdown()
    } catch (err) {
      setError(err instanceof Error ? err.message : '發送 OTP 失敗，請稍後再試')
    } finally {
      setIsSendingOtp(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!otp) return

    setIsLoading(true)
    setError(null)
    
    try {
      // 先驗證 OTP
      await verifyOTP({ email, otp })
      // 驗證成功後取得使用者資料
      const user = await getMe()
      onLogin(user)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'OTP 驗證失敗，請檢查驗證碼是否正確')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-background via-background to-muted/20 p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-lg bg-primary/10">
            <img src={`${import.meta.env.BASE_URL}moda_logo.png`} alt="moda" className="h-12 w-12 object-contain" />
          </div>
          <CardTitle className="text-2xl font-bold">登入</CardTitle>
          <CardDescription>數位發展部訪客系統</CardDescription>
        </CardHeader>
        <CardContent>
          {error && (
            <div className="mb-4 rounded-md bg-destructive/15 p-3 text-sm text-destructive">
              {error}
            </div>
          )}
          {!otpSent ? (
            <form onSubmit={handleSendOtp} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="email">電子郵件</Label>
                <Input
                  id="email"
                  type="email"
                  placeholder="name@example.com"
                  value={email}
                  onChange={(e) => {
                    setEmail(e.target.value)
                    setError(null)
                  }}
                  required
                  disabled={isSendingOtp}
                />
              </div>
              <Button type="submit" className="w-full" disabled={isSendingOtp || !email}>
                {isSendingOtp ? '發送中...' : '發送驗證碼'}
              </Button>
            </form>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="email">電子郵件</Label>
                <Input
                  id="email"
                  type="email"
                  value={email}
                  disabled
                  className="bg-muted"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="otp">驗證碼</Label>
                <Input
                  id="otp"
                  type="text"
                  placeholder="請輸入 6 位數驗證碼"
                  value={otp}
                  onChange={(e) => {
                    setOtp(e.target.value.replace(/\D/g, '').slice(0, 6))
                    setError(null)
                  }}
                  required
                  disabled={isLoading}
                  maxLength={6}
                  className="text-center text-2xl tracking-widest"
                />
                <p className="text-sm text-muted-foreground">
                  驗證碼已發送至 {email}
                </p>
              </div>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  className="flex-1"
                  onClick={() => {
                    if (timerRef.current) {
                      clearInterval(timerRef.current)
                      timerRef.current = null
                    }
                    setOtpSent(false)
                    setOtp('')
                    setCountdown(0)
                    setError(null)
                  }}
                  disabled={isLoading}
                >
                  重新輸入
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="flex-1"
                  onClick={handleSendOtp}
                  disabled={isSendingOtp || countdown > 0 || isLoading}
                >
                  {countdown > 0 ? `重新發送 (${countdown}s)` : '重新發送'}
                </Button>
              </div>
              <Button type="submit" className="w-full" disabled={isLoading || otp.length !== 6}>
                {isLoading ? '驗證中...' : '登入'}
              </Button>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

