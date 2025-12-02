import { useState, useEffect, useRef } from 'react'
import {
  Field,
  FieldDescription,
  FieldError,
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

const API_BASE_URL = '/api'

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
  initialEmail?: string
  token?: string
}

export function RegistrationForm({ 
  onSubmit, 
  onError,
  initialEmail = '',
  token = ''
}: RegistrationFormProps) {
  const [formData, setFormData] = useState<RegistrationForm>({
    name: '',
    email: initialEmail,
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
  const [fieldErrors, setFieldErrors] = useState<{
    email?: string
    phone?: string
  }>({})
  const cooldownIntervalRef = useRef<number | null>(null)
  const expiryIntervalRef = useRef<number | null>(null)

  // 當 initialEmail 改變時更新表單 email
  useEffect(() => {
    if (initialEmail) {
      setFormData(prev => ({ ...prev, email: initialEmail }))
    }
  }, [initialEmail])

  // Email 格式驗證
  const validateEmail = (email: string): boolean => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    return emailRegex.test(email)
  }

  // 手機號驗證：09 開頭，共十碼
  const validatePhone = (phone: string): boolean => {
    const phoneRegex = /^09\d{8}$/
    return phoneRegex.test(phone)
  }

  // 發送 OTP (POST /register/otp)
  const sendOTP = async () => {
    if (!formData.email) {
      const errorMsg = '請輸入電子信箱'
      setError(errorMsg)
      setFieldErrors({ ...fieldErrors, email: errorMsg })
      onError?.(errorMsg)
      return
    }

    if (!validateEmail(formData.email)) {
      const errorMsg = '請輸入有效的電子信箱格式'
      setError(errorMsg)
      setFieldErrors({ ...fieldErrors, email: errorMsg })
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
      setFieldErrors({ ...fieldErrors, email: undefined }) // 清除 email 錯誤
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

    // 清除之前的錯誤
    const newFieldErrors: { email?: string; phone?: string } = {}

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

    // 驗證 Email 格式
    if (!validateEmail(formData.email)) {
      newFieldErrors.email = '請輸入有效的電子信箱格式'
    }

    // 驗證手機號格式
    if (!validatePhone(formData.phone)) {
      newFieldErrors.phone = '請輸入有效的手機號碼（09 開頭，共十碼）'
    }

    // 如果有欄位驗證錯誤，顯示錯誤並返回
    if (Object.keys(newFieldErrors).length > 0) {
      setFieldErrors(newFieldErrors)
      const errorMsg = '請修正表單錯誤後再提交'
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
      const requestBody: {
        name: string
        email: string
        phone: string
        company: string
        otp: string
        token?: string
      } = {
        name: formData.name,
        email: formData.email,
        phone: formData.phone,
        company: formData.company,
        otp: formData.otp,
      }

      // 如果有 token，則添加到請求中
      if (token) {
        requestBody.token = token
      }

      const response = await fetch(`${API_BASE_URL}/Register/qrcode`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestBody),
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
                  placeholder="請輸入您的姓名"
                  maxLength={50}
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
                    className="flex-1"
                    placeholder="example@email.com"
                    required
                    disabled
                    aria-invalid={!!fieldErrors.email}
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
                {fieldErrors.email && (
                  <FieldError>{fieldErrors.email}</FieldError>
                )}
                {otpSent && (
                  <div className="mt-2">
                    <Alert 
                      variant="success" 
                      className="py-2 px-3 flex items-center gap-2 [&>svg]:relative [&>svg]:left-0 [&>svg]:top-0 [&>svg~*]:pl-0 [&>svg+div]:translate-y-0"
                    >
                      <Mail className="h-4 w-4 flex-shrink-0" />
                      <span className="flex-1 text-sm">驗證碼已發送至您的信箱</span>
                      {otpExpiry > 0 && (
                        <Badge variant="secondary" className="flex-shrink-0">
                          <Clock className="h-3 w-3 mr-1" />
                          {Math.floor(otpExpiry / 60)}:
                          {(otpExpiry % 60).toString().padStart(2, '0')}
                        </Badge>
                      )}
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
                  onChange={e => {
                    const value = e.target.value.replace(/\D/g, '') // 只允許數字
                    setFormData({ ...formData, phone: value })
                    // 清除錯誤當用戶開始輸入時
                    if (fieldErrors.phone) {
                      setFieldErrors({ ...fieldErrors, phone: undefined })
                    }
                  }}
                  onBlur={() => {
                    if (formData.phone && !validatePhone(formData.phone)) {
                      setFieldErrors({
                        ...fieldErrors,
                        phone: '請輸入有效的手機號碼（09 開頭，共十碼）',
                      })
                    } else if (formData.phone && validatePhone(formData.phone)) {
                      setFieldErrors({ ...fieldErrors, phone: undefined })
                    }
                  }}
                  placeholder="請輸入您的手機號碼 09 開頭"
                  maxLength={10}
                  required
                  aria-invalid={!!fieldErrors.phone}
                />
                {fieldErrors.phone && (
                  <FieldError>{fieldErrors.phone}</FieldError>
                )}
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
                  placeholder="請輸入您的公司或單位名稱"
                  maxLength={100}
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
