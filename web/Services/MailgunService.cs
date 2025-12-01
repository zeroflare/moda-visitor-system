using System.Text;
using System.Net.Http.Headers;

namespace web.Services;

public class MailgunService : IMailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailgunService> _logger;

    public MailgunService(HttpClient httpClient, IConfiguration configuration, ILogger<MailgunService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOTPAsync(string email, string otp)
    {
        var apiKey = _configuration["Mailgun:ApiKey"];
        var domain = _configuration["Mailgun:Domain"];
        var url = $"https://api.mailgun.net/v3/{domain}/messages";

        var content = new MultipartFormDataContent();
        content.Add(new StringContent($"數位發展部訪客系統<noreply@{domain}>"), "from");
        content.Add(new StringContent(email), "to");
        content.Add(new StringContent("訪客系統電子信箱驗證碼"), "subject");
        content.Add(new StringContent($"您好！\n\n您的驗證碼是：{otp}\n請於 10 分鐘內輸入完成驗證。"), "text");
        content.Add(new StringContent($"<p>您好！</p><p>您的驗證碼是：<strong>{otp}</strong></p><p>請於 10 分鐘內輸入完成驗證。</p>"), "html");

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", 
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}")));
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            _logger.LogError("Mailgun error: {Error}", errorText);
            throw new HttpRequestException("寄信失敗，請稍後再試");
        }
    }

    public async Task SendRegisterInvitationAsync(string email, string token, string registerUrl)
    {
        var apiKey = _configuration["Mailgun:ApiKey"];
        var domain = _configuration["Mailgun:Domain"];
        var url = $"https://api.mailgun.net/v3/{domain}/messages";

        var content = new MultipartFormDataContent();
        content.Add(new StringContent($"數位發展部訪客系統<noreply@{domain}>"), "from");
        content.Add(new StringContent(email), "to");
        content.Add(new StringContent("訪客系統註冊邀請"), "subject");
        content.Add(new StringContent($"您好！\n\n您收到了一封註冊邀請信。\n\n請點擊以下連結進行註冊：\n{registerUrl}\n\n此連結有效期限為兩天。"), "text");
        content.Add(new StringContent($"<p>您好！</p><p>您收到了一封註冊邀請信。</p><p>請點擊以下連結進行註冊：</p><p><a href=\"{registerUrl}\">{registerUrl}</a></p><p>此連結有效期限為兩天。</p>"), "html");

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", 
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}")));
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            _logger.LogError("Mailgun error: {Error}", errorText);
            throw new HttpRequestException("寄信失敗，請稍後再試");
        }
    }
}

