using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/checklogs")]
public class DashboardCheckLogController : ControllerBase
{
    private readonly ICheckLogService _checkLogService;
    private readonly ILogger<DashboardCheckLogController> _logger;

    public DashboardCheckLogController(
        ICheckLogService checkLogService,
        ILogger<DashboardCheckLogController> logger)
    {
        _checkLogService = checkLogService;
        _logger = logger;
    }

    /// <summary>
    /// 查詢簽到簽退原始資料
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CheckLogResponse>>> GetCheckLogs()
    {
        try
        {
            var checkLogs = await _checkLogService.GetCheckLogsAsync();
            return Ok(checkLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得簽到記錄失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }
}

