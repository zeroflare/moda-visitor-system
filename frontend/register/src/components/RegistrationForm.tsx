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
    name?: string
    company?: string
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

  // 手機號驗證：台灣手機號碼或市話格式
  const validatePhone = (phone: string): boolean => {
    const phoneRegex = /^(?:09\d{2}-?\d{3}-?\d{3}|0\d{1,3}-?\d{6,8})$/
    return phoneRegex.test(phone)
  }

  // 驗證姓名和公司：只能輸入中英文、數字和底線
  const validateNameOrCompany = (value: string): boolean => {
    const regex = /^[\u4e00-\u9fa5a-zA-Z0-9_]+$/
    return regex.test(value)
  }

  // 過濾姓名和公司輸入：只保留中英文、數字和底線
  const filterNameOrCompany = (value: string): string => {
    return value.replace(/[^\u4e00-\u9fa5a-zA-Z0-9_]/g, '')
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
    const newFieldErrors: { email?: string; phone?: string; name?: string; company?: string } = {}

    // 驗證姓名：直接驗證原始輸入
    const trimmedName = formData.name.trim()
    if (!trimmedName) {
      newFieldErrors.name = '請輸入姓名'
    } else if (!validateNameOrCompany(trimmedName)) {
      newFieldErrors.name = '姓名只能輸入中英文、數字和底線，不能包含空格或其他特殊字元'
    }

    // 驗證公司：直接驗證原始輸入
    const trimmedCompany = formData.company.trim()
    if (!trimmedCompany) {
      newFieldErrors.company = '請輸入公司/單位'
    } else if (!validateNameOrCompany(trimmedCompany)) {
      newFieldErrors.company = '公司/單位只能輸入中英文、數字和底線，不能包含空格或其他特殊字元'
    }

    // 驗證電話號碼：直接驗證原始輸入
    const trimmedPhone = formData.phone.trim()
    if (!trimmedPhone) {
      newFieldErrors.phone = '請輸入電話號碼'
    } else if (!validatePhone(trimmedPhone)) {
      newFieldErrors.phone = '請輸入有效的台灣手機號碼或市話格式（例如：0912345678 或 0212345678）'
    }

    // 驗證 Email 格式
    const trimmedEmail = formData.email.trim()
    if (!trimmedEmail) {
      newFieldErrors.email = '請輸入電子信箱'
    } else if (!validateEmail(trimmedEmail)) {
      newFieldErrors.email = '請輸入有效的電子信箱格式'
    }

    // 驗證 OTP
    const trimmedOtp = formData.otp.trim()
    if (!trimmedOtp) {
      const errorMsg = '請輸入驗證碼'
      setError(errorMsg)
      onError?.(errorMsg)
      return
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
      // 驗證通過後，清理資料用於 API 請求
      const cleanedName = filterNameOrCompany(formData.name.trim())
      const cleanedCompany = filterNameOrCompany(formData.company.trim())
      const cleanedPhone = formData.phone.trim().replace(/\s/g, '')
      const cleanedEmail = formData.email.trim()
      const cleanedOtp = formData.otp.trim()

      const requestBody: {
        name: string
        email: string
        phone: string
        company: string
        otp: string
        token?: string
      } = {
        name: cleanedName,
        email: cleanedEmail,
        phone: cleanedPhone,
        company: cleanedCompany,
        otp: cleanedOtp,
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
        // 檢查是否為 token 不存在或已過期的錯誤
        if (response.status === 404 && errorData.error === 'token 不存在或已過期') {
          throw new Error('token 不存在或已過期')
        }
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
                  onChange={e => {
                    setFormData({ ...formData, name: e.target.value })
                    // 清除錯誤當用戶開始輸入時
                    if (fieldErrors.name) {
                      setFieldErrors({ ...fieldErrors, name: undefined })
                    }
                  }}
                  placeholder="請輸入您的姓名（僅中英文、數字和底線）"
                  maxLength={50}
                  required
                  aria-invalid={!!fieldErrors.name}
                />
                {fieldErrors.name && (
                  <FieldError>{fieldErrors.name}</FieldError>
                )}
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
                    setFormData({ ...formData, phone: e.target.value })
                    // 清除錯誤當用戶開始輸入時
                    if (fieldErrors.phone) {
                      setFieldErrors({ ...fieldErrors, phone: undefined })
                    }
                  }}
                  placeholder="例如：0912-345-678 或 02-1234-5678"
                  maxLength={15}
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
                  onChange={e => {
                    setFormData({ ...formData, company: e.target.value })
                    // 清除錯誤當用戶開始輸入時
                    if (fieldErrors.company) {
                      setFieldErrors({ ...fieldErrors, company: undefined })
                    }
                  }}
                  placeholder="請輸入您的公司或單位名稱（僅中英文、數字和底線）"
                  maxLength={100}
                  required
                  aria-invalid={!!fieldErrors.company}
                />
                {fieldErrors.company && (
                  <FieldError>{fieldErrors.company}</FieldError>
                )}
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
                      送出中...
                    </>
                  ) : (
                    <>
                      <CheckCircle2 className="h-4 w-4" />
                      送出註冊
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
