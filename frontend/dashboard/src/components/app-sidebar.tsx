import * as React from "react"
import { Link, useLocation } from "react-router-dom"

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

const data = {
  navMain: [
    {
      title: "資料列表",
      items: [
        {
          title: "原始資料表",
          path: "/raw-data",
        },
        {
          title: "整理後資料表",
          path: "/processed-data",
        },
        {
          title: "憑證列表",
          path: "#",
        },
      ],
    },
    {
      title: "帳號與安全",
      items: [
        {
          title: "人員管理",
          path: "/user-management",
        },
        {
          title: "發送註冊信",
          path: "/register-mail",
        },
        {
          title: "操作紀錄",
          path: "#",
        },
      ],
    },
    {
      title: "系統配置",
      items: [
        {
          title: "櫃檯管理",
          path: "/counter-management",
        },
        {
          title: "會議室管理",
          path: "/meetingroom-management",
        },
        {
          title: "手動排程",
          path: "/cron-management",
        },
        {
          title: "通知 Webhook 管理",
          path: "/notifywebhook-management",
        },
        {
          title: "參數設定",
          path: "#",
        },
        {
          title: "郵件模板",
          path: "#",
        },
      ],
    },
  ],
}

export function AppSidebar(props: React.ComponentProps<typeof Sidebar>) {
  const location = useLocation()

  return (
    <Sidebar {...props}>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" asChild>
              <Link to="/raw-data">
                <div className="flex aspect-square size-8 items-center justify-center rounded-lg">
                  <img src={`${import.meta.env.BASE_URL}moda_logo.png`} alt="moda" className="h-full w-full object-contain" />
                </div>
                <div className="flex flex-col gap-0.5 leading-none">
                  <span className="font-medium">數位發展部</span>
                  <span className="">訪客系統</span>
                </div>
              </Link>
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
                  <span className="font-medium">{item.title}</span>
                </SidebarMenuButton>
                {item.items?.length ? (
                  <SidebarMenuSub>
                    {item.items.map((subItem) => (
                      <SidebarMenuSubItem key={subItem.title}>
                        <SidebarMenuSubButton 
                          asChild 
                          isActive={location.pathname === subItem.path}
                        >
                          {subItem.path === "#" ? (
                            <span>{subItem.title}</span>
                          ) : (
                            <Link to={subItem.path}>
                              {subItem.title}
                            </Link>
                          )}
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
