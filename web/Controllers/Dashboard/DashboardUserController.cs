using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/users")]
public class DashboardUserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<DashboardUserController> _logger;

    public DashboardUserController(
        IUserService userService,
        ILogger<DashboardUserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有使用者列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得使用者列表失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 根據 Email 取得使用者資訊
    /// </summary>
    [HttpGet("{email}")]
    public async Task<ActionResult<User>> GetUser(string email)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { error = "使用者不存在" });
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得使用者資訊失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 建立新使用者
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return BadRequest(new { error = "Email 為必填欄位" });
            }

            if (string.IsNullOrWhiteSpace(user.Username))
            {
                return BadRequest(new { error = "使用者名稱為必填欄位" });
            }

            if (string.IsNullOrWhiteSpace(user.Role))
            {
                return BadRequest(new { error = "角色為必填欄位" });
            }

            // 檢查 Email 是否已存在
            var existingUser = await _userService.GetUserByEmailAsync(user.Email);
            if (existingUser != null)
            {
                return Conflict(new { error = "該 Email 的使用者已存在" });
            }

            var createdUser = await _userService.CreateUserAsync(user);
            return CreatedAtAction(nameof(GetUser), new { email = createdUser.Email }, createdUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立使用者失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 更新使用者資訊
    /// </summary>
    [HttpPut("{email}")]
    public async Task<ActionResult<User>> UpdateUser(string email, [FromBody] User user)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                return BadRequest(new { error = "使用者名稱為必填欄位" });
            }

            if (string.IsNullOrWhiteSpace(user.Role))
            {
                return BadRequest(new { error = "角色為必填欄位" });
            }

            var updatedUser = await _userService.UpdateUserAsync(email, user);
            if (updatedUser == null)
            {
                return NotFound(new { error = "使用者不存在" });
            }

            return Ok(updatedUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新使用者失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }

    /// <summary>
    /// 刪除使用者
    /// </summary>
    [HttpDelete("{email}")]
    public async Task<IActionResult> DeleteUser(string email)
    {
        try
        {
            var deleted = await _userService.DeleteUserAsync(email);
            if (!deleted)
            {
                return NotFound(new { error = "使用者不存在" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除使用者失敗");
            return StatusCode(500, new { error = "伺服器錯誤" });
        }
    }
}

