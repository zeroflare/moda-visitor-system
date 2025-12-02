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
  getCounters,
  createCounter,
  getCounterById,
  updateCounter,
  deleteCounter,
  type Counter,
} from '@/services/counterApi'

export function CounterManagement() {
  const [counters, setCounters] = useState<Counter[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const [selectedCounter, setSelectedCounter] = useState<Counter | null>(null)
  const [formData, setFormData] = useState({
    name: '',
  })
  const [formError, setFormError] = useState<string>('')
  const [submitting, setSubmitting] = useState(false)

  // 載入櫃檯列表
  const loadCounters = async () => {
    try {
      setLoading(true)
      setError('')
      const data = await getCounters()
      setCounters(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : '載入櫃檯列表失敗')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadCounters()
  }, [])

  // 開啟新增對話框
  const handleOpenCreateDialog = () => {
    setFormData({ name: '' })
    setFormError('')
    setIsCreateDialogOpen(true)
  }

  // 開啟編輯對話框
  const handleOpenEditDialog = async (counter: Counter) => {
    try {
      if (!counter.id) return
      setFormError('')
      const counterData = await getCounterById(counter.id)
      setSelectedCounter(counterData)
      setFormData({
        name: counterData.name || '',
      })
      setIsEditDialogOpen(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : '載入櫃檯資料失敗')
    }
  }

  // 開啟刪除對話框
  const handleOpenDeleteDialog = (counter: Counter) => {
    setSelectedCounter(counter)
    setIsDeleteDialogOpen(true)
  }

  // 提交新增表單
  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError('')
    setSubmitting(true)

    try {
      if (!formData.name.trim()) {
        setFormError('請填寫櫃檯名稱')
        return
      }

      await createCounter({ name: formData.name.trim() })
      setIsCreateDialogOpen(false)
      await loadCounters()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : '新增櫃檯失敗')
    } finally {
      setSubmitting(false)
    }
  }

  // 提交編輯表單
  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedCounter || !selectedCounter.id) return

    setFormError('')
    setSubmitting(true)

    try {
      if (!formData.name.trim()) {
        setFormError('請填寫櫃檯名稱')
        return
      }

      await updateCounter(selectedCounter.id, {
        name: formData.name.trim(),
      })
      setIsEditDialogOpen(false)
      setSelectedCounter(null)
      await loadCounters()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : '更新櫃檯失敗')
    } finally {
      setSubmitting(false)
    }
  }

  // 確認刪除
  const handleDeleteConfirm = async () => {
    if (!selectedCounter || !selectedCounter.id) return

    try {
      await deleteCounter(selectedCounter.id)
      setIsDeleteDialogOpen(false)
      setSelectedCounter(null)
      await loadCounters()
    } catch (err) {
      setError(err instanceof Error ? err.message : '刪除櫃檯失敗')
      setIsDeleteDialogOpen(false)
    }
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-4">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>櫃檯管理</CardTitle>
              <CardDescription>管理系統櫃檯資訊</CardDescription>
            </div>
            <Button onClick={handleOpenCreateDialog}>
              <Plus className="h-4 w-4 mr-2" />
              新增櫃檯
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
                    <th className="text-left p-4 font-medium">櫃檯 ID</th>
                    <th className="text-left p-4 font-medium">櫃檯名稱</th>
                    <th className="text-right p-4 font-medium">操作</th>
                  </tr>
                </thead>
                <tbody>
                  {counters.length === 0 ? (
                    <tr>
                      <td colSpan={3} className="text-center p-8 text-muted-foreground">
                        尚無櫃檯資料
                      </td>
                    </tr>
                  ) : (
                    counters.map((counter) => (
                      <tr key={counter.id || Math.random()} className="border-b hover:bg-muted/50">
                        <td className="p-4 font-mono text-sm">{counter.id || '-'}</td>
                        <td className="p-4">{counter.name || '-'}</td>
                        <td className="p-4">
                          <div className="flex items-center justify-end gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleOpenEditDialog(counter)}
                              disabled={!counter.id}
                            >
                              <Pencil className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="destructive"
                              size="sm"
                              onClick={() => handleOpenDeleteDialog(counter)}
                              disabled={!counter.id}
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

      {/* 新增櫃檯對話框 */}
      <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>新增櫃檯</DialogTitle>
            <DialogDescription>請填寫櫃檯資訊以新增櫃檯</DialogDescription>
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
                <Label htmlFor="create-name">櫃檯名稱 *</Label>
                <Input
                  id="create-name"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="請輸入櫃檯名稱"
                  required
                />
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

      {/* 編輯櫃檯對話框 */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>編輯櫃檯</DialogTitle>
            <DialogDescription>修改櫃檯資訊</DialogDescription>
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
                <Label htmlFor="edit-name">櫃檯名稱 *</Label>
                <Input
                  id="edit-name"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="請輸入櫃檯名稱"
                  required
                />
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
              您確定要刪除櫃檯「{selectedCounter?.name}」嗎？
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

