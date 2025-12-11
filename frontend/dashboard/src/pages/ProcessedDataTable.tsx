import { useState, useEffect, useMemo } from 'react'
import { type DateRange } from 'react-day-picker'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Pagination } from '@/components/ui/pagination'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Button } from '@/components/ui/button'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { Calendar } from '@/components/ui/calendar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Loader2,
  AlertTriangle,
  X,
  Filter,
  CalendarIcon,
  Download,
  FileSpreadsheet,
  FileText,
} from 'lucide-react'
import { getVisitorLogs, type VisitorLog } from '@/services/visitorLogApi'
import { format } from 'date-fns'
import ExcelJS from '@zurmokeeper/exceljs'

const ITEMS_PER_PAGE = 10

export function ProcessedDataTable() {
  const [visitorLogs, setVisitorLogs] = useState<VisitorLog[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')
  const [currentPage, setCurrentPage] = useState(1)

  // 篩選狀態
  const [filterVisitor, setFilterVisitor] = useState<string>('')
  const [filterMeeting, setFilterMeeting] = useState<string>('')
  const [filterInviter, setFilterInviter] = useState<string>('')
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

  // 格式化會議時間（從 meetingStart 和 meetingEnd 組合）
  const formatMeetingTime = (
    meetingStart: string | null,
    meetingEnd: string | null
  ) => {
    if (!meetingStart && !meetingEnd) return '-'
    if (meetingStart && meetingEnd) {
      // 如果兩個時間在同一天，只顯示一次日期
      const startDate = new Date(meetingStart)
      const endDate = new Date(meetingEnd)
      if (startDate.toDateString() === endDate.toDateString()) {
        return `${format(startDate, 'yyyy/MM/dd HH:mm')} - ${format(endDate, 'HH:mm')}`
      }
      return `${format(startDate, 'yyyy/MM/dd HH:mm')} - ${format(endDate, 'yyyy/MM/dd HH:mm')}`
    }
    return meetingStart || meetingEnd || '-'
  }

  // 篩選邏輯
  const filteredLogs = useMemo(() => {
    return visitorLogs.filter(log => {
      // 訪客資訊篩選（姓名、公司、Email、電話）
      const visitorMatch =
        !filterVisitor ||
        (log.vistorName?.toLowerCase().includes(filterVisitor.toLowerCase()) ??
          false) ||
        (log.vistorDept?.toLowerCase().includes(filterVisitor.toLowerCase()) ??
          false) ||
        (log.vistorEmail?.toLowerCase().includes(filterVisitor.toLowerCase()) ??
          false) ||
        (log.vistorPhone?.includes(filterVisitor) ?? false)

      // 會議資訊篩選（會議名稱、會議室）
      const meetingMatch =
        !filterMeeting ||
        (log.meetingName?.toLowerCase().includes(filterMeeting.toLowerCase()) ??
          false) ||
        (log.meetingRoom?.toLowerCase().includes(filterMeeting.toLowerCase()) ??
          false)

      // 邀請者資訊篩選（姓名、單位、職稱、Email）
      const inviterMatch =
        !filterInviter ||
        (log.inviterName?.toLowerCase().includes(filterInviter.toLowerCase()) ??
          false) ||
        (log.inviterDept?.toLowerCase().includes(filterInviter.toLowerCase()) ??
          false) ||
        (log.inviterTitle
          ?.toLowerCase()
          .includes(filterInviter.toLowerCase()) ??
          false) ||
        (log.inviterEmail
          ?.toLowerCase()
          .includes(filterInviter.toLowerCase()) ??
          false)

      // 日期範圍篩選（根據會議開始時間）
      let dateMatch = true
      if (dateRange?.from || dateRange?.to) {
        if (!log.meetingStart) {
          // 如果沒有會議開始時間，則不匹配
          dateMatch = false
        } else {
          const meetingDate = new Date(log.meetingStart)
          meetingDate.setHours(0, 0, 0, 0)

          if (dateRange.from) {
            const startDate = new Date(dateRange.from)
            startDate.setHours(0, 0, 0, 0)
            if (meetingDate < startDate) {
              dateMatch = false
            }
          }

          if (dateRange.to && dateMatch) {
            const endDate = new Date(dateRange.to)
            endDate.setHours(23, 59, 59, 999)
            if (meetingDate > endDate) {
              dateMatch = false
            }
          }
        }
      }

      return visitorMatch && meetingMatch && inviterMatch && dateMatch
    })
  }, [visitorLogs, filterVisitor, filterMeeting, filterInviter, dateRange])

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
  }, [filterVisitor, filterMeeting, filterInviter, dateRange])

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
  const calculateDuration = (
    checkin: string | null,
    checkout: string | null
  ) => {
    if (!checkin) return '-'
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
  const hasActiveFilters =
    filterVisitor ||
    filterMeeting ||
    filterInviter ||
    dateRange?.from ||
    dateRange?.to

  // 清除所有篩選
  const clearAllFilters = () => {
    setFilterVisitor('')
    setFilterMeeting('')
    setFilterInviter('')
    setDateRange(undefined)
  }

  // 格式化日期範圍顯示
  const formatDateRange = () => {
    if (!dateRange?.from && !dateRange?.to) {
      return '選擇會議日期範圍'
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
    return '選擇會議日期範圍'
  }

  // 準備匯出資料
  const prepareExportData = () => {
    return filteredLogs.map(log => ({
      訪客姓名: log.vistorName || '-',
      訪客公司: log.vistorDept || '-',
      訪客Email: log.vistorEmail || '-',
      訪客電話: log.vistorPhone || '-',
      會議名稱: log.meetingName || '-',
      會議室: log.meetingRoom || '-',
      會議時間: formatMeetingTime(log.meetingStart, log.meetingEnd),
      邀請者姓名: log.inviterName || '-',
      邀請者單位: log.inviterDept || '-',
      邀請者職稱: log.inviterTitle || '-',
      邀請者Email: log.inviterEmail || '-',
      簽到時間: formatDateTime(log.checkinTimestamp),
      簽退時間: formatDateTime(log.checkoutTimestamp),
      停留時間: log.checkoutTimestamp
        ? calculateDuration(log.checkinTimestamp, log.checkoutTimestamp)
        : '-',
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
        headers
          .map(header => {
            const value = row[header as keyof typeof row]
            // 處理包含逗號、引號或換行的值
            if (
              typeof value === 'string' &&
              (value.includes(',') ||
                value.includes('"') ||
                value.includes('\n'))
            ) {
              return `"${value.replace(/"/g, '""')}"`
            }
            return value
          })
          .join(',')
      ),
    ].join('\n')

    // 加入 BOM 以支援中文
    const BOM = '\uFEFF'
    const blob = new Blob([BOM + csvContent], {
      type: 'text/csv;charset=utf-8;',
    })
    const link = document.createElement('a')
    const url = URL.createObjectURL(blob)
    link.setAttribute('href', url)
    link.setAttribute(
      'download',
      `整理後資料表_${format(new Date(), 'yyyyMMdd_HHmmss')}.csv`
    )
    link.style.visibility = 'hidden'
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
  }

  // 匯出 XLSX
  const exportToXLSX = async () => {
    const data = prepareExportData()
    if (data.length === 0) {
      return
    }

    // 建立工作簿
    const workbook = new ExcelJS.Workbook()
    const worksheet = workbook.addWorksheet('整理後資料表')

    // 設定欄寬
    worksheet.columns = [
      { width: 12 }, // 訪客姓名
      { width: 20 }, // 訪客公司
      { width: 25 }, // 訪客Email
      { width: 15 }, // 訪客電話
      { width: 20 }, // 會議名稱
      { width: 15 }, // 會議室
      { width: 20 }, // 會議時間
      { width: 12 }, // 邀請者姓名
      { width: 20 }, // 邀請者單位
      { width: 15 }, // 邀請者職稱
      { width: 25 }, // 邀請者Email
      { width: 20 }, // 簽到時間
      { width: 20 }, // 簽退時間
      { width: 15 }, // 停留時間
    ]

    // 加入標題列
    const headers = Object.keys(data[0])
    worksheet.addRow(headers)

    // 設定標題列樣式
    const headerRow = worksheet.getRow(1)
    headerRow.font = { bold: true }
    headerRow.fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: 'FFE0E0E0' },
    }

    // 加入資料
    data.forEach(row => {
      worksheet.addRow(Object.values(row))
    })

    // 匯出檔案
    const buffer = await workbook.xlsx.writeBuffer()
    const blob = new Blob([buffer], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `整理後資料表_${format(new Date(), 'yyyyMMdd_HHmmss')}.xlsx`
    link.style.visibility = 'hidden'
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-2 sm:p-4">
      <Card>
        <CardHeader className="space-y-4">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <CardTitle>整理後資料表</CardTitle>
              <CardDescription>
                查看和管理依據 Google 日曆的簽到簽退紀錄
              </CardDescription>
            </div>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  size="sm"
                  className="w-full sm:w-auto"
                >
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
        <CardContent className="px-2 sm:px-6">
          {error && (
            <Alert variant="destructive" className="mb-4">
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {/* 篩選區域 */}
          {!loading && (
            <div className="mb-4 sm:mb-6 space-y-3 sm:space-y-4">
              <div className="flex flex-wrap items-center gap-2">
                <Filter className="h-4 w-4 text-muted-foreground" />
                <h3 className="text-sm font-medium">篩選條件</h3>
                {hasActiveFilters && (
                  <span className="text-xs text-muted-foreground">
                    （已篩選出 {filteredLogs.length} 筆資料）
                  </span>
                )}
              </div>
              <div className="rounded-lg border bg-muted/30 p-3 sm:p-4">
                <div className="space-y-3 sm:space-y-4">
                  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
                    <div className="space-y-2">
                      <Label
                        htmlFor="filter-visitor"
                        className="text-xs font-medium"
                      >
                        訪客資訊
                      </Label>
                      <Input
                        id="filter-visitor"
                        placeholder="搜尋訪客姓名、公司、Email..."
                        value={filterVisitor}
                        onChange={e => setFilterVisitor(e.target.value)}
                        className="h-9"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label
                        htmlFor="filter-meeting"
                        className="text-xs font-medium"
                      >
                        會議資訊
                      </Label>
                      <Input
                        id="filter-meeting"
                        placeholder="搜尋會議名稱、會議室..."
                        value={filterMeeting}
                        onChange={e => setFilterMeeting(e.target.value)}
                        className="h-9"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label
                        htmlFor="filter-inviter"
                        className="text-xs font-medium"
                      >
                        邀請者資訊
                      </Label>
                      <Input
                        id="filter-inviter"
                        placeholder="搜尋邀請者姓名、單位..."
                        value={filterInviter}
                        onChange={e => setFilterInviter(e.target.value)}
                        className="h-9"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label className="text-xs font-medium">
                        會議日期範圍
                      </Label>
                      <Popover>
                        <PopoverTrigger asChild>
                          <Button
                            variant="outline"
                            className="h-9 w-full justify-start text-left font-normal"
                          >
                            <CalendarIcon className="mr-2 h-4 w-4" />
                            <span className="truncate">
                              {formatDateRange()}
                            </span>
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
                  </div>
                  {hasActiveFilters && (
                    <div className="flex justify-end">
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
          )}

          {loading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : (
            <div className="overflow-x-auto -mx-2 sm:mx-0">
              <table className="w-full border-collapse min-w-[1000px]">
                <thead>
                  <tr className="border-b">
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">
                      訪客資訊
                    </th>
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">
                      會議資訊
                    </th>
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">
                      邀請者資訊
                    </th>
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">
                      簽到時間
                    </th>
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">
                      簽退時間
                    </th>
                    <th className="text-left p-2 sm:p-3 lg:p-4 font-medium text-xs sm:text-sm">
                      停留時間
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {filteredLogs.length === 0 ? (
                    <tr>
                      <td
                        colSpan={6}
                        className="text-center p-6 sm:p-8 text-muted-foreground text-sm"
                      >
                        {visitorLogs.length === 0
                          ? '尚無資料'
                          : '沒有符合篩選條件的資料'}
                      </td>
                    </tr>
                  ) : (
                    paginatedLogs.map((log, index) => (
                      <tr
                        key={`${log.checkinTimestamp || index}-${index}`}
                        className="border-b hover:bg-muted/50"
                      >
                        <td className="p-2 sm:p-3 lg:p-4">
                          <div className="space-y-0.5 sm:space-y-1">
                            <div className="font-medium text-xs sm:text-sm">
                              {log.vistorName || '-'}
                            </div>
                            <div className="text-xs">
                              {log.vistorDept || '-'}
                            </div>
                            <div className="text-xs text-muted-foreground truncate">
                              {log.vistorEmail || '-'}
                            </div>
                            <div className="text-xs text-muted-foreground">
                              {log.vistorPhone || '-'}
                            </div>
                          </div>
                        </td>
                        <td className="p-2 sm:p-3 lg:p-4">
                          <div className="space-y-0.5 sm:space-y-1">
                            <div className="font-medium text-xs sm:text-sm">
                              {log.meetingName || '-'}
                            </div>
                            <div className="text-xs text-muted-foreground">
                              {log.meetingRoom || '-'}
                            </div>
                            <div className="text-xs text-muted-foreground whitespace-nowrap">
                              {formatMeetingTime(
                                log.meetingStart,
                                log.meetingEnd
                              )}
                            </div>
                          </div>
                        </td>
                        <td className="p-2 sm:p-3 lg:p-4">
                          <div className="space-y-0.5 sm:space-y-1">
                            <div className="font-medium text-xs sm:text-sm">
                              {log.inviterName || '-'}
                            </div>
                            <div className="text-xs">
                              {log.inviterDept || '-'}
                            </div>
                            <div className="text-xs text-muted-foreground">
                              {log.inviterTitle || '-'}
                            </div>
                            <div className="text-xs text-muted-foreground truncate">
                              {log.inviterEmail || '-'}
                            </div>
                          </div>
                        </td>
                        <td className="p-2 sm:p-3 lg:p-4 whitespace-nowrap text-xs sm:text-sm">
                          {formatDateTime(log.checkinTimestamp)}
                        </td>
                        <td className="p-2 sm:p-3 lg:p-4 whitespace-nowrap text-xs sm:text-sm">
                          {formatDateTime(log.checkoutTimestamp)}
                        </td>
                        <td className="p-2 sm:p-3 lg:p-4 whitespace-nowrap text-xs sm:text-sm">
                          {log.checkoutTimestamp
                            ? calculateDuration(
                                log.checkinTimestamp,
                                log.checkoutTimestamp
                              )
                            : '-'}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
              {filteredLogs.length > 0 && (
                <div className="mt-4">
                  <Pagination
                    currentPage={currentPage}
                    totalPages={totalPages}
                    onPageChange={setCurrentPage}
                    totalItems={filteredLogs.length}
                    itemsPerPage={ITEMS_PER_PAGE}
                  />
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
