const API_BASE_URL = '/api/dashboard/counters'

export interface Counter {
  id: string | null
  name: string | null
}

// GET /api/dashboard/counters - 取得櫃檯列表
export async function getCounters(): Promise<Counter[]> {
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
      .catch(() => ({ message: '取得櫃檯列表失敗' }))
    throw new Error(errorData.message || '取得櫃檯列表失敗')
  }

  return response.json()
}

// POST /api/dashboard/counters - 新增櫃檯
export async function createCounter(counter: {
  id: string
  name: string
}): Promise<Counter> {
  const response = await fetch(API_BASE_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
    body: JSON.stringify(counter),
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: '新增櫃檯失敗' }))
    throw new Error(errorData.message || '新增櫃檯失敗')
  }

  return response.json()
}

// GET /api/dashboard/counters/{id} - 取得單一櫃檯
export async function getCounterById(id: string): Promise<Counter> {
  const response = await fetch(`${API_BASE_URL}/${encodeURIComponent(id)}`, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: '取得櫃檯資料失敗' }))
    throw new Error(errorData.message || '取得櫃檯資料失敗')
  }

  return response.json()
}

// PUT /api/dashboard/counters/{id} - 更新櫃檯
export async function updateCounter(
  id: string,
  updates: {
    name: string
  }
): Promise<Counter> {
  const response = await fetch(`${API_BASE_URL}/${encodeURIComponent(id)}`, {
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
      .catch(() => ({ message: '更新櫃檯失敗' }))
    throw new Error(errorData.message || '更新櫃檯失敗')
  }

  return response.json()
}

// DELETE /api/dashboard/counters/{id} - 刪除櫃檯
export async function deleteCounter(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: '刪除櫃檯失敗' }))
    throw new Error(errorData.message || '刪除櫃檯失敗')
  }

  // DELETE 請求可能沒有回應內容
  if (response.status !== 204 && response.headers.get('content-length') !== '0') {
    await response.json().catch(() => {})
  }
}

