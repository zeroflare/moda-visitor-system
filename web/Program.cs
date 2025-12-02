using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using web.Data;
using web.Middleware;
using web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add Database
var connectionString = builder.Configuration.GetConnectionString("MySQL");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.Parse("8.0.21-mysql")));
}

// Add Counter Service
builder.Services.AddScoped<ICounterService, CounterService>();

// Add MeetingRoom Service
builder.Services.AddScoped<IMeetingRoomService, MeetingRoomService>();

// Add RegisterToken Service
builder.Services.AddScoped<IRegisterTokenService, RegisterTokenService>();

// Add NotifyWebhook Service
builder.Services.AddScoped<INotifyWebhookService, NotifyWebhookService>();

// Add User Service
builder.Services.AddScoped<IUserService, UserService>();

// Add VisitorLog Service
builder.Services.AddScoped<IVisitorLogService, VisitorLogService>();

// Add CheckLog Service
builder.Services.AddScoped<ICheckLogService, CheckLogService>();

// Add Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}

// Add HttpClient for external services
builder.Services.AddHttpClient<ITwdiwService, TwdiwService>();
builder.Services.AddHttpClient<IMailService, MailgunService>();
builder.Services.AddHttpClient<IGoogleChatService, GoogleChatService>();

// Add services
builder.Services.AddScoped<ITwdiwService, TwdiwService>();
builder.Services.AddScoped<IMailService, MailgunService>();
builder.Services.AddScoped<IGoogleChatService, GoogleChatService>();

// Add background services
builder.Services.AddSingleton<DailyScheduledService>();
builder.Services.AddSingleton<IDailyScheduledService>(sp => sp.GetRequiredService<DailyScheduledService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<DailyScheduledService>());

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Enable static files
app.UseDefaultFiles();
app.UseStaticFiles();

// Add request logger middleware
app.UseMiddleware<RequestLoggerMiddleware>();

app.UseRouting();

app.UseSession();

// Add Dashboard authentication middleware (before authorization)
app.UseMiddleware<DashboardAuthMiddleware>();

app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
