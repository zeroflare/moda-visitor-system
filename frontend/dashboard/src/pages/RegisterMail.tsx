import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Loader2, AlertTriangle, CheckCircle2, Mail } from 'lucide-react'
import { sendRegisterMail } from '@/services/registerMailApi'

export function RegisterMail() {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string>('')
  const [success, setSuccess] = useState<string>('')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setSuccess('')
    setLoading(true)

    try {
      if (!email.trim()) {
        setError('請輸入電子郵件地址')
        setLoading(false)
        return
      }

      // 簡單的電子郵件格式驗證
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
      if (!emailRegex.test(email)) {
        setError('請輸入有效的電子郵件地址')
        setLoading(false)
        return
      }

      await sendRegisterMail({ email })
      setSuccess('註冊信已成功發送！')
      setEmail('') // 清空輸入框
    } catch (err) {
      setError(err instanceof Error ? err.message : '發送註冊信失敗')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-4">
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Mail className="h-5 w-5" />
            <div>
              <CardTitle>發送註冊信</CardTitle>
              <CardDescription>發送註冊信給指定的電子郵件地址</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <Alert variant="destructive">
                <AlertTriangle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            {success && (
              <Alert className="border-green-500 bg-green-50 text-green-900">
                <CheckCircle2 className="h-4 w-4" />
                <AlertDescription>{success}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="email">電子郵件地址 *</Label>
              <Input
                id="email"
                type="email"
                value={email}
                onChange={(e) => {
                  setEmail(e.target.value)
                  setError('')
                  setSuccess('')
                }}
                placeholder="example@email.com"
                required
                disabled={loading}
              />
            </div>

            <Button type="submit" disabled={loading} className="w-full">
              {loading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  發送中...
                </>
              ) : (
                <>
                  <Mail className="h-4 w-4 mr-2" />
                  發送註冊信
                </>
              )}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
