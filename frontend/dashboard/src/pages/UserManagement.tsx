import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function UserManagement() {
  return (
    <div className="flex flex-1 flex-col gap-4 p-4">
      <Card>
        <CardHeader>
          <CardTitle>人員管理</CardTitle>
          <CardDescription>管理系統使用者帳號</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="text-muted-foreground">
            {/* 內容將在此處添加 */}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

