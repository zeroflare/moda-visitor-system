const API_BASE_URL = '/api/dashboard/registermail'

export interface SendRegisterMailRequest {
  email: string
}

export interface SendRegisterMailResponse {
  message?: string
}

// POST /api/dashboard/registermail - 發送註冊信
export async function sendRegisterMail(
  request: SendRegisterMailRequest
): Promise<SendRegisterMailResponse> {
  const response = await fetch(API_BASE_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: '發送註冊信失敗' }))
    throw new Error(errorData.message || '發送註冊信失敗')
  }

  return response.json()
}
