using System.Text;
using System.Net.Http.Headers;
using System.Net;
using System.Net.Mail;

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

        // 驗證和清理 email 地址
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email 地址不能為空", nameof(email));
        }
        
        MailAddress validatedEmail;
        try
        {
            validatedEmail = new MailAddress(email);
        }
        catch (FormatException)
        {
            _logger.LogError("Invalid email format: {Email}", email);
            throw new ArgumentException("無效的 Email 地址格式", nameof(email));
        }
        
        var safeEmail = validatedEmail.Address;

        // HTML 轉義 OTP 以防止 XSS 攻擊
        var safeOtp = WebUtility.HtmlEncode(otp);

        var content = new MultipartFormDataContent();
        content.Add(new StringContent($"數位發展部訪客系統<visitor@{domain}>"), "from");
        content.Add(new StringContent(safeEmail), "to");
        content.Add(new StringContent("訪客系統電子信箱驗證碼"), "subject");
        content.Add(new StringContent($"您好！\n\n您的驗證碼是：{otp}\n請於 10 分鐘內輸入完成驗證。"), "text");
        content.Add(new StringContent($"<p>您好！</p><p>您的驗證碼是：<strong>{safeOtp}</strong></p><p>請於 10 分鐘內輸入完成驗證。</p>"), "html");

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

        // --- Begin: Validate and sanitize registerUrl ---
        if (!Uri.TryCreate(registerUrl, UriKind.Absolute, out var parsedRegisterUri))
        {
            _logger.LogError("registerUrl is not a valid absolute URI");
            throw new ArgumentException("無效的註冊連結");
        }
        // Only allow HTTP/S
        if (parsedRegisterUri.Scheme != Uri.UriSchemeHttp && parsedRegisterUri.Scheme != Uri.UriSchemeHttps)
        {
            _logger.LogError("registerUrl scheme not allowed");
            throw new ArgumentException("註冊連結協議無效");
        }
        // Only allow configured host
        var allowedHost = (_configuration["BaseUrl"] != null)
            ? new Uri(_configuration["BaseUrl"]).Host
            : null;
        if (allowedHost != null && !string.Equals(parsedRegisterUri.Host, allowedHost, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("registerUrl host not allowed");
            throw new ArgumentException("註冊連結主機無效");
        }
        // --- End: Validate and sanitize registerUrl ---

        // 驗證和清理 email 地址
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email 地址不能為空", nameof(email));
        }
        
        // 使用 MailAddress 驗證 email 格式，這會自動清理和驗證地址
        MailAddress validatedEmail;
        try
        {
            validatedEmail = new MailAddress(email);
        }
        catch (FormatException)
        {
            _logger.LogError("Invalid email format: {Email}", email);
            throw new ArgumentException("無效的 Email 地址格式", nameof(email));
        }
        
        // 使用驗證後的 email 地址（只使用地址部分，不包含顯示名稱）
        var safeEmail = validatedEmail.Address;

        var safeRegisterUrl = WebUtility.HtmlEncode(registerUrl); // Encode for HTML context
        var content = new MultipartFormDataContent();
        content.Add(new StringContent($"數位發展部<visitor@{domain}>"), "from");
        content.Add(new StringContent(safeEmail), "to");
        content.Add(new StringContent("數位發展部訪客系統 - 訪客資料填寫通知"), "subject");
        content.Add(new StringContent($"您好，\n\n您即將參與數位發展部的會議，為完成訪客登記程序，請您填寫相關資料。\n\n請點擊以下連結完成資料填寫：\n{safeRegisterUrl}\n\n此連結將於 48 小時後失效，請儘早完成填寫。\n\n如有任何問題，請聯繫會議主辦單位。\n\n此為系統自動發送信件，請勿直接回覆。\n\n數位發展部"), "text");
        content.Add(new StringContent($"<div style=\"font-family: Arial, sans-serif; line-height: 1.6; color: #333;\">" +
            $"<p>您好，</p>" +
            $"<p>您即將參與數位發展部的會議，為完成訪客登記程序，請您填寫相關資料。</p>" +
            $"<p>請點擊以下連結完成資料填寫：</p>" +
            $"<p style=\"margin: 20px 0;\"><a href=\"{safeRegisterUrl}\" style=\"display: inline-block; padding: 12px 24px; background-color: #0066cc; color: #ffffff; text-decoration: none; border-radius: 4px;\">填寫訪客資料</a></p>" +
            $"<p style=\"color: #666; font-size: 14px;\">或複製以下網址至瀏覽器開啟：<br>{safeRegisterUrl}</p>" +
            $"<p style=\"color: #999; font-size: 12px; margin-top: 30px;\">此連結將於 48 小時後失效，請儘早完成填寫。</p>" +
            $"<p style=\"color: #999; font-size: 12px;\">如有任何問題，請聯繫會議主辦單位。</p>" +
            $"<hr style=\"border: none; border-top: 1px solid #eee; margin: 30px 0;\">" +
            $"<p style=\"color: #999; font-size: 11px;\">此為系統自動發送信件，請勿直接回覆。<br>數位發展部</p>" +
            $"</div>"), "html");

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

    public async Task SendCheckinNotificationAsync(string inviterEmail, string visitorName, string visitorEmail, string meetingName, string meetingRoom, string checkinTime)
    {
        var apiKey = _configuration["Mailgun:ApiKey"];
        var domain = _configuration["Mailgun:Domain"];
        var url = $"https://api.mailgun.net/v3/{domain}/messages";

        // 驗證和清理 email 地址
        if (string.IsNullOrWhiteSpace(inviterEmail))
        {
            throw new ArgumentException("Email 地址不能為空", nameof(inviterEmail));
        }
        
        MailAddress validatedEmail;
        try
        {
            validatedEmail = new MailAddress(inviterEmail);
        }
        catch (FormatException)
        {
            _logger.LogError("Invalid email format: {Email}", inviterEmail);
            throw new ArgumentException("無效的 Email 地址格式", nameof(inviterEmail));
        }
        
        var safeInviterEmail = validatedEmail.Address;

        // HTML 轉義用戶輸入以防止 XSS 攻擊
        var safeVisitorName = WebUtility.HtmlEncode(visitorName);
        var safeVisitorEmail = WebUtility.HtmlEncode(visitorEmail);
        var safeMeetingName = WebUtility.HtmlEncode(meetingName);
        var safeMeetingRoom = WebUtility.HtmlEncode(meetingRoom);
        var safeCheckinTime = WebUtility.HtmlEncode(checkinTime);

        var content = new MultipartFormDataContent();
        content.Add(new StringContent($"數位發展部訪客系統<visitor@{domain}>"), "from");
        content.Add(new StringContent(safeInviterEmail), "to");
        content.Add(new StringContent("訪客簽到通知"), "subject");
        content.Add(new StringContent($"您好！\n\n有訪客已完成簽到：\n\n訪客姓名：{visitorName}\n訪客信箱：{visitorEmail}\n會議名稱：{meetingName}\n會議室：{meetingRoom}\n簽到時間：{checkinTime}"), "text");
        content.Add(new StringContent($"<p>您好！</p><p>有訪客已完成簽到：</p><ul><li>訪客姓名：{safeVisitorName}</li><li>訪客信箱：{safeVisitorEmail}</li><li>會議名稱：{safeMeetingName}</li><li>會議室：{safeMeetingRoom}</li><li>簽到時間：{safeCheckinTime}</li></ul>"), "html");

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

