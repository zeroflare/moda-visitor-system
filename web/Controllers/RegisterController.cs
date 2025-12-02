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
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(
        ICacheService cacheService,
        IMailService mailService,
        ITwdiwService twdiwService,
        ILogger<RegisterController> logger)
    {
        _cacheService = cacheService;
        _mailService = mailService;
        _twdiwService = twdiwService;
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
}

