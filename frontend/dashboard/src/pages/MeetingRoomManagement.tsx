import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Plus, Pencil, Trash2, Loader2, AlertTriangle } from 'lucide-react'
import {
  getMeetingRooms,
  createMeetingRoom,
  getMeetingRoomById,
  updateMeetingRoom,
  deleteMeetingRoom,
  type MeetingRoomResponse,
} from '@/services/meetingRoomApi'
import { getCounters, type Counter } from '@/services/counterApi'

export function MeetingRoomManagement() {
  const [meetingRooms, setMeetingRooms] = useState<MeetingRoomResponse[]>([])
  const [counters, setCounters] = useState<Counter[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const [selectedMeetingRoom, setSelectedMeetingRoom] = useState<MeetingRoomResponse | null>(null)
  const [formData, setFormData] = useState({
    id: '',
    name: '',
    counterId: '',
  })
  const [formError, setFormError] = useState<string>('')
  const [submitting, setSubmitting] = useState(false)

  // 載入櫃檯列表
  const loadCounters = async () => {
    try {
      const data = await getCounters()
      setCounters(data)
    } catch (err) {
      console.error('載入櫃檯列表失敗:', err)
    }
  }

  // 載入會議室列表
  const loadMeetingRooms = async () => {
    try {
      setLoading(true)
      setError('')
      const data = await getMeetingRooms()
      setMeetingRooms(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : '載入會議室列表失敗')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadCounters()
    loadMeetingRooms()
  }, [])

  // 開啟新增對話框
  const handleOpenCreateDialog = () => {
    setFormData({ id: '', name: '', counterId: '' })
    setFormError('')
    setIsCreateDialogOpen(true)
  }

  // 開啟編輯對話框
  const handleOpenEditDialog = async (meetingRoom: MeetingRoomResponse) => {
    try {
      if (!meetingRoom.id) return
      setFormError('')
      const meetingRoomData = await getMeetingRoomById(meetingRoom.id)
      setSelectedMeetingRoom(meetingRoomData)
      setFormData({
        id: meetingRoomData.id || '',
        name: meetingRoomData.name || '',
        counterId: meetingRoomData.counterId || '',
      })
      setIsEditDialogOpen(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : '載入會議室資料失敗')
    }
  }

  // 開啟刪除對話框
  const handleOpenDeleteDialog = (meetingRoom: MeetingRoomResponse) => {
    setSelectedMeetingRoom(meetingRoom)
    setIsDeleteDialogOpen(true)
  }

  // 提交新增表單
  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError('')
    setSubmitting(true)

    try {
      if (!formData.id.trim()) {
        setFormError('請填寫會議室 ID')
        return
      }

      if (!formData.name.trim() || !formData.counterId) {
        setFormError('請填寫所有欄位')
        return
      }

      await createMeetingRoom({
        id: formData.id.trim(),
        name: formData.name.trim(),
        counterId: formData.counterId,
      })
      setIsCreateDialogOpen(false)
      await loadMeetingRooms()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : '新增會議室失敗')
    } finally {
      setSubmitting(false)
    }
  }

  // 提交編輯表單
  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedMeetingRoom || !selectedMeetingRoom.id) return

    setFormError('')
    setSubmitting(true)

    try {
      if (!formData.name.trim() || !formData.counterId) {
        setFormError('請填寫所有欄位')
        return
      }

      await updateMeetingRoom(selectedMeetingRoom.id, {
        name: formData.name.trim(),
        counterId: formData.counterId,
      })
      setIsEditDialogOpen(false)
      setSelectedMeetingRoom(null)
      await loadMeetingRooms()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : '更新會議室失敗')
    } finally {
      setSubmitting(false)
    }
  }

  // 確認刪除
  const handleDeleteConfirm = async () => {
    if (!selectedMeetingRoom || !selectedMeetingRoom.id) return

    try {
      await deleteMeetingRoom(selectedMeetingRoom.id)
      setIsDeleteDialogOpen(false)
      setSelectedMeetingRoom(null)
      await loadMeetingRooms()
    } catch (err) {
      setError(err instanceof Error ? err.message : '刪除會議室失敗')
      setIsDeleteDialogOpen(false)
    }
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-4">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>會議室管理</CardTitle>
              <CardDescription>管理系統會議室資訊</CardDescription>
            </div>
            <Button onClick={handleOpenCreateDialog}>
              <Plus className="h-4 w-4 mr-2" />
              新增會議室
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
              <table className="w-full border-collapse">
                <thead>
                  <tr className="border-b">
                    <th className="text-left p-4 font-medium">會議室 ID</th>
                    <th className="text-left p-4 font-medium">會議室名稱</th>
                    <th className="text-left p-4 font-medium">櫃檯 ID</th>
                    <th className="text-left p-4 font-medium">櫃檯名稱</th>
                    <th className="text-right p-4 font-medium">操作</th>
                  </tr>
                </thead>
                <tbody>
                  {meetingRooms.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="text-center p-8 text-muted-foreground">
                        尚無會議室資料
                      </td>
                    </tr>
                  ) : (
                    meetingRooms.map((meetingRoom) => (
                      <tr
                        key={meetingRoom.id || Math.random()}
                        className="border-b hover:bg-muted/50"
                      >
                        <td className="p-4 font-mono text-sm">{meetingRoom.id || '-'}</td>
                        <td className="p-4">{meetingRoom.name || '-'}</td>
                        <td className="p-4 font-mono text-sm">{meetingRoom.counterId || '-'}</td>
                        <td className="p-4">{meetingRoom.counterName || '-'}</td>
                        <td className="p-4">
                          <div className="flex items-center justify-end gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleOpenEditDialog(meetingRoom)}
                              disabled={!meetingRoom.id}
                            >
                              <Pencil className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="destructive"
                              size="sm"
                              onClick={() => handleOpenDeleteDialog(meetingRoom)}
                              disabled={!meetingRoom.id}
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* 新增會議室對話框 */}
      <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>新增會議室</DialogTitle>
            <DialogDescription>請填寫會議室資訊以新增會議室</DialogDescription>
          </DialogHeader>
          <form onSubmit={handleCreateSubmit}>
            <div className="space-y-4 py-4">
              {formError && (
                <Alert variant="destructive">
                  <AlertTriangle className="h-4 w-4" />
                  <AlertDescription>{formError}</AlertDescription>
                </Alert>
              )}
              <div className="space-y-2">
                <Label htmlFor="create-id">會議室 ID *</Label>
                <Input
                  id="create-id"
                  value={formData.id}
                  onChange={(e) => setFormData({ ...formData, id: e.target.value })}
                  placeholder="請輸入會議室 ID"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="create-name">會議室名稱 *</Label>
                <Input
                  id="create-name"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="請輸入會議室名稱"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="create-counter">櫃檯 *</Label>
                <select
                  id="create-counter"
                  value={formData.counterId}
                  onChange={(e) => setFormData({ ...formData, counterId: e.target.value })}
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                  required
                >
                  <option value="">請選擇櫃檯</option>
                  {counters.map((counter) => (
                    <option key={counter.id} value={counter.id || ''}>
                      {counter.name || counter.id}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsCreateDialogOpen(false)}
                disabled={submitting}
              >
                取消
              </Button>
              <Button type="submit" disabled={submitting}>
                {submitting ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    處理中...
                  </>
                ) : (
                  '新增'
                )}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* 編輯會議室對話框 */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>編輯會議室</DialogTitle>
            <DialogDescription>修改會議室資訊</DialogDescription>
          </DialogHeader>
          <form onSubmit={handleEditSubmit}>
            <div className="space-y-4 py-4">
              {formError && (
                <Alert variant="destructive">
                  <AlertTriangle className="h-4 w-4" />
                  <AlertDescription>{formError}</AlertDescription>
                </Alert>
              )}
              <div className="space-y-2">
                <Label htmlFor="edit-name">會議室名稱 *</Label>
                <Input
                  id="edit-name"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="請輸入會議室名稱"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-counter">櫃檯 *</Label>
                <select
                  id="edit-counter"
                  value={formData.counterId}
                  onChange={(e) => setFormData({ ...formData, counterId: e.target.value })}
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                  required
                >
                  <option value="">請選擇櫃檯</option>
                  {counters.map((counter) => (
                    <option key={counter.id} value={counter.id || ''}>
                      {counter.name || counter.id}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsEditDialogOpen(false)}
                disabled={submitting}
              >
                取消
              </Button>
              <Button type="submit" disabled={submitting}>
                {submitting ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    處理中...
                  </>
                ) : (
                  '儲存'
                )}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* 刪除確認對話框 */}
      <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>確認刪除</DialogTitle>
            <DialogDescription>
              您確定要刪除會議室「{selectedMeetingRoom?.name}」嗎？
              <br />
              此操作無法復原。
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsDeleteDialogOpen(false)}
            >
              取消
            </Button>
            <Button type="button" variant="destructive" onClick={handleDeleteConfirm}>
              刪除
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

