const API_BASE_URL = '/api/dashboard/checklogs'

export interface CheckLog {
  timestamp: string
  type: string | null
  inviterEmail: string
  inviterName: string | null
  inviterDept: string | null
  inviterTitle: string | null
  vistorEmail: string
  vistorName: string | null
  vistorDept: string | null
  vistorPhone: string | null
  meetingTime: string
  meetingName: string | null
  meetingRoom: string | null
}

// GET /api/dashboard/checklogs - 查詢簽到簽退原始資料
export async function getCheckLogs(): Promise<CheckLog[]> {
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
      .catch(() => ({ message: '取得簽到記錄失敗' }))
    throw new Error(errorData.message || '取得簽到記錄失敗')
  }

  const data = await response.json()
  // API 返回的資料已經按時間排序，直接返回
  return data
}

