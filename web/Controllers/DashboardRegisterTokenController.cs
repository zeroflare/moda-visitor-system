using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/registertokens")]
public class DashboardRegisterTokenController : ControllerBase
{
    private readonly IRegisterTokenService _registerTokenService;
    private readonly ILogger<DashboardRegisterTokenController> _logger;

    public DashboardRegisterTokenController(
        IRegisterTokenService registerTokenService,
        ILogger<DashboardRegisterTokenController> logger)
    {
        _registerTokenService = registerTokenService;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有註冊 token 列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RegisterToken>>> GetRegisterTokens([FromQuery] string? visitorEmail)
    {
        try
        {
            IEnumerable<RegisterToken> tokens;
            if (!string.IsNullOrEmpty(visitorEmail))
            {
                tokens = await _registerTokenService.GetRegisterTokensByEmailAsync(visitorEmail);
            }
            else
            {
                tokens = await _registerTokenService.GetAllRegisterTokensAsync();
            }
            return Ok(tokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得註冊 token 列表失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 根據 ID 取得註冊 token 資訊
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RegisterToken>> GetRegisterToken(string id)
    {
        try
        {
            var token = await _registerTokenService.GetRegisterTokenByIdAsync(id);
            if (token == null)
            {
                return NotFound(new { error = "註冊 token 不存在" });
            }
            return Ok(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得註冊 token 資訊失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 建立新註冊 token
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RegisterToken>> CreateRegisterToken([FromBody] RegisterToken registerToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(registerToken.VisitorEmail))
            {
                return BadRequest(new { error = "訪客信箱為必填欄位" });
            }

            if (registerToken.ExpiredAt <= registerToken.CreatedAt)
            {
                return BadRequest(new { error = "過期日期必須晚於建立日期" });
            }

            // 如果沒有提供 ID，自動生成 UUID
            if (string.IsNullOrWhiteSpace(registerToken.Id))
            {
                registerToken.Id = Guid.NewGuid().ToString();
            }

            var createdToken = await _registerTokenService.CreateRegisterTokenAsync(registerToken);
            return CreatedAtAction(nameof(GetRegisterToken), new { id = createdToken.Id }, createdToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立註冊 token 失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 更新註冊 token 資訊
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<RegisterToken>> UpdateRegisterToken(string id, [FromBody] RegisterToken registerToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(registerToken.VisitorEmail))
            {
                return BadRequest(new { error = "訪客信箱為必填欄位" });
            }

            if (registerToken.ExpiredAt <= registerToken.CreatedAt)
            {
                return BadRequest(new { error = "過期日期必須晚於建立日期" });
            }

            var updatedToken = await _registerTokenService.UpdateRegisterTokenAsync(id, registerToken);
            if (updatedToken == null)
            {
                return NotFound(new { error = "註冊 token 不存在" });
            }

            return Ok(updatedToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新註冊 token 失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 刪除註冊 token
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRegisterToken(string id)
    {
        try
        {
            var deleted = await _registerTokenService.DeleteRegisterTokenAsync(id);
            if (!deleted)
            {
                return NotFound(new { error = "註冊 token 不存在" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除註冊 token 失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }
}

