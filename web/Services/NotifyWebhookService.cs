using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class NotifyWebhookService : INotifyWebhookService
{
    private readonly ApplicationDbContext _context;

    public NotifyWebhookService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NotifyWebhook>> GetAllNotifyWebhooksAsync()
    {
        return await _context.NotifyWebhooks.ToListAsync();
    }

    public async Task<NotifyWebhook?> GetNotifyWebhookByDeptAsync(string dept)
    {
        return await _context.NotifyWebhooks.FindAsync(dept);
    }

    public async Task<NotifyWebhook?> GetNotifyWebhookByDeptAndTypeAsync(string dept, string type)
    {
        return await _context.NotifyWebhooks
            .FirstOrDefaultAsync(w => w.Dept == dept && w.Type == type);
    }

    public async Task<NotifyWebhook> CreateNotifyWebhookAsync(NotifyWebhook notifyWebhook)
    {
        _context.NotifyWebhooks.Add(notifyWebhook);
        await _context.SaveChangesAsync();
        return notifyWebhook;
    }

    public async Task<NotifyWebhook?> UpdateNotifyWebhookAsync(string dept, NotifyWebhook notifyWebhook)
    {
        var existingWebhook = await _context.NotifyWebhooks.FindAsync(dept);
        if (existingWebhook == null)
        {
            return null;
        }

        existingWebhook.Type = notifyWebhook.Type;
        existingWebhook.Webhook = notifyWebhook.Webhook;
        
        await _context.SaveChangesAsync();
        return existingWebhook;
    }

    public async Task<bool> DeleteNotifyWebhookAsync(string dept)
    {
        var webhook = await _context.NotifyWebhooks.FindAsync(dept);
        if (webhook == null)
        {
            return false;
        }

        _context.NotifyWebhooks.Remove(webhook);
        await _context.SaveChangesAsync();
        return true;
    }
}

