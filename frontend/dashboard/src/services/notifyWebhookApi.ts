const API_BASE_URL = '/api/dashboard/notifywebhooks'

export interface NotifyWebhook {
  dept: string
  type: string
  webhook: string
}

// GET /api/dashboard/notifywebhooks - 取得所有 webhook 列表
export async function getNotifyWebhooks(): Promise<NotifyWebhook[]> {
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
      .catch(() => ({ error: '取得 webhook 列表失敗' }))
    throw new Error(errorData.error || '取得 webhook 列表失敗')
  }

  return response.json()
}

// GET /api/dashboard/notifywebhooks/{dept} - 根據單位取得 webhook
export async function getNotifyWebhookByDept(dept: string): Promise<NotifyWebhook> {
  const response = await fetch(`${API_BASE_URL}/${encodeURIComponent(dept)}`, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ error: '取得 webhook 資料失敗' }))
    throw new Error(errorData.error || '取得 webhook 資料失敗')
  }

  return response.json()
}

// POST /api/dashboard/notifywebhooks - 建立新 webhook
export async function createNotifyWebhook(webhook: {
  dept: string
  type: string
  webhook: string
}): Promise<NotifyWebhook> {
  const response = await fetch(API_BASE_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
    body: JSON.stringify(webhook),
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ error: '建立 webhook 失敗' }))
    throw new Error(errorData.error || '建立 webhook 失敗')
  }

  return response.json()
}

// PUT /api/dashboard/notifywebhooks/{dept} - 更新 webhook
export async function updateNotifyWebhook(
  dept: string,
  updates: {
    type: string
    webhook: string
  }
): Promise<NotifyWebhook> {
  const response = await fetch(`${API_BASE_URL}/${encodeURIComponent(dept)}`, {
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
      .catch(() => ({ error: '更新 webhook 失敗' }))
    throw new Error(errorData.error || '更新 webhook 失敗')
  }

  return response.json()
}

// DELETE /api/dashboard/notifywebhooks/{dept} - 刪除 webhook
export async function deleteNotifyWebhook(dept: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/${encodeURIComponent(dept)}`, {
    method: 'DELETE',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ error: '刪除 webhook 失敗' }))
    throw new Error(errorData.error || '刪除 webhook 失敗')
  }

  // DELETE 請求可能沒有回應內容
  if (response.status !== 204 && response.headers.get('content-length') !== '0') {
    await response.json().catch(() => {})
  }
}

