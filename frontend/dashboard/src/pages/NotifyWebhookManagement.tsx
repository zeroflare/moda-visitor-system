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
  getNotifyWebhooks,
  createNotifyWebhook,
  getNotifyWebhookByDept,
  updateNotifyWebhook,
  deleteNotifyWebhook,
  type NotifyWebhook,
} from '@/services/notifyWebhookApi'

export function NotifyWebhookManagement() {
  const [webhooks, setWebhooks] = useState<NotifyWebhook[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const [selectedWebhook, setSelectedWebhook] = useState<NotifyWebhook | null>(null)
  const [formData, setFormData] = useState({
    dept: '',
    type: 'googlechat',
    webhook: '',
  })
  const [formError, setFormError] = useState<string>('')
  const [submitting, setSubmitting] = useState(false)

  // 載入 webhook 列表
  const loadWebhooks = async () => {
    try {
      setLoading(true)
      setError('')
      const data = await getNotifyWebhooks()
      setWebhooks(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : '載入 webhook 列表失敗')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadWebhooks()
  }, [])

  // 開啟新增對話框
  const handleOpenCreateDialog = () => {
    setFormData({ dept: '', type: 'googlechat', webhook: '' })
    setFormError('')
    setIsCreateDialogOpen(true)
  }

  // 開啟編輯對話框
  const handleOpenEditDialog = async (webhook: NotifyWebhook) => {
    try {
      if (!webhook.dept) return
      setFormError('')
      const webhookData = await getNotifyWebhookByDept(webhook.dept)
      setSelectedWebhook(webhookData)
      setFormData({
        dept: webhookData.dept || '',
        type: webhookData.type || 'googlechat',
        webhook: webhookData.webhook || '',
      })
      setIsEditDialogOpen(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : '載入 webhook 資料失敗')
    }
  }

  // 開啟刪除對話框
  const handleOpenDeleteDialog = (webhook: NotifyWebhook) => {
    setSelectedWebhook(webhook)
    setIsDeleteDialogOpen(true)
  }

  // 提交新增表單
  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError('')
    setSubmitting(true)

    try {
      if (!formData.dept.trim()) {
        setFormError('請填寫單位')
        return
      }

      if (!formData.webhook.trim()) {
        setFormError('請填寫 webhook 路徑')
        return
      }

      await createNotifyWebhook({
        dept: formData.dept.trim(),
        type: 'googlechat', // 固定為 googlechat
        webhook: formData.webhook.trim(),
      })
      setIsCreateDialogOpen(false)
      await loadWebhooks()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : '新增 webhook 失敗')
    } finally {
      setSubmitting(false)
    }
  }

  // 提交編輯表單
  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedWebhook || !selectedWebhook.dept) return

    setFormError('')
    setSubmitting(true)

    try {
      if (!formData.webhook.trim()) {
        setFormError('請填寫 webhook 路徑')
        return
      }

      await updateNotifyWebhook(selectedWebhook.dept, {
        type: 'googlechat', // 固定為 googlechat
        webhook: formData.webhook.trim(),
      })
      setIsEditDialogOpen(false)
      setSelectedWebhook(null)
      await loadWebhooks()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : '更新 webhook 失敗')
    } finally {
      setSubmitting(false)
    }
  }

  // 確認刪除
  const handleDeleteConfirm = async () => {
    if (!selectedWebhook || !selectedWebhook.dept) return

    try {
      await deleteNotifyWebhook(selectedWebhook.dept)
      setIsDeleteDialogOpen(false)
      setSelectedWebhook(null)
      await loadWebhooks()
    } catch (err) {
      setError(err instanceof Error ? err.message : '刪除 webhook 失敗')
      setIsDeleteDialogOpen(false)
    }
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-4">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>通知 Webhook 管理</CardTitle>
              <CardDescription>管理系統通知 webhook 設定</CardDescription>
            </div>
            <Button onClick={handleOpenCreateDialog}>
              <Plus className="h-4 w-4 mr-2" />
              新增 Webhook
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
                    <th className="text-left p-4 font-medium">單位</th>
                    <th className="text-left p-4 font-medium">類型</th>
                    <th className="text-left p-4 font-medium">Webhook 路徑</th>
                    <th className="text-right p-4 font-medium">操作</th>
                  </tr>
                </thead>
                <tbody>
                  {webhooks.length === 0 ? (
                    <tr>
                      <td colSpan={4} className="text-center p-8 text-muted-foreground">
                        尚無 webhook 資料
                      </td>
                    </tr>
                  ) : (
                    webhooks.map((webhook) => (
                      <tr key={webhook.dept} className="border-b hover:bg-muted/50">
                        <td className="p-4">{webhook.dept || '-'}</td>
                        <td className="p-4">{webhook.type || '-'}</td>
                        <td className="p-4 font-mono text-sm break-all">{webhook.webhook || '-'}</td>
                        <td className="p-4">
                          <div className="flex items-center justify-end gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleOpenEditDialog(webhook)}
                              disabled={!webhook.dept}
                            >
                              <Pencil className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="destructive"
                              size="sm"
                              onClick={() => handleOpenDeleteDialog(webhook)}
                              disabled={!webhook.dept}
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

      {/* 新增 Webhook 對話框 */}
      <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>新增 Webhook</DialogTitle>
            <DialogDescription>請填寫 webhook 資訊以新增通知設定</DialogDescription>
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
                <Label htmlFor="create-dept">單位 *</Label>
                <Input
                  id="create-dept"
                  value={formData.dept}
                  onChange={(e) => setFormData({ ...formData, dept: e.target.value })}
                  placeholder="請輸入單位名稱"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="create-type">類型 *</Label>
                <Input
                  id="create-type"
                  value={formData.type}
                  disabled
                  className="bg-muted"
                />
                <p className="text-sm text-muted-foreground">類型固定為 googlechat</p>
              </div>
              <div className="space-y-2">
                <Label htmlFor="create-webhook">Webhook 路徑 *</Label>
                <Input
                  id="create-webhook"
                  value={formData.webhook}
                  onChange={(e) => setFormData({ ...formData, webhook: e.target.value })}
                  placeholder="請輸入 webhook URL"
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

      {/* 編輯 Webhook 對話框 */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>編輯 Webhook</DialogTitle>
            <DialogDescription>修改 webhook 資訊</DialogDescription>
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
                <Label htmlFor="edit-dept">單位</Label>
                <Input
                  id="edit-dept"
                  value={formData.dept}
                  disabled
                  className="bg-muted"
                />
                <p className="text-sm text-muted-foreground">單位無法修改</p>
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-type">類型 *</Label>
                <Input
                  id="edit-type"
                  value={formData.type}
                  disabled
                  className="bg-muted"
                />
                <p className="text-sm text-muted-foreground">類型固定為 googlechat</p>
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-webhook">Webhook 路徑 *</Label>
                <Input
                  id="edit-webhook"
                  value={formData.webhook}
                  onChange={(e) => setFormData({ ...formData, webhook: e.target.value })}
                  placeholder="請輸入 webhook URL"
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
              您確定要刪除單位「{selectedWebhook?.dept}」的 webhook 嗎？
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

