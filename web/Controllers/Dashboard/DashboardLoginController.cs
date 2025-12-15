using Microsoft.AspNetCore.Mvc;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/login")]
public class DashboardLoginController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IMailService _mailService;
    private readonly IUserService _userService;
    private readonly ILogger<DashboardLoginController> _logger;

    public DashboardLoginController(
        ICacheService cacheService,
        IMailService mailService,
        IUserService userService,
        ILogger<DashboardLoginController> logger)
    {
        _cacheService = cacheService;
        _mailService = mailService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 發送 Dashboard 登入 OTP
    /// </summary>
    [HttpPost("otp")]
    public async Task<IActionResult> SendOTP([FromBody] SendDashboardOTPRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { error = "缺少 email 欄位" });
        }

        try
        {
            // 檢查是否在冷卻期內
            var cooldownKey = $"dashboard:cooldown:{request.Email}";
            var lastSent = await _cacheService.GetAsync(cooldownKey);

            if (!string.IsNullOrEmpty(lastSent))
            {
                return BadRequest(new { error = "請稍後再試，每分鐘僅能寄送一次驗證碼" });
            }

            // 產生六位數 OTP
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();

            // 儲存 OTP (10 分鐘有效)
            await _cacheService.SetAsync($"dashboard:otp:{request.Email}", otp, TimeSpan.FromMinutes(10));

            // 寄信
            await _mailService.SendOTPAsync(request.Email, otp);

            // 寫入冷卻鍵 (60 秒內不能再寄)
            await _cacheService.SetAsync(cooldownKey, "1", TimeSpan.FromSeconds(60));

            return Ok(new { message = "OTP sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "寄信錯誤");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 驗證 Dashboard 登入 OTP
    /// </summary>
    [HttpPost("result")]
    public async Task<IActionResult> VerifyOTP([FromBody] VerifyDashboardOTPRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { message = "缺少 email 欄位" });
        }

        if (string.IsNullOrEmpty(request.Otp))
        {
            return BadRequest(new { message = "缺少 otp 欄位" });
        }

        try
        {
            // 檢查 email 是否存在於 users table
            var user = await _userService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "User not found" });
            }

            // 檢查 OTP
            var storedOtp = await _cacheService.GetAsync($"dashboard:otp:{request.Email}");
            if (string.IsNullOrEmpty(storedOtp))
            {
                return BadRequest(new { message = "Invalid or expired OTP" });
            }

            if (storedOtp != request.Otp)
            {
                return BadRequest(new { message = "Invalid or expired OTP" });
            }

            // OTP 驗證成功後，刪除舊的 OTP
            await _cacheService.DeleteAsync($"dashboard:otp:{request.Email}");

            // 生成 session ID
            var sessionId = Guid.NewGuid().ToString();
            
            // 儲存 session (24 小時有效)
            await _cacheService.SetAsync($"dashboard:session:{sessionId}", request.Email, TimeSpan.FromHours(24));

            // 設置 session cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(24),
                Path = "/"
            };
            
            Response.Cookies.Append("dashboard_session", sessionId, cookieOptions);

            return Ok(new { message = "Login successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "驗證 OTP 錯誤");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// Dashboard 登出：清除 session 與 cookie
    /// </summary>
    [HttpGet("/api/dashboard/logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var sessionId = Request.Cookies["dashboard_session"];

            if (!string.IsNullOrEmpty(sessionId))
            {
                await _cacheService.DeleteAsync($"dashboard:session:{sessionId}");

                // 清除 cookie
                Response.Cookies.Append("dashboard_session", string.Empty, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UnixEpoch,
                    Path = "/"
                });
            }

            // 清除本地 Session
            HttpContext.Session.Clear();

            return Ok(new { message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }
}

// Request models
public record SendDashboardOTPRequest(string Email);
public record VerifyDashboardOTPRequest(string Email, string Otp);

