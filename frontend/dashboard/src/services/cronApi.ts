const API_BASE_URL = '/api/dashboard/cron'

export interface CronTriggerResponse {
  message: string
  triggeredAt: string
  triggeredBy: string
}

// GET /api/dashboard/cron - 手動觸發每日排程任務
export async function triggerCron(): Promise<CronTriggerResponse> {
  const response = await fetch(API_BASE_URL, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ error: '觸發排程任務失敗' }))
    throw new Error(errorData.error || errorData.message || '觸發排程任務失敗')
  }

  return response.json()
}

