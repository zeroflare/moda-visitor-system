using web.Services;

namespace web.Middleware;

public class DashboardAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DashboardAuthMiddleware> _logger;

    public DashboardAuthMiddleware(RequestDelegate next, ILogger<DashboardAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICacheService cacheService, IUserService userService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 排除 /api/dashboard/login 路徑
        if (path.StartsWith("/api/dashboard/login", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 只處理 /api/dashboard/* 路徑
        if (!path.StartsWith("/api/dashboard/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 檢查是否已登入
        var sessionId = context.Request.Cookies["dashboard_session"];
        if (string.IsNullOrEmpty(sessionId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "未登入或登入已過期" });
            return;
        }

        // 從 Redis 獲取使用者 email
        var email = await cacheService.GetAsync($"dashboard:session:{sessionId}");
        if (string.IsNullOrEmpty(email))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "未登入或登入已過期" });
            return;
        }

        // 獲取使用者資訊
        var user = await userService.GetUserByEmailAsync(email);
        if (user == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "使用者不存在" });
            return;
        }

        // 將使用者資訊儲存到 HttpContext.Items 中，供後續使用
        context.Items["CurrentUser"] = user;

        // 檢查非 GET 請求是否需要 admin 權限
        var method = context.Request.Method;
        if (method != "GET" && method != "HEAD" && method != "OPTIONS")
        {
            if (user.Role != "admin")
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "只有 admin 角色可以執行此操作" });
                return;
            }
        }

        await _next(context);
    }
}

