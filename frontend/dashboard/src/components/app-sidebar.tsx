import * as React from "react"

import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarRail,
} from "@/components/ui/sidebar"

type Page = 'raw-data' | 'processed-data' | 'user-management' | null

interface AppSidebarProps extends React.ComponentProps<typeof Sidebar> {
  onNavigate?: (page: Page) => void
}

const data = {
  navMain: [
    {
      title: "數據列表",
      url: "#",
      items: [
        {
          title: "原始資料表",
          url: "#",
          page: 'raw-data' as Page,
        },
        {
          title: "整理後資料表",
          url: "#",
          page: 'processed-data' as Page,
        },
        {
          title: "憑證列表",
          url: "#",
          page: null,
        },
      ],
    },
    {
      title: "帳號與安全",
      url: "#",
      items: [
        {
          title: "人員管理",
          url: "#",
          page: 'user-management' as Page,
        },
        {
          title: "操作紀錄",
          url: "#",
          page: null,
        },
      ],
    },
    {
      title: "系統配置",
      url: "#",
      items: [
        {
          title: "參數設定",
          url: "#",
          page: null,
        },
        {
          title: "郵件模板",
          url: "#",
          page: null,
        },
      ],
    },
  ],
}

export function AppSidebar({ onNavigate, ...props }: AppSidebarProps) {
  return (
    <Sidebar {...props}>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" asChild>
              <a href="#">
                <div className="flex aspect-square size-8 items-center justify-center rounded-lg">
                  <img src="/moda_logo.png" alt="moda" className="h-full w-full object-contain" />
                </div>
                <div className="flex flex-col gap-0.5 leading-none">
                  <span className="font-medium">數位發展部</span>
                  <span className="">訪客系統</span>
                </div>
              </a>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarMenu>
            {data.navMain.map((item) => (
              <SidebarMenuItem key={item.title}>
                <SidebarMenuButton asChild>
                  <a href={item.url} className="font-medium">
                    {item.title}
                  </a>
                </SidebarMenuButton>
                {item.items?.length ? (
                  <SidebarMenuSub>
                    {item.items.map((subItem) => (
                      <SidebarMenuSubItem key={subItem.title}>
                        <SidebarMenuSubButton asChild>
                          <a
                            href={subItem.url}
                            onClick={(e) => {
                              e.preventDefault()
                              if (onNavigate && subItem.page !== undefined) {
                                onNavigate(subItem.page)
                              }
                            }}
                          >
                            {subItem.title}
                          </a>
                        </SidebarMenuSubButton>
                      </SidebarMenuSubItem>
                    ))}
                  </SidebarMenuSub>
                ) : null}
              </SidebarMenuItem>
            ))}
          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  )
}
