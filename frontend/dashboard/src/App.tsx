import { useState, useEffect } from 'react'
import { Routes, Route, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { getMe } from '@/services/loginApi'
import { AppSidebar } from '@/components/app-sidebar'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'
import { Separator } from '@/components/ui/separator'
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from '@/components/ui/sidebar'
import { Login } from '@/pages/Login'
import { RawDataTable } from '@/pages/RawDataTable'
import { ProcessedDataTable } from '@/pages/ProcessedDataTable'
import { UserManagement } from '@/pages/UserManagement'

interface User {
  email: string
  username: string
  role: string
}

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null) // null 表示正在檢查
  const [user, setUser] = useState<User | null>(null)
  const navigate = useNavigate()

  // 檢查 session cookie 是否有效
  useEffect(() => {
    const checkSession = async () => {
      try {
        const userData = await getMe()
        setUser(userData)
        setIsAuthenticated(true)
      } catch {
        // Session 無效或不存在
        setUser(null)
        setIsAuthenticated(false)
      }
    }

    checkSession()
  }, [])

  // 使用 user 來避免 lint 警告（未來可能會在 UI 中使用）
  if (user) {
    // user 資料已載入，可在未來用於顯示使用者資訊
  }

  const handleLogin = (user: User) => {
    console.log('Login successful:', user)
    setUser(user)
    setIsAuthenticated(true)
    navigate('/raw-data', { replace: true })
  }

  // 正在檢查 session，顯示載入狀態或空白
  if (isAuthenticated === null) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-muted-foreground">載入中...</div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Login onLogin={handleLogin} />
  }

  return (
    <Routes>
      <Route path="/login" element={<Login onLogin={handleLogin} />} />
      <Route path="/*" element={<DashboardLayout />} />
    </Routes>
  )
}

function DashboardLayout() {
  const location = useLocation()

  const getBreadcrumb = () => {
    if (location.pathname === '/raw-data' || location.pathname === '/') {
      return {
        parent: '數據列表',
        current: '原始資料表',
      }
    }
    if (location.pathname === '/processed-data') {
      return {
        parent: '數據列表',
        current: '整理後資料表',
      }
    }
    if (location.pathname === '/user-management') {
      return {
        parent: '帳號與安全',
        current: '人員管理',
      }
    }
    return {
      parent: '',
      current: 'Dashboard',
    }
  }

  const breadcrumb = getBreadcrumb()

  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset>
        <header className="flex h-16 shrink-0 items-center gap-2 border-b">
          <div className="flex items-center gap-2 px-3">
            <SidebarTrigger />
            <Separator orientation="vertical" className="mr-2 h-4" />
            <Breadcrumb>
              <BreadcrumbList>
                {breadcrumb.parent && (
                  <>
                    <BreadcrumbItem className="hidden md:block">
                      <BreadcrumbLink href="#">{breadcrumb.parent}</BreadcrumbLink>
                    </BreadcrumbItem>
                    <BreadcrumbSeparator className="hidden md:block" />
                  </>
                )}
                <BreadcrumbItem>
                  <BreadcrumbPage>{breadcrumb.current}</BreadcrumbPage>
                </BreadcrumbItem>
              </BreadcrumbList>
            </Breadcrumb>
          </div>
        </header>
        <Routes>
          <Route path="/" element={<Navigate to="/raw-data" replace />} />
          <Route path="/raw-data" element={<RawDataTable />} />
          <Route path="/processed-data" element={<ProcessedDataTable />} />
          <Route path="/user-management" element={<UserManagement />} />
        </Routes>
      </SidebarInset>
    </SidebarProvider>
  )
}

export default App
