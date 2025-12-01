using Microsoft.AspNetCore.Mvc;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/registermail")]
public class DashboardRegisterMailController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IMailService _mailService;
    private readonly ILogger<DashboardRegisterMailController> _logger;
    private readonly IConfiguration _configuration;

    public DashboardRegisterMailController(
        ICacheService cacheService,
        IMailService mailService,
        IConfiguration configuration,
        ILogger<DashboardRegisterMailController> logger)
    {
        _cacheService = cacheService;
        _mailService = mailService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 發送註冊邀請信
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendRegisterMail([FromBody] SendRegisterMailRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { error = "缺少 email 欄位" });
        }

        try
        {
            // 產生 token (UUID)
            var token = Guid.NewGuid().ToString();

            // 將 token 和 email 儲存到 Redis，有效期兩天
            var cacheKey = $"register:token:{token}";
            await _cacheService.SetAsync(cacheKey, request.Email, TimeSpan.FromDays(2));

            // 取得註冊 URL（從配置或使用預設值）
            var baseUrl = _configuration["BaseUrl"] ?? 
                         $"{Request.Scheme}://{Request.Host}";
            var registerUrl = $"{baseUrl}/register?token={token}";

            // 發送註冊邀請信
            await _mailService.SendRegisterInvitationAsync(request.Email, token, registerUrl);

            return Ok(new { message = "註冊邀請信已發送" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "發送註冊邀請信錯誤");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }
}

public record SendRegisterMailRequest(string Email);

