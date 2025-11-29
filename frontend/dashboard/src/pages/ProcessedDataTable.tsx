import { useState, useEffect, useMemo } from 'react'
import { type DateRange } from 'react-day-picker'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Pagination } from '@/components/ui/pagination'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Button } from '@/components/ui/button'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Calendar } from '@/components/ui/calendar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Loader2, AlertTriangle, X, Filter, CalendarIcon, Download, FileSpreadsheet, FileText } from 'lucide-react'
import { getVisitorLogs, type VisitorLog } from '@/services/visitorLogApi'
import { format } from 'date-fns'
import * as XLSX from 'xlsx'

const ITEMS_PER_PAGE = 10

export function ProcessedDataTable() {
  const [visitorLogs, setVisitorLogs] = useState<VisitorLog[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')
  const [currentPage, setCurrentPage] = useState(1)
  
  // 篩選狀態
  const [filterVisitor, setFilterVisitor] = useState<string>('')
  const [filterMeetingName, setFilterMeetingName] = useState<string>('')
  const [filterInviter, setFilterInviter] = useState<string>('')
  const [filterMeetingRoom, setFilterMeetingRoom] = useState<string>('')
  const [dateRange, setDateRange] = useState<DateRange | undefined>()

  // 載入資料
  const loadData = async () => {
    try {
      setLoading(true)
      setError('')
      const data = await getVisitorLogs()
      setVisitorLogs(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : '載入資料失敗')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadData()
  }, [])

  // 獲取所有唯一的會議室列表
  const meetingRooms = useMemo(() => {
    const rooms = new Set(visitorLogs.map(log => log.meetingRoom))
    return Array.from(rooms).sort()
  }, [visitorLogs])

  // 篩選邏輯
  const filteredLogs = useMemo(() => {
    return visitorLogs.filter(log => {
      // 訪客資訊篩選（姓名、公司、Email、電話）
      const visitorMatch = !filterVisitor || 
        log.vistorName.toLowerCase().includes(filterVisitor.toLowerCase()) ||
        log.vistorDept.toLowerCase().includes(filterVisitor.toLowerCase()) ||
        log.vistorEmail.toLowerCase().includes(filterVisitor.toLowerCase()) ||
        log.vistorPhone.includes(filterVisitor)

      // 會議名稱篩選
      const meetingNameMatch = !filterMeetingName ||
        log.meetingName.toLowerCase().includes(filterMeetingName.toLowerCase())

      // 邀請者資訊篩選（姓名、單位、職稱、Email）
      const inviterMatch = !filterInviter ||
        log.inviterName.toLowerCase().includes(filterInviter.toLowerCase()) ||
        log.inviterDept.toLowerCase().includes(filterInviter.toLowerCase()) ||
        log.inviterTitle.toLowerCase().includes(filterInviter.toLowerCase()) ||
        log.inviterEmail.toLowerCase().includes(filterInviter.toLowerCase())

      // 會議室篩選
      const meetingRoomMatch = !filterMeetingRoom ||
        log.meetingRoom === filterMeetingRoom

      // 日期範圍篩選（根據簽到時間）
      let dateMatch = true
      if (dateRange?.from || dateRange?.to) {
        const checkinDate = new Date(log.checkinTimestamp)
        checkinDate.setHours(0, 0, 0, 0)
        
        if (dateRange.from) {
          const startDate = new Date(dateRange.from)
          startDate.setHours(0, 0, 0, 0)
          if (checkinDate < startDate) {
            dateMatch = false
          }
        }
        
        if (dateRange.to && dateMatch) {
          const endDate = new Date(dateRange.to)
          endDate.setHours(23, 59, 59, 999)
          if (checkinDate > endDate) {
            dateMatch = false
          }
        }
      }

      return visitorMatch && meetingNameMatch && inviterMatch && meetingRoomMatch && dateMatch
    })
  }, [visitorLogs, filterVisitor, filterMeetingName, filterInviter, filterMeetingRoom, dateRange])

  // 分頁計算（基於篩選後的資料）
  const paginatedLogs = useMemo(() => {
    const startIndex = (currentPage - 1) * ITEMS_PER_PAGE
    const endIndex = startIndex + ITEMS_PER_PAGE
    return filteredLogs.slice(startIndex, endIndex)
  }, [filteredLogs, currentPage])

  const totalPages = Math.ceil(filteredLogs.length / ITEMS_PER_PAGE)

  // 當篩選條件改變時，重置到第一頁
  useEffect(() => {
    setCurrentPage(1)
  }, [filterVisitor, filterMeetingName, filterInviter, filterMeetingRoom, dateRange])

  // 當資料載入完成時，重置到第一頁
  useEffect(() => {
    if (!loading && visitorLogs.length > 0) {
      setCurrentPage(1)
    }
  }, [loading, visitorLogs.length])

  // 格式化時間顯示
  const formatDateTime = (timestamp: string | null) => {
    if (!timestamp) return '-'
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

  // 計算停留時間
  const calculateDuration = (checkin: string, checkout: string | null) => {
    if (!checkout) return '進行中'
    
    const checkinTime = new Date(checkin).getTime()
    const checkoutTime = new Date(checkout).getTime()
    const durationMs = checkoutTime - checkinTime
    const hours = Math.floor(durationMs / (1000 * 60 * 60))
    const minutes = Math.floor((durationMs % (1000 * 60 * 60)) / (1000 * 60))
    
    if (hours > 0) {
      return `${hours} 小時 ${minutes} 分鐘`
    }
    return `${minutes} 分鐘`
  }

  // 檢查是否有篩選條件
  const hasActiveFilters = filterVisitor || filterMeetingName || filterInviter || filterMeetingRoom || dateRange?.from || dateRange?.to

  // 清除所有篩選
  const clearAllFilters = () => {
    setFilterVisitor('')
    setFilterMeetingName('')
    setFilterInviter('')
    setFilterMeetingRoom('')
    setDateRange(undefined)
  }

  // 格式化日期範圍顯示
  const formatDateRange = () => {
    if (!dateRange?.from && !dateRange?.to) {
      return '選擇日期範圍'
    }
    if (dateRange.from && dateRange.to) {
      return `${format(dateRange.from, 'yyyy/MM/dd')} - ${format(dateRange.to, 'yyyy/MM/dd')}`
    }
    if (dateRange.from) {
      return `從 ${format(dateRange.from, 'yyyy/MM/dd')}`
    }
    if (dateRange.to) {
      return `至 ${format(dateRange.to, 'yyyy/MM/dd')}`
    }
    return '選擇日期範圍'
  }

  // 準備匯出資料
  const prepareExportData = () => {
    return filteredLogs.map(log => ({
      '訪客姓名': log.vistorName,
      '訪客公司': log.vistorDept,
      '訪客Email': log.vistorEmail,
      '訪客電話': log.vistorPhone,
      '會議名稱': log.meetingName,
      '會議室': log.meetingRoom,
      '會議時間': log.meetingTime,
      '邀請者姓名': log.inviterName,
      '邀請者單位': log.inviterDept,
      '邀請者職稱': log.inviterTitle,
      '邀請者Email': log.inviterEmail,
      '簽到時間': formatDateTime(log.checkinTimestamp),
      '簽退時間': formatDateTime(log.checkoutTimestamp),
      '停留時間': log.checkoutTimestamp 
        ? calculateDuration(log.checkinTimestamp, log.checkoutTimestamp)
        : '-'
    }))
  }

  // 匯出 CSV
  const exportToCSV = () => {
    const data = prepareExportData()
    if (data.length === 0) {
      return
    }

    // 取得欄位名稱
    const headers = Object.keys(data[0])
    
    // 建立 CSV 內容
    const csvContent = [
      headers.join(','),
      ...data.map(row => 
        headers.map(header => {
          const value = row[header as keyof typeof row]
          // 處理包含逗號、引號或換行的值
          if (typeof value === 'string' && (value.includes(',') || value.includes('"') || value.includes('\n'))) {
            return `"${value.replace(/"/g, '""')}"`
          }
          return value
        }).join(',')
      )
    ].join('\n')

    // 加入 BOM 以支援中文
    const BOM = '\uFEFF'
    const blob = new Blob([BOM + csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    const url = URL.createObjectURL(blob)
    link.setAttribute('href', url)
    link.setAttribute('download', `整理後資料表_${format(new Date(), 'yyyyMMdd_HHmmss')}.csv`)
    link.style.visibility = 'hidden'
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
  }

  // 匯出 XLSX
  const exportToXLSX = () => {
    const data = prepareExportData()
    if (data.length === 0) {
      return
    }

    // 建立工作簿
    const wb = XLSX.utils.book_new()
    
    // 將資料轉換為工作表
    const ws = XLSX.utils.json_to_sheet(data)
    
    // 設定欄寬
    const colWidths = [
      { wch: 12 }, // 訪客姓名
      { wch: 20 }, // 訪客公司
      { wch: 25 }, // 訪客Email
      { wch: 15 }, // 訪客電話
      { wch: 20 }, // 會議名稱
      { wch: 15 }, // 會議室
      { wch: 20 }, // 會議時間
      { wch: 12 }, // 邀請者姓名
      { wch: 20 }, // 邀請者單位
      { wch: 15 }, // 邀請者職稱
      { wch: 25 }, // 邀請者Email
      { wch: 20 }, // 簽到時間
      { wch: 20 }, // 簽退時間
      { wch: 15 }, // 停留時間
    ]
    ws['!cols'] = colWidths
    
    // 將工作表加入工作簿
    XLSX.utils.book_append_sheet(wb, ws, '整理後資料表')
    
    // 匯出檔案
    XLSX.writeFile(wb, `整理後資料表_${format(new Date(), 'yyyyMMdd_HHmmss')}.xlsx`)
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-4">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>整理後資料表</CardTitle>
              <CardDescription>查看和管理依據 Google 日曆的簽到簽退紀錄</CardDescription>
            </div>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" size="sm">
                  <Download className="h-4 w-4 mr-2" />
                  匯出
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={exportToCSV}>
                  <FileText className="h-4 w-4 mr-2" />
                  匯出為 CSV
                </DropdownMenuItem>
                <DropdownMenuItem onClick={exportToXLSX}>
                  <FileSpreadsheet className="h-4 w-4 mr-2" />
                  匯出為 XLSX
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </CardHeader>
        <CardContent>
          {error && (
            <Alert variant="destructive" className="mb-4">
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {/* 篩選區域 */}
          {!loading && (
            <div className="mb-6 space-y-4">
              <div className="flex items-center gap-2">
                <Filter className="h-4 w-4 text-muted-foreground" />
                <h3 className="text-sm font-medium">篩選條件</h3>
                {hasActiveFilters && (
                  <span className="text-xs text-muted-foreground">
                    （已篩選出 {filteredLogs.length} 筆資料）
                  </span>
                )}
              </div>
              <div className="rounded-lg border bg-muted/30 p-4">
                <div className="space-y-4">
                  <div className="flex flex-wrap gap-4">
                    <div className="space-y-2 min-w-[200px] max-w-[220px] flex-1">
                      <Label htmlFor="filter-visitor" className="text-xs font-medium">
                        訪客資訊
                      </Label>
                      <Input
                        id="filter-visitor"
                        placeholder="搜尋訪客姓名、公司、Email..."
                        value={filterVisitor}
                        onChange={(e) => setFilterVisitor(e.target.value)}
                        className="h-9"
                      />
                    </div>
                    <div className="space-y-2 min-w-[200px] max-w-[220px] flex-1">
                      <Label htmlFor="filter-meeting-name" className="text-xs font-medium">
                        會議名稱
                      </Label>
                      <Input
                        id="filter-meeting-name"
                        placeholder="搜尋會議名稱..."
                        value={filterMeetingName}
                        onChange={(e) => setFilterMeetingName(e.target.value)}
                        className="h-9"
                      />
                    </div>
                    <div className="space-y-2 min-w-[200px] max-w-[220px] flex-1">
                      <Label htmlFor="filter-inviter" className="text-xs font-medium">
                        邀請者資訊
                      </Label>
                      <Input
                        id="filter-inviter"
                        placeholder="搜尋邀請者姓名、單位..."
                        value={filterInviter}
                        onChange={(e) => setFilterInviter(e.target.value)}
                        className="h-9"
                      />
                    </div>
                    <div className="space-y-2 min-w-[200px] max-w-[220px] flex-1">
                      <Label htmlFor="filter-meeting-room" className="text-xs font-medium">
                        會議室
                      </Label>
                      <select
                        id="filter-meeting-room"
                        value={filterMeetingRoom}
                        onChange={(e) => setFilterMeetingRoom(e.target.value)}
                        className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        <option value="">全部</option>
                        {meetingRooms.map(room => (
                          <option key={room} value={room}>{room}</option>
                        ))}
                      </select>
                    </div>
                    <div className="space-y-2 min-w-[200px] max-w-[280px] flex-1">
                      <Label className="text-xs font-medium">
                        日期範圍
                      </Label>
                      <Popover>
                        <PopoverTrigger asChild>
                          <Button
                            variant="outline"
                            className="h-9 w-full justify-start text-left font-normal"
                          >
                            <CalendarIcon className="mr-2 h-4 w-4" />
                            {formatDateRange()}
                          </Button>
                        </PopoverTrigger>
                        <PopoverContent className="w-auto p-0" align="start">
                          <Calendar
                            mode="range"
                            defaultMonth={dateRange?.from}
                            selected={dateRange}
                            onSelect={setDateRange}
                            numberOfMonths={2}
                          />
                        </PopoverContent>
                      </Popover>
                    </div>
                    {hasActiveFilters && (
                      <div className="space-y-2 flex items-end ml-auto">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={clearAllFilters}
                          className="h-9"
                        >
                          <X className="h-3 w-3 mr-1" />
                          清除篩選
                        </Button>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
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
                    <th className="text-left p-4 font-medium">訪客資訊</th>
                    <th className="text-left p-4 font-medium">會議名稱</th>
                    <th className="text-left p-4 font-medium">會議室</th>
                    <th className="text-left p-4 font-medium">會議時間</th>
                    <th className="text-left p-4 font-medium">邀請者資訊</th>
                    <th className="text-left p-4 font-medium">簽到時間</th>
                    <th className="text-left p-4 font-medium">簽退時間</th>
                    <th className="text-left p-4 font-medium">停留時間</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredLogs.length === 0 ? (
                    <tr>
                      <td colSpan={8} className="text-center p-8 text-muted-foreground">
                        {visitorLogs.length === 0 ? '尚無資料' : '沒有符合篩選條件的資料'}
                      </td>
                    </tr>
                  ) : (
                    paginatedLogs.map((log, index) => (
                      <tr key={`${log.checkinTimestamp}-${index}`} className="border-b hover:bg-muted/50">
                        <td className="p-4">
                          <div className="space-y-1">
                            <div className="font-medium">{log.vistorName}</div>
                            <div className="text-sm">{log.vistorDept}</div>
                            <div className="text-sm text-muted-foreground">{log.vistorEmail}</div>
                            <div className="text-sm text-muted-foreground">{log.vistorPhone}</div>
                          </div>
                        </td>
                        <td className="p-4">{log.meetingName}</td>
                        <td className="p-4">{log.meetingRoom}</td>
                        <td className="p-4 whitespace-nowrap">{log.meetingTime}</td>
                        <td className="p-4">
                          <div className="space-y-1">
                            <div className="font-medium">{log.inviterName}</div>
                            <div className="text-sm">{log.inviterDept}</div>
                            <div className="text-sm text-muted-foreground">{log.inviterTitle}</div>
                            <div className="text-sm text-muted-foreground">{log.inviterEmail}</div>
                          </div>
                        </td>
                        <td className="p-4 whitespace-nowrap">{formatDateTime(log.checkinTimestamp)}</td>
                        <td className="p-4 whitespace-nowrap">{formatDateTime(log.checkoutTimestamp)}</td>
                        <td className="p-4 whitespace-nowrap">
                          {log.checkoutTimestamp 
                            ? calculateDuration(log.checkinTimestamp, log.checkoutTimestamp)
                            : '-'
                          }
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
              {filteredLogs.length > 0 && (
                <Pagination
                  currentPage={currentPage}
                  totalPages={totalPages}
                  onPageChange={setCurrentPage}
                  totalItems={filteredLogs.length}
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
