import { useState, useEffect, useMemo, useRef } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Pagination } from '@/components/ui/pagination'
import { Button } from '@/components/ui/button'
import { Loader2, AlertTriangle, RefreshCw, Check } from 'lucide-react'
import { getCheckLogs, type CheckLog } from '@/services/checklogApi'

const ITEMS_PER_PAGE = 10

export function RawDataTable() {
  const [checkLogs, setCheckLogs] = useState<CheckLog[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')
  const [currentPage, setCurrentPage] = useState(1)
  const [autoRefresh, setAutoRefresh] = useState(false)
  const intervalRef = useRef<number | null>(null)

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

  // 自動刷新邏輯
  useEffect(() => {
    if (autoRefresh) {
      // 啟動自動刷新，每5秒刷新一次
      intervalRef.current = setInterval(() => {
        loadData()
      }, 5000)
    } else {
      // 清除定時器
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
    }

    // 清理函數：組件卸載或 autoRefresh 改變時清除定時器
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
    }
  }, [autoRefresh])

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
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>原始資料表</CardTitle>
              <CardDescription>查看和管理簽到簽退原始資料</CardDescription>
            </div>
            <Button
              variant={autoRefresh ? 'default' : 'outline'}
              size="sm"
              onClick={() => setAutoRefresh(!autoRefresh)}
              className="gap-2"
            >
              {autoRefresh ? (
                <>
                  <Check className="h-4 w-4" />
                  自動刷新中
                </>
              ) : (
                <>
                  <RefreshCw className="h-4 w-4" />
                  自動刷新
                </>
              )}
            </Button>
          </div>
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
              <table className="w-full border-collapse min-w-[1000px]">
                <thead>
                  <tr className="border-b">
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">時間</th>
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">類型</th>
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">訪客資訊</th>
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">會議資訊</th>
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">邀請者資訊</th>
                  </tr>
                </thead>
                <tbody>
                  {checkLogs.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="text-center p-6 sm:p-8 text-muted-foreground text-sm">
                        尚無資料
                      </td>
                    </tr>
                  ) : (
                    paginatedLogs.map((log, index) => (
                      <tr key={`${log.timestamp}-${index}`} className="border-b hover:bg-muted/50">
                        <td className="p-2 sm:p-3 lg:p-4 whitespace-nowrap text-xs sm:text-sm">{formatDateTime(log.timestamp)}</td>
                        <td className="p-2 sm:p-3 lg:p-4">
                          <Badge variant={log.type === '簽到' ? 'default' : 'secondary'}>
                            {log.type || '-'}
                          </Badge>
                        </td>
                        <td className="p-2 sm:p-3 lg:p-4">
                          <div className="space-y-0.5 sm:space-y-1">
                            <div className="font-medium text-xs sm:text-sm">{log.vistorName || '-'}</div>
                            <div className="text-xs">{log.vistorDept || '-'}</div>
                            <div className="text-xs text-muted-foreground truncate">{log.vistorEmail || '-'}</div>
                            <div className="text-xs text-muted-foreground">{log.vistorPhone || '-'}</div>
                          </div>
                        </td>
                        <td className="p-2 sm:p-3 lg:p-4">
                          <div className="space-y-0.5 sm:space-y-1">
                            <div className="font-medium text-xs sm:text-sm">{log.meetingName || '-'}</div>
                            <div className="text-xs text-muted-foreground">{log.meetingRoom || '-'}</div>
                            <div className="text-xs text-muted-foreground whitespace-nowrap">{log.meetingTime || '-'}</div>
                          </div>
                        </td>
                        <td className="p-2 sm:p-3 lg:p-4">
                          <div className="space-y-0.5 sm:space-y-1">
                            <div className="font-medium text-xs sm:text-sm">{log.inviterName || '-'}</div>
                            <div className="text-xs">{log.inviterDept || '-'}</div>
                            <div className="text-xs text-muted-foreground">{log.inviterTitle || '-'}</div>
                            <div className="text-xs text-muted-foreground truncate">{log.inviterEmail || '-'}</div>
                          </div>
                        </td>
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
