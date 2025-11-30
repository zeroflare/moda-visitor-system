using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckinController : ControllerBase
{
    private readonly ITwdiwService _twdiwService;
    private readonly ILogger<CheckinController> _logger;

    public CheckinController(ITwdiwService twdiwService, ILogger<CheckinController> logger)
    {
        _twdiwService = twdiwService;
        _logger = logger;
    }

    [HttpGet("qrcode")]
    public async Task<IActionResult> GetQRCode([FromQuery] string transactionId, [FromQuery] string? counter)
    {
        if (string.IsNullOrEmpty(transactionId) || !IsUUID(transactionId))
        {
            return BadRequest(new { error = true, message = "transactionId 格式錯誤，需為 UUID" });
        }

        try
        {
            var result = await _twdiwService.GetQRCodeAsync(transactionId, counter);
            return Ok(result);
        }
        catch (HttpRequestException)
        {
            return StatusCode(500, new { error = true, message = "取得 QR Code 失敗" });
        }
    }

    [HttpGet("result")]
    public async Task<IActionResult> GetResult([FromQuery] string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId) || !IsUUID(transactionId))
        {
            return BadRequest(new { error = true, message = "transactionId 格式錯誤，需為 UUID" });
        }

        try
        {
            var result = await _twdiwService.GetCheckinResultAsync(transactionId);
            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.Message == "等待驗證")
        {
            return BadRequest(new { message = "等待驗證" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetResult exception");
            return StatusCode(500, new { error = true, message = "發生錯誤" });
        }
    }

    [HttpGet("info")]
    public IActionResult GetCounterInfo([FromQuery] string counter)
    {
        // TODO: 實作從資料庫或設定檔取得櫃檯資訊
        return NotFound(new { message = "Counter not found" });
    }

    private static bool IsUUID(string value)
    {
        var uuidRegex = new Regex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$", RegexOptions.IgnoreCase);
        return uuidRegex.IsMatch(value);
    }
}

