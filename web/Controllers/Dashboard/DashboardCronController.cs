using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;
using web.Services.Scheduled;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/cron")]
public class DashboardCronController : ControllerBase
{
    private readonly IDailyScheduledService _dailyScheduledService;
    private readonly ILogger<DashboardCronController> _logger;

    public DashboardCronController(
        IDailyScheduledService dailyScheduledService,
        ILogger<DashboardCronController> logger)
    {
        _dailyScheduledService = dailyScheduledService ?? throw new ArgumentNullException(nameof(dailyScheduledService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 手動觸發每日排程任務
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TriggerDailyScheduledTask()
    {
        // 檢查是否已登入（從 middleware 中獲取使用者資訊）
        var user = HttpContext.Items["CurrentUser"] as User;
        
        if (user == null)
        {
            return Unauthorized(new { error = "未登入或登入已過期" });
        }

        try
        {
            _logger.LogInformation("Manual trigger of daily scheduled task requested by user: {Email}", user.Email);
            
            await _dailyScheduledService.ExecuteDailyTaskAsync();
            
            return Ok(new 
            { 
                message = "每日排程任務已成功觸發",
                triggeredAt = DateTime.UtcNow,
                triggeredBy = user.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手動觸發每日排程任務失敗");
            return StatusCode(500, new { error = "觸發排程任務失敗", details = ex.Message });
        }
    }
}

