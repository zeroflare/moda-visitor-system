const API_BASE_URL = '/api/dashboard/users'

export interface User {
  username: string
  email: string
  role: string
}

// GET /api/dashboard/users - 取得使用者列表
export async function getUsers(): Promise<User[]> {
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
      .catch(() => ({ message: '取得使用者列表失敗' }))
    throw new Error(errorData.message || '取得使用者列表失敗')
  }

  return response.json()
}

// POST /api/dashboard/users - 新增使用者
export async function createUser(user: {
  username: string
  email: string
  role: string
}): Promise<User> {
  const response = await fetch(API_BASE_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
    body: JSON.stringify(user),
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: '新增使用者失敗' }))
    throw new Error(errorData.message || '新增使用者失敗')
  }

  return response.json()
}

// GET /api/dashboard/users/{email} - 取得單一使用者
export async function getUserByEmail(email: string): Promise<User> {
  const response = await fetch(`${API_BASE_URL}/${encodeURIComponent(email)}`, {
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

// PUT /api/dashboard/users/{email} - 更新使用者
export async function updateUser(
  email: string,
  updates: {
    username: string
    email: string
    role: string
  }
): Promise<User> {
  const response = await fetch(`${API_BASE_URL}/${encodeURIComponent(email)}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
    body: JSON.stringify(updates),
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: '更新使用者失敗' }))
    throw new Error(errorData.message || '更新使用者失敗')
  }

  return response.json()
}

// DELETE /api/dashboard/users/{email} - 刪除使用者
export async function deleteUser(email: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/${encodeURIComponent(email)}`, {
    method: 'DELETE',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: '刪除使用者失敗' }))
    throw new Error(errorData.message || '刪除使用者失敗')
  }

  // DELETE 請求可能沒有回應內容
  if (response.status !== 204 && response.headers.get('content-length') !== '0') {
    await response.json().catch(() => {})
  }
}

