using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/visitorlogs")]
public class DashboardVisitorLogController : ControllerBase
{
    private readonly IVisitorLogService _visitorLogService;
    private readonly ILogger<DashboardVisitorLogController> _logger;

    public DashboardVisitorLogController(
        IVisitorLogService visitorLogService,
        ILogger<DashboardVisitorLogController> logger)
    {
        _visitorLogService = visitorLogService;
        _logger = logger;
    }

    /// <summary>
    /// 依據google日曆的簽到簽退紀錄
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VisitorLogResponse>>> GetVisitorLogs()
    {
        try
        {
            var visitorLogs = await _visitorLogService.GetVisitorLogsAsync();
            return Ok(visitorLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得簽到記錄失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }
}

