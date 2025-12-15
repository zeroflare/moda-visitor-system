// 開發環境使用相對路徑（透過 Vite proxy），生產環境使用完整 URL
const API_BASE_URL = '/api/dashboard/login'

export interface SendOTPRequest {
  email: string
}

export interface SendOTPResponse {
  message: string
}

export interface VerifyOTPRequest {
  email: string
  otp: string
}

export interface VerifyOTPResponse {
  message: string
}

export interface User {
  email: string
  username: string
  role: string
}

// POST /api/dashboard/login/otp - 發送 OTP
export async function sendOTP(
  request: SendOTPRequest
): Promise<SendOTPResponse> {
  const response = await fetch(`${API_BASE_URL}/otp`, {
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
      .catch(() => ({ message: '發送 OTP 失敗' }))
    throw new Error(errorData.message || '發送 OTP 失敗')
  }

  return response.json()
}

// POST /api/dashboard/login/result - 驗證 OTP
export async function verifyOTP(
  request: VerifyOTPRequest
): Promise<VerifyOTPResponse> {
  const response = await fetch(`${API_BASE_URL}/result`, {
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
      .catch(() => ({ message: 'OTP 驗證失敗' }))
    throw new Error(errorData.message || 'OTP 驗證失敗或過期')
  }

  return response.json()
}

// GET /api/dashboard/me - 取得目前登入使用者資料
export async function getMe(): Promise<User> {
  const API_ME_URL = '/api/dashboard/me'

  const response = await fetch(API_ME_URL, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: '取得使用者資料失敗' }))
    throw new Error(errorData.message || '取得使用者資料失敗')
  }

  return response.json()
}

export interface LogoutResponse {
  message: string
}

// GET /api/dashboard/logout - 登出，清除 session 與 cookie
export async function logout(): Promise<void> {
  const API_LOGOUT_URL = '/api/dashboard/logout'

  try {
    const response = await fetch(API_LOGOUT_URL, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include', // 確保 cookies 會被包含在請求中
    })

    if (!response.ok) {
      const errorData = await response
        .json()
        .catch(() => ({ error: '登出失敗' }))
      throw new Error(errorData.error || '登出失敗')
    }

    // 即使後端成功，也清除本地 cookie（以防萬一）
    document.cookie = 'dashboard_session=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;'
  } catch (error) {
    // 即使 API 調用失敗，也清除本地 cookie
    document.cookie = 'dashboard_session=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;'
    // 重新拋出錯誤，讓調用者知道登出可能未完全成功
    throw error
  }
}
