namespace web.Services;

public interface IGoogleChatService
{
    Task SendNotificationAsync(string webhookUrl, string message);
}

