import { useState, useEffect, useMemo } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Pagination } from '@/components/ui/pagination'
import { Loader2, AlertTriangle } from 'lucide-react'
import { getCheckLogs, type CheckLog } from '@/services/checklogApi'

const ITEMS_PER_PAGE = 10

export function RawDataTable() {
  const [checkLogs, setCheckLogs] = useState<CheckLog[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')
  const [currentPage, setCurrentPage] = useState(1)

  // 載入資料
  const loadData = async () => {
    try {
      setLoading(true)
      setError('')
      const data = await getCheckLogs()
      setCheckLogs(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : '載入資料失敗')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadData()
  }, [])

  // 分頁計算
  const paginatedLogs = useMemo(() => {
    const startIndex = (currentPage - 1) * ITEMS_PER_PAGE
    const endIndex = startIndex + ITEMS_PER_PAGE
    return checkLogs.slice(startIndex, endIndex)
  }, [checkLogs, currentPage])

  const totalPages = Math.ceil(checkLogs.length / ITEMS_PER_PAGE)

  // 當資料載入完成時，重置到第一頁
  useEffect(() => {
    if (!loading && checkLogs.length > 0) {
      setCurrentPage(1)
    }
  }, [loading, checkLogs.length])

  // 格式化時間顯示
  const formatDateTime = (timestamp: string) => {
    const date = new Date(timestamp)
    return date.toLocaleString('zh-TW', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    })
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-4">
      <Card>
        <CardHeader>
          <CardTitle>原始資料表</CardTitle>
          <CardDescription>查看和管理簽到簽退原始資料</CardDescription>
        </CardHeader>
        <CardContent>
          {error && (
            <Alert variant="destructive" className="mb-4">
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {loading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full border-collapse">
                <thead>
                  <tr className="border-b">
                    <th className="text-left p-4 font-medium">時間</th>
                    <th className="text-left p-4 font-medium">類型</th>
                    <th className="text-left p-4 font-medium">邀請者</th>
                    <th className="text-left p-4 font-medium">邀請者單位</th>
                    <th className="text-left p-4 font-medium">邀請者職稱</th>
                    <th className="text-left p-4 font-medium">訪客姓名</th>
                    <th className="text-left p-4 font-medium">訪客公司</th>
                    <th className="text-left p-4 font-medium">訪客電話</th>
                    <th className="text-left p-4 font-medium">會議時間</th>
                    <th className="text-left p-4 font-medium">會議名稱</th>
                    <th className="text-left p-4 font-medium">會議室</th>
                  </tr>
                </thead>
                <tbody>
                  {checkLogs.length === 0 ? (
                    <tr>
                      <td colSpan={11} className="text-center p-8 text-muted-foreground">
                        尚無資料
                      </td>
                    </tr>
                  ) : (
                    paginatedLogs.map((log, index) => (
                      <tr key={`${log.timestamp}-${index}`} className="border-b hover:bg-muted/50">
                        <td className="p-4 whitespace-nowrap">{formatDateTime(log.timestamp)}</td>
                        <td className="p-4">
                          <Badge variant={log.type === '簽到' ? 'default' : 'secondary'}>
                            {log.type || '-'}
                          </Badge>
                        </td>
                        <td className="p-4">
                          <div>
                            <div className="font-medium">{log.inviterName || '-'}</div>
                            <div className="text-sm text-muted-foreground">{log.inviterEmail}</div>
                          </div>
                        </td>
                        <td className="p-4">{log.inviterDept || '-'}</td>
                        <td className="p-4">{log.inviterTitle || '-'}</td>
                        <td className="p-4">
                          <div>
                            <div className="font-medium">{log.vistorName || '-'}</div>
                            <div className="text-sm text-muted-foreground">{log.vistorEmail}</div>
                          </div>
                        </td>
                        <td className="p-4">{log.vistorDept || '-'}</td>
                        <td className="p-4">{log.vistorPhone || '-'}</td>
                        <td className="p-4 whitespace-nowrap">{log.meetingTime || '-'}</td>
                        <td className="p-4">{log.meetingName || '-'}</td>
                        <td className="p-4">{log.meetingRoom || '-'}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
              {checkLogs.length > 0 && (
                <Pagination
                  currentPage={currentPage}
                  totalPages={totalPages}
                  onPageChange={setCurrentPage}
                  totalItems={checkLogs.length}
                  itemsPerPage={ITEMS_PER_PAGE}
                />
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
