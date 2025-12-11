import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Loader2, AlertTriangle, CheckCircle2, Clock } from 'lucide-react'
import { triggerCron } from '@/services/cronApi'

export function CronManagement() {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string>('')
  const [success, setSuccess] = useState<string>('')
  const [lastTriggered, setLastTriggered] = useState<{
    triggeredAt: string
    triggeredBy: string
  } | null>(null)

  const handleTriggerCron = async () => {
    setLoading(true)
    setError('')
    setSuccess('')

    try {
      const response = await triggerCron()
      setSuccess(response.message || '排程任務已成功觸發')
      setLastTriggered({
        triggeredAt: response.triggeredAt,
        triggeredBy: response.triggeredBy,
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : '觸發排程任務失敗')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-4">
      <Card>
        <CardHeader>
          <div>
            <CardTitle>手動排程</CardTitle>
            <CardDescription>手動觸發每日排程任務</CardDescription>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {error && (
            <Alert variant="destructive">
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {success && (
            <Alert>
              <CheckCircle2 className="h-4 w-4" />
              <AlertDescription>{success}</AlertDescription>
            </Alert>
          )}

          <div className="space-y-4">
            <div className="rounded-lg border p-4">
              <div className="flex items-center gap-2 mb-2">
                <Clock className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm font-medium">排程說明</span>
              </div>
              <p className="text-sm text-muted-foreground">
                點擊下方按鈕可手動觸發每日排程任務。此操作將執行系統預設的每日排程作業。
              </p>
            </div>

            {lastTriggered && (
              <div className="rounded-lg border p-4 bg-muted/50">
                <div className="text-sm space-y-1">
                  <div>
                    <span className="font-medium">最後觸發時間：</span>
                    <span className="text-muted-foreground">
                      {new Date(lastTriggered.triggeredAt).toLocaleString('zh-TW')}
                    </span>
                  </div>
                  <div>
                    <span className="font-medium">觸發者：</span>
                    <span className="text-muted-foreground">{lastTriggered.triggeredBy}</span>
                  </div>
                </div>
              </div>
            )}

            <Button
              onClick={handleTriggerCron}
              disabled={loading}
              className="w-full sm:w-auto"
            >
              {loading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  觸發中...
                </>
              ) : (
                '觸發排程任務'
              )}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

