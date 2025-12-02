using Microsoft.AspNetCore.Mvc;
using web.Models;

namespace web.Controllers;

[ApiController]
[Route("api/dashboard/me")]
public class DashboardMeController : ControllerBase
{
    /// <summary>
    /// 取得當前登入使用者資訊
    /// </summary>
    [HttpGet]
    public ActionResult<User> GetCurrentUser()
    {
        // 從 middleware 中獲取使用者資訊
        var user = HttpContext.Items["CurrentUser"] as User;
        
        if (user == null)
        {
            return Unauthorized(new { error = "未登入或登入已過期" });
        }

        return Ok(new
        {
            username = user.Username,
            email = user.Email,
            role = user.Role
        });
    }
}

