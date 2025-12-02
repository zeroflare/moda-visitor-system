const API_BASE_URL = '/api/dashboard/visitorlogs'

export interface VisitorLog {
  checkinTimestamp: string | null
  checkoutTimestamp: string | null
  inviterEmail: string
  inviterName: string | null
  inviterDept: string | null
  inviterTitle: string | null
  vistorEmail: string
  vistorName: string | null
  vistorDept: string | null
  vistorPhone: string | null
  meetingStart: string | null
  meetingEnd: string | null
  meetingName: string | null
  meetingRoom: string | null
}

// GET /api/dashboard/visitorlogs - 取得歷史簽到記錄
export async function getVisitorLogs(): Promise<VisitorLog[]> {
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
      .catch(() => ({ message: '取得訪客記錄失敗' }))
    throw new Error(errorData.message || '取得訪客記錄失敗')
  }

  const data = await response.json()
  // API 返回的資料已經按時間排序，直接返回
  return data
}

