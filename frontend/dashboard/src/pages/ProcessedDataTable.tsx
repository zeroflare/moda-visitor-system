import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function ProcessedDataTable() {
  return (
    <div className="flex flex-1 flex-col gap-4 p-4">
      <Card>
        <CardHeader>
          <CardTitle>整理後資料表</CardTitle>
          <CardDescription>查看和管理整理後的資料</CardDescription>
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

