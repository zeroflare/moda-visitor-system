using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace web.Services;

public class GoogleChatService : IGoogleChatService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleChatService> _logger;

    public GoogleChatService(HttpClient httpClient, ILogger<GoogleChatService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string webhookUrl, string message)
    {
        try
        {
            var payload = new
            {
                text = message
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(webhookUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError("Google Chat webhook 發送失敗: {StatusCode}, {Error}", response.StatusCode, errorText);
                throw new HttpRequestException($"Google Chat webhook 發送失敗: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "發送 Google Chat 通知錯誤");
            throw;
        }
    }
}

