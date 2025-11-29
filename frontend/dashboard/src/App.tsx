import { useState } from 'react'
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
import { RawDataTable } from '@/pages/RawDataTable'
import { ProcessedDataTable } from '@/pages/ProcessedDataTable'
import { UserManagement } from '@/pages/UserManagement'

type Page = 'raw-data' | 'processed-data' | 'user-management' | null

function App() {
  const [currentPage, setCurrentPage] = useState<Page>(null)

  const getBreadcrumb = () => {
    switch (currentPage) {
      case 'raw-data':
        return {
          parent: '數據列表',
          current: '原始資料表',
        }
      case 'processed-data':
        return {
          parent: '數據列表',
          current: '整理後資料表',
        }
      case 'user-management':
        return {
          parent: '帳號與安全',
          current: '人員管理',
        }
      default:
        return {
          parent: '',
          current: 'Dashboard',
        }
    }
  }

  const renderPage = () => {
    switch (currentPage) {
      case 'raw-data':
        return <RawDataTable />
      case 'processed-data':
        return <ProcessedDataTable />
      case 'user-management':
        return <UserManagement />
      default:
        return (
          <div className="flex flex-1 flex-col gap-4 p-4">
            <div className="grid auto-rows-min gap-4 md:grid-cols-3">
              <div className="bg-muted/50 aspect-video rounded-xl" />
              <div className="bg-muted/50 aspect-video rounded-xl" />
              <div className="bg-muted/50 aspect-video rounded-xl" />
            </div>
            <div className="bg-muted/50 min-h-[100vh] flex-1 rounded-xl md:min-h-min" />
          </div>
        )
    }
  }

  const breadcrumb = getBreadcrumb()

  return (
    <SidebarProvider>
      <AppSidebar onNavigate={setCurrentPage} />
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
        {renderPage()}
      </SidebarInset>
    </SidebarProvider>
  )
}

export default App
