const API_BASE_URL = '/api/dashboard/meetingrooms'

export interface MeetingRoom {
  id: string | null
  name: string | null
  counterId: string | null
}

export interface MeetingRoomResponse {
  id: string | null
  name: string | null
  counterId: string | null
  counterName: string | null
}

// GET /api/dashboard/meetingrooms - 取得會議室列表
export async function getMeetingRooms(counterId?: string): Promise<MeetingRoomResponse[]> {
  const url = counterId
    ? `${API_BASE_URL}?counterId=${encodeURIComponent(counterId)}`
    : API_BASE_URL

  const response = await fetch(url, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: '取得會議室列表失敗' }))
    throw new Error(errorData.message || '取得會議室列表失敗')
  }

  return response.json()
}

// POST /api/dashboard/meetingrooms - 新增會議室
export async function createMeetingRoom(meetingRoom: {
  name: string
  counterId: string
}): Promise<MeetingRoom> {
  const response = await fetch(API_BASE_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // 確保 cookies 會被包含在請求中
    body: JSON.stringify(meetingRoom),
  })

  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: '新增會議室失敗' }))
    throw new Error(errorData.message || '新增會議室失敗')
  }

  return response.json()
}

// GET /api/dashboard/meetingrooms/{id} - 取得單一會議室
export async function getMeetingRoomById(id: string): Promise<MeetingRoomResponse> {
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
      .catch(() => ({ message: '取得會議室資料失敗' }))
    throw new Error(errorData.message || '取得會議室資料失敗')
  }

  return response.json()
}

// PUT /api/dashboard/meetingrooms/{id} - 更新會議室
export async function updateMeetingRoom(
  id: string,
  updates: {
    name: string
    counterId: string
  }
): Promise<MeetingRoom> {
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
      .catch(() => ({ message: '更新會議室失敗' }))
    throw new Error(errorData.message || '更新會議室失敗')
  }

  return response.json()
}

// DELETE /api/dashboard/meetingrooms/{id} - 刪除會議室
export async function deleteMeetingRoom(id: string): Promise<void> {
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
      .catch(() => ({ message: '刪除會議室失敗' }))
    throw new Error(errorData.message || '刪除會議室失敗')
  }

  // DELETE 請求可能沒有回應內容
  if (response.status !== 204 && response.headers.get('content-length') !== '0') {
    await response.json().catch(() => {})
  }
}

