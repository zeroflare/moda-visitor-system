import { useState } from 'react'
import { Routes, Route, Navigate, useLocation } from 'react-router-dom'
import './App.css'
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

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(true)

  const handleLogin = (email: string, otp: string) => {
    console.log('Login attempt:', email, otp)
    setIsAuthenticated(true)
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
