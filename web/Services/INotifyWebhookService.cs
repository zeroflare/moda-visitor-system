using web.Models;

namespace web.Services;

public interface INotifyWebhookService
{
    Task<IEnumerable<NotifyWebhook>> GetAllNotifyWebhooksAsync();
    Task<NotifyWebhook?> GetNotifyWebhookByDeptAsync(string dept);
    Task<NotifyWebhook> CreateNotifyWebhookAsync(NotifyWebhook notifyWebhook);
    Task<NotifyWebhook?> UpdateNotifyWebhookAsync(string dept, NotifyWebhook notifyWebhook);
    Task<bool> DeleteNotifyWebhookAsync(string dept);
}

