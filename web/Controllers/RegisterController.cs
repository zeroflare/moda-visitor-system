using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IMailService _mailService;
    private readonly ITwdiwService _twdiwService;
    private readonly INotifyWebhookService _notifyWebhookService;
    private readonly IGoogleChatService _googleChatService;
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(
        ICacheService cacheService,
        IMailService mailService,
        ITwdiwService twdiwService,
        INotifyWebhookService notifyWebhookService,
        IGoogleChatService googleChatService,
        ILogger<RegisterController> logger)
    {
        _cacheService = cacheService;
        _mailService = mailService;
        _twdiwService = twdiwService;
        _notifyWebhookService = notifyWebhookService;
        _googleChatService = googleChatService;
        _logger = logger;
    }

    [HttpPost("otp")]
    public async Task<IActionResult> SendOTP([FromBody] SendOTPRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { error = "缺少 email 欄位" });
        }

        try
        {
            // 檢查是否在冷卻期內
            var cooldownKey = $"cooldown:{request.Email}";
            var lastSent = await _cacheService.GetAsync(cooldownKey);

            if (!string.IsNullOrEmpty(lastSent))
            {
                return BadRequest(new { error = "請稍後再試，每分鐘僅能寄送一次驗證碼" });
            }

            // 產生六位數 OTP
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();

            // 儲存 OTP (10 分鐘有效)
            await _cacheService.SetAsync($"otp:{request.Email}", otp, TimeSpan.FromMinutes(10));

            // 寄信
            await _mailService.SendOTPAsync(request.Email, otp);

            // 寫入冷卻鍵 (60 秒內不能再寄)
            await _cacheService.SetAsync(cooldownKey, "1", TimeSpan.FromSeconds(60));

            return Ok(new SendOTPResponse("OTP sent successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "寄信錯誤");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    [HttpPost("qrcode")]
    public async Task<IActionResult> SubmitRegistration([FromBody] SubmitRegistrationRequest request)
    {
        if (string.IsNullOrEmpty(request.Name) || 
            string.IsNullOrEmpty(request.Email) || 
            string.IsNullOrEmpty(request.Otp) || 
            string.IsNullOrEmpty(request.Phone) || 
            string.IsNullOrEmpty(request.Company) ||
            string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new { error = "缺少必要欄位 (name, email, otp, phone, company, token)" });
        }

        try
        {
            // 檢查 email 與 token 代表的 email 是否一致
            var cacheKey = $"register:token:{request.Token}";
            var tokenEmail = await _cacheService.GetAsync(cacheKey);
            
            if (string.IsNullOrEmpty(tokenEmail))
            {
                return BadRequest(new { error = "token 不存在或已過期" });
            }

            if (tokenEmail != request.Email)
            {
                return BadRequest(new { error = "email 與 token 不匹配" });
            }

            // 檢查 OTP
            var storedOtp = await _cacheService.GetAsync($"otp:{request.Email}");
            if (string.IsNullOrEmpty(storedOtp))
            {
                return BadRequest(new { error = "驗證碼已失效或未發送" });
            }

            if (storedOtp != request.Otp)
            {
                return BadRequest(new { error = "驗證碼錯誤" });
            }

            // OTP 驗證成功後，刪除舊的 OTP
            await _cacheService.DeleteAsync($"otp:{request.Email}");

            // 呼叫 TWDIW 服務
            var result = await _twdiwService.SubmitRegistrationAsync(request);

            // 註冊完成後，刪除 token（註銷 token）
            await _cacheService.DeleteAsync(cacheKey);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "呼叫 TWDIW 錯誤");
            return StatusCode(500, new { error = "伺服器錯誤，請稍後再試" });
        }
    }

    [HttpGet("result")]
    public async Task<IActionResult> GetRegistrationResult([FromQuery] string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId))
        {
            return BadRequest(new { error = true, message = "transactionId 為必填欄位" });
        }

        try
        {
            var result = await _twdiwService.GetRegistrationResultAsync(transactionId);
            
            // 檢查是否註冊完成（根據返回的 Message 判斷）
            string? message = null;
            try
            {
                if (result is System.Text.Json.JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty("Message", out var messageElement))
                    {
                        message = messageElement.GetString();
                    }
                }
                else
                {
                    // 使用反射檢查 Message 屬性
                    var resultType = result.GetType();
                    var messageProperty = resultType.GetProperty("Message");
                    if (messageProperty != null)
                    {
                        message = messageProperty.GetValue(result)?.ToString();
                    }
                }
            }
            catch (Exception notifyEx)
            {
                _logger.LogError(notifyEx, "檢查註冊完成狀態或發送通知失敗");
                // 不影響主要流程，繼續執行
            }

            // 如果還在等待註冊，返回 400
            if (message == "Waiting for registration")
            {
                return BadRequest(new { error = true, message = "Waiting for registration" });
            }

            // 如果註冊完成，發送通知
            if (message == "Registration completed")
            {
                try
                {
                    await SendRegistrationCompletedNotificationAsync(transactionId, result);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(notifyEx, "發送註冊完成通知失敗");
                    // 不影響主要流程，繼續執行
                }
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "getRegistrationResult exception");
            return StatusCode(500, new { error = true, message = "發生錯誤" });
        }
    }

    /// <summary>
    /// 根據 token 取得註冊 email
    /// </summary>
    [HttpGet("info")]
    public async Task<IActionResult> GetRegisterInfo([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { error = "缺少 token 參數" });
        }

        try
        {
            // 從 Redis 取得 email
            var cacheKey = $"register:token:{token}";
            var email = await _cacheService.GetAsync(cacheKey);

            if (string.IsNullOrEmpty(email))
            {
                return NotFound(new { error = "token 不存在或已過期" });
            }

            return Ok(new { email });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得註冊資訊錯誤");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 發送註冊完成通知到 Google Chat
    /// </summary>
    private async Task SendRegistrationCompletedNotificationAsync(string transactionId, object result)
    {
        try
        {
            var adminWebhook = await _notifyWebhookService.GetNotifyWebhookByDeptAndTypeAsync("admin", "googlechat");
            if (adminWebhook != null && !string.IsNullOrEmpty(adminWebhook.Webhook))
            {
                var registrationTime = DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm");
                
                // 從結果中提取用戶註冊資訊
                string? name = null;
                string? email = null;
                string? company = null;
                string? phone = null;

                try
                {
                    System.Text.Json.JsonElement? dataElement = null;

                    if (result is System.Text.Json.JsonElement jsonElement)
                    {
                        if (jsonElement.TryGetProperty("Data", out var dataProperty))
                        {
                            dataElement = dataProperty;
                        }
                    }
                    else
                    {
                        // 使用反射獲取 Data 屬性
                        var resultType = result.GetType();
                        var dataProperty = resultType.GetProperty("Data");
                        if (dataProperty != null)
                        {
                            var dataValue = dataProperty.GetValue(result);
                            if (dataValue is System.Text.Json.JsonElement jsonData)
                            {
                                dataElement = jsonData;
                            }
                        }
                    }

                    if (dataElement.HasValue)
                    {
                        var data = dataElement.Value;
                        
                        // 嘗試從 claims 中提取資訊（類似 GetCheckinResultAsync 的方式）
                        if (data.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == System.Text.Json.JsonValueKind.Array && dataArray.GetArrayLength() > 0)
                        {
                            var firstItem = dataArray[0];
                            if (firstItem.TryGetProperty("claims", out var claims))
                            {
                                foreach (var claim in claims.EnumerateArray())
                                {
                                    if (claim.TryGetProperty("ename", out var ename))
                                    {
                                        var enameValue = ename.GetString();
                                        if (claim.TryGetProperty("value", out var value))
                                        {
                                            var valueString = value.GetString();
                                            switch (enameValue)
                                            {
                                                case "name":
                                                    name = valueString;
                                                    break;
                                                case "email":
                                                    email = valueString;
                                                    break;
                                                case "company":
                                                    company = valueString;
                                                    break;
                                                case "phone":
                                                    phone = valueString;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "提取用戶註冊資訊失敗，將使用預設值");
                }

                // 構建通知訊息
                var notificationMessage = $"✅ 訪客註冊完成通知\n\n" +
                                         $"交易 ID：{transactionId}\n" +
                                         $"完成時間：{registrationTime}";

                if (!string.IsNullOrEmpty(name))
                {
                    notificationMessage += $"\n姓名：{name}";
                }
                if (!string.IsNullOrEmpty(email))
                {
                    notificationMessage += $"\n電子郵件：{email}";
                }
                if (!string.IsNullOrEmpty(company))
                {
                    notificationMessage += $"\n公司：{company}";
                }
                if (!string.IsNullOrEmpty(phone))
                {
                    notificationMessage += $"\n電話：{phone}";
                }

                await _googleChatService.SendNotificationAsync(adminWebhook.Webhook, notificationMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "發送註冊完成 Google Chat 通知失敗");
            throw;
        }
    }
}

