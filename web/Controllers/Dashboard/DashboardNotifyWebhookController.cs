using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/notifywebhooks")]
public class DashboardNotifyWebhookController : ControllerBase
{
    private readonly INotifyWebhookService _notifyWebhookService;
    private readonly ILogger<DashboardNotifyWebhookController> _logger;

    public DashboardNotifyWebhookController(
        INotifyWebhookService notifyWebhookService,
        ILogger<DashboardNotifyWebhookController> logger)
    {
        _notifyWebhookService = notifyWebhookService;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有 webhook 列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotifyWebhook>>> GetNotifyWebhooks()
    {
        try
        {
            var webhooks = await _notifyWebhookService.GetAllNotifyWebhooksAsync();
            return Ok(webhooks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得 webhook 列表失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 根據單位取得 webhook 資訊
    /// </summary>
    [HttpGet("{dept}")]
    public async Task<ActionResult<NotifyWebhook>> GetNotifyWebhook(string dept)
    {
        try
        {
            var webhook = await _notifyWebhookService.GetNotifyWebhookByDeptAsync(dept);
            if (webhook == null)
            {
                return NotFound(new { error = "webhook 不存在" });
            }
            return Ok(webhook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得 webhook 資訊失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 建立新 webhook
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<NotifyWebhook>> CreateNotifyWebhook([FromBody] NotifyWebhook notifyWebhook)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(notifyWebhook.Dept))
            {
                return BadRequest(new { error = "單位為必填欄位" });
            }

            if (string.IsNullOrWhiteSpace(notifyWebhook.Type))
            {
                return BadRequest(new { error = "類型為必填欄位" });
            }

            if (string.IsNullOrWhiteSpace(notifyWebhook.Webhook))
            {
                return BadRequest(new { error = "webhook 路徑為必填欄位" });
            }

            // 檢查單位是否已存在
            var existingWebhook = await _notifyWebhookService.GetNotifyWebhookByDeptAsync(notifyWebhook.Dept);
            if (existingWebhook != null)
            {
                return Conflict(new { error = "該單位的 webhook 已存在" });
            }

            var createdWebhook = await _notifyWebhookService.CreateNotifyWebhookAsync(notifyWebhook);
            return CreatedAtAction(nameof(GetNotifyWebhook), new { dept = createdWebhook.Dept }, createdWebhook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立 webhook 失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 更新 webhook 資訊
    /// </summary>
    [HttpPut("{dept}")]
    public async Task<ActionResult<NotifyWebhook>> UpdateNotifyWebhook(string dept, [FromBody] NotifyWebhook notifyWebhook)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(notifyWebhook.Type))
            {
                return BadRequest(new { error = "類型為必填欄位" });
            }

            if (string.IsNullOrWhiteSpace(notifyWebhook.Webhook))
            {
                return BadRequest(new { error = "webhook 路徑為必填欄位" });
            }

            var updatedWebhook = await _notifyWebhookService.UpdateNotifyWebhookAsync(dept, notifyWebhook);
            if (updatedWebhook == null)
            {
                return NotFound(new { error = "webhook 不存在" });
            }

            return Ok(updatedWebhook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 webhook 失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 刪除 webhook
    /// </summary>
    [HttpDelete("{dept}")]
    public async Task<IActionResult> DeleteNotifyWebhook(string dept)
    {
        try
        {
            var deleted = await _notifyWebhookService.DeleteNotifyWebhookAsync(dept);
            if (!deleted)
            {
                return NotFound(new { error = "webhook 不存在" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除 webhook 失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }
}

