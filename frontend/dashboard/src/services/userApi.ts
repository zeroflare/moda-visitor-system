// 假資料模擬 API
// const API_BASE_URL = '/api/dashboard/users' // 未來使用真實 API 時啟用

// 模擬延遲
const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

// 假資料儲存
const mockUsers: User[] = [
  {
    username: 'visitor-admin',
    email: 'admin@example.com',
    role: 'admin',
  },
  {
    username: 'visitor-user1',
    email: 'user1@example.com',
    role: 'user',
  },
  {
    username: 'visitor-user2',
    email: 'user2@example.com',
    role: 'user',
  },
  {
    username: 'visitor-user3',
    email: 'user3@example.com',
    role: 'user',
  },
  {
    username: 'visitor-user4',
    email: 'user4@example.com',
    role: 'user',
  },
  {
    username: 'visitor-admin2',
    email: 'admin2@example.com',
    role: 'admin',
  },
  {
    username: 'visitor-user5',
    email: 'user5@example.com',
    role: 'user',
  },
  {
    username: 'visitor-user6',
    email: 'user6@example.com',
    role: 'user',
  },
]

export interface User {
  username: string
  email: string
  role: 'admin' | 'user'
}

// GET /dashboard/users - 取得使用者列表
export async function getUsers(): Promise<User[]> {
  await delay(300)
  return [...mockUsers]
}

// POST /dashboard/users - 新增使用者
export async function createUser(user: Omit<User, 'email'> & { email: string }): Promise<User> {
  await delay(500)
  
  // 檢查 email 是否已存在
  if (mockUsers.some(u => u.email === user.email)) {
    throw new Error('此電子郵件已被使用')
  }
  
  const newUser: User = {
    username: user.username,
    email: user.email,
    role: user.role,
  }
  
  mockUsers.push(newUser)
  return { ...newUser }
}

// GET /dashboard/users/{email} - 取得單一使用者
export async function getUserByEmail(email: string): Promise<User> {
  await delay(300)
  
  const user = mockUsers.find(u => u.email === email)
  if (!user) {
    throw new Error('使用者不存在')
  }
  
  return { ...user }
}

// PUT /dashboard/users/{email} - 更新使用者
export async function updateUser(
  email: string,
  updates: Partial<User>
): Promise<User> {
  await delay(500)
  
  const index = mockUsers.findIndex(u => u.email === email)
  if (index === -1) {
    throw new Error('使用者不存在')
  }
  
  // 如果更新 email，檢查新 email 是否已被使用
  if (updates.email && updates.email !== email) {
    if (mockUsers.some(u => u.email === updates.email)) {
      throw new Error('此電子郵件已被使用')
    }
  }
  
  mockUsers[index] = {
    ...mockUsers[index],
    ...updates,
  }
  
  return { ...mockUsers[index] }
}

// DELETE /dashboard/users/{email} - 刪除使用者
export async function deleteUser(email: string): Promise<void> {
  await delay(500)
  
  const index = mockUsers.findIndex(u => u.email === email)
  if (index === -1) {
    throw new Error('使用者不存在')
  }
  
  mockUsers.splice(index, 1)
}

