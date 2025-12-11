namespace web.Middleware;

public class RequestLoggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggerMiddleware> _logger;

    public RequestLoggerMiddleware(RequestDelegate next, ILogger<RequestLoggerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var start = DateTime.UtcNow;
        await _next(context);
        var duration = (DateTime.UtcNow - start).TotalMilliseconds;
        
        _logger.LogInformation(
            "[{Method}] {Path} - {StatusCode} ({Duration}ms)",
            context.Request.Method,
            context.Request.Path.ToString().Replace("\r", "").Replace("\n", ""),
            context.Response.StatusCode,
            duration
        );
    }
}

