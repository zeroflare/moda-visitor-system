import { useState, useEffect, useRef } from 'react'
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
  FieldSet,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { CheckCircle2, Clock, Mail, Loader2 } from 'lucide-react'

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

interface RegistrationFormProps {
  onSubmit: (data: QRCodeResponse) => void
  onError?: (error: string) => void
}

export function RegistrationForm({ onSubmit, onError }: RegistrationFormProps) {
  const [formData, setFormData] = useState<RegistrationForm>({
    name: '',
    email: '',
    phone: '',
    company: '',
    otp: '',
  })
  const [loading, setLoading] = useState<boolean>(false)
  const [sendingOTP, setSendingOTP] = useState<boolean>(false)
  const [error, setError] = useState<string>('')
  const [otpSent, setOtpSent] = useState<boolean>(false)
  const [otpCooldown, setOtpCooldown] = useState<number>(0) // OTP 發送冷卻時間（秒）
  const [otpExpiry, setOtpExpiry] = useState<number>(0) // OTP 有效期倒計時（秒）
  const cooldownIntervalRef = useRef<number | null>(null)
  const expiryIntervalRef = useRef<number | null>(null)

  // 發送 OTP (POST /register/otp)
  const sendOTP = async () => {
    if (!formData.email) {
      const errorMsg = '請輸入電子信箱'
      setError(errorMsg)
      onError?.(errorMsg)
      return
    }

    if (otpCooldown > 0) {
      const errorMsg = `請稍後再試，每分鐘僅能寄送一次驗證碼（剩餘 ${otpCooldown} 秒）`
      setError(errorMsg)
      onError?.(errorMsg)
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
          throw new Error(
            errorData.error || '請稍後再試，每分鐘僅能寄送一次驗證碼'
          )
        }
        throw new Error(errorData.error || '發送驗證碼失敗')
      }

      await response.json()
      setOtpSent(true)
      setOtpCooldown(60) // 設置 60 秒冷卻期
      setOtpExpiry(600) // 設置 10 分鐘有效期
      setError('')
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : '發生錯誤'
      setError(errorMsg)
      onError?.(errorMsg)
    } finally {
      setSendingOTP(false)
    }
  }

  // 提交註冊表單 (POST /register/qrcode)
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    // 驗證必填欄位
    if (
      !formData.name ||
      !formData.email ||
      !formData.phone ||
      !formData.company ||
      !formData.otp
    ) {
      const errorMsg = '請填寫所有必填欄位'
      setError(errorMsg)
      onError?.(errorMsg)
      return
    }

    if (!otpSent) {
      const errorMsg = '請先發送並輸入驗證碼'
      setError(errorMsg)
      onError?.(errorMsg)
      return
    }

    if (otpExpiry <= 0) {
      const errorMsg = '驗證碼已過期，請重新發送'
      setError(errorMsg)
      onError?.(errorMsg)
      return
    }

    setLoading(true)
    setError('')

    // 調用真實 API
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
      setError('')
      onSubmit(data)
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : '發生錯誤'
      setError(errorMsg)
      onError?.(errorMsg)
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
      setOtpCooldown(prev => {
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
      setOtpExpiry(prev => {
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

  return (
    <Card>
      <CardHeader>
        <CardTitle>註冊資訊</CardTitle>
        <CardDescription>請填寫您的個人資訊以完成註冊</CardDescription>
      </CardHeader>
      <CardContent>
        {error && (
          <Alert variant="destructive" className="mb-6">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <form onSubmit={handleSubmit} className="space-y-6">
          <FieldSet>
            <FieldGroup>
              <Field>
                <FieldLabel htmlFor="name">姓名 *</FieldLabel>
                <Input
                  type="text"
                  id="name"
                  value={formData.name}
                  onChange={e =>
                    setFormData({ ...formData, name: e.target.value })
                  }
                  required
                />
              </Field>

              <Field>
                <FieldLabel htmlFor="email">電子信箱 *</FieldLabel>
                <div className="flex gap-2">
                  <Input
                    type="email"
                    id="email"
                    value={formData.email}
                    onChange={e => {
                      setFormData({ ...formData, email: e.target.value })
                      setOtpSent(false)
                      setOtpExpiry(0)
                    }}
                    className="flex-1"
                    required
                  />
                  <Button
                    type="button"
                    onClick={sendOTP}
                    disabled={sendingOTP || !formData.email || otpCooldown > 0}
                    className="whitespace-nowrap"
                    variant="outline"
                  >
                    {sendingOTP ? (
                      <>
                        <Loader2 className="h-4 w-4 animate-spin" />
                        發送中...
                      </>
                    ) : otpCooldown > 0 ? (
                      <>
                        <Clock className="h-4 w-4" />
                        重新發送 ({otpCooldown}s)
                      </>
                    ) : (
                      '驗證'
                    )}
                  </Button>
                </div>
                {otpSent && (
                  <div className="flex items-center gap-2 mt-2">
                    <Alert variant="success" className="py-2 px-3 flex-1">
                      <Mail className="h-4 w-4" />
                      <AlertDescription className="flex items-center gap-2">
                        <span>驗證碼已發送至您的信箱</span>
                        {otpExpiry > 0 && (
                          <Badge variant="secondary" className="ml-auto">
                            <Clock className="h-3 w-3 mr-1" />
                            {Math.floor(otpExpiry / 60)}:
                            {(otpExpiry % 60).toString().padStart(2, '0')}
                          </Badge>
                        )}
                      </AlertDescription>
                    </Alert>
                  </div>
                )}
              </Field>

              <Field>
                <FieldLabel htmlFor="otp">驗證碼 *</FieldLabel>
                <Input
                  type="text"
                  id="otp"
                  value={formData.otp}
                  onChange={e => {
                    const value = e.target.value.replace(/\D/g, '') // 只允許數字
                    setFormData({ ...formData, otp: value })
                  }}
                  placeholder="請輸入 6 位數驗證碼"
                  maxLength={6}
                  required
                  disabled={!otpSent}
                  aria-invalid={!otpSent && formData.otp.length > 0}
                />
                {!otpSent ? (
                  <FieldDescription>
                    請點擊驗證按鈕將驗證碼發送至您的電子信箱
                  </FieldDescription>
                ) : null}
              </Field>

              <Field>
                <FieldLabel htmlFor="phone">電話 *</FieldLabel>
                <Input
                  type="tel"
                  id="phone"
                  value={formData.phone}
                  onChange={e =>
                    setFormData({ ...formData, phone: e.target.value })
                  }
                  required
                />
              </Field>

              <Field>
                <FieldLabel htmlFor="company">公司/單位 *</FieldLabel>
                <Input
                  type="text"
                  id="company"
                  value={formData.company}
                  onChange={e =>
                    setFormData({ ...formData, company: e.target.value })
                  }
                  required
                />
              </Field>

              <Field>
                <Button
                  type="submit"
                  className="w-full"
                  disabled={loading || !otpSent}
                  size="lg"
                >
                  {loading ? (
                    <>
                      <Loader2 className="h-4 w-4 animate-spin" />
                      提交中...
                    </>
                  ) : (
                    <>
                      <CheckCircle2 className="h-4 w-4" />
                      提交註冊
                    </>
                  )}
                </Button>
              </Field>
            </FieldGroup>
          </FieldSet>
        </form>
      </CardContent>
    </Card>
  )
}
