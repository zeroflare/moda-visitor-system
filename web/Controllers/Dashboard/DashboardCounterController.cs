using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/counters")]
public class DashboardCounterController : ControllerBase
{
    private readonly ICounterService _counterService;
    private readonly ILogger<DashboardCounterController> _logger;

    public DashboardCounterController(
        ICounterService counterService,
        ILogger<DashboardCounterController> logger)
    {
        _counterService = counterService;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有櫃檯列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Counter>>> GetCounters()
    {
        try
        {
            var counters = await _counterService.GetAllCountersAsync();
            return Ok(counters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得櫃檯列表失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 根據 ID 取得櫃檯資訊
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Counter>> GetCounter(string id)
    {
        try
        {
            var counter = await _counterService.GetCounterByIdAsync(id);
            if (counter == null)
            {
                return NotFound(new { error = "櫃檯不存在" });
            }
            return Ok(counter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得櫃檯資訊失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 建立新櫃檯
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Counter>> CreateCounter([FromBody] Counter counter)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(counter.Id))
            {
                return BadRequest(new { error = "櫃檯 ID 為必填欄位" });
            }

            if (string.IsNullOrWhiteSpace(counter.Name))
            {
                return BadRequest(new { error = "櫃檯名稱為必填欄位" });
            }

            // 檢查 ID 是否已存在
            var existingCounter = await _counterService.GetCounterByIdAsync(counter.Id);
            if (existingCounter != null)
            {
                return Conflict(new { error = "櫃檯 ID 已存在" });
            }

            var createdCounter = await _counterService.CreateCounterAsync(counter);
            return CreatedAtAction(nameof(GetCounter), new { id = createdCounter.Id }, createdCounter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立櫃檯失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 更新櫃檯資訊
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Counter>> UpdateCounter(string id, [FromBody] Counter counter)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(counter.Name))
            {
                return BadRequest(new { error = "櫃檯名稱為必填欄位" });
            }

            var updatedCounter = await _counterService.UpdateCounterAsync(id, counter);
            if (updatedCounter == null)
            {
                return NotFound(new { error = "櫃檯不存在" });
            }

            return Ok(updatedCounter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新櫃檯失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 刪除櫃檯
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCounter(string id)
    {
        try
        {
            var deleted = await _counterService.DeleteCounterAsync(id);
            if (!deleted)
            {
                return NotFound(new { error = "櫃檯不存在" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除櫃檯失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }
}

