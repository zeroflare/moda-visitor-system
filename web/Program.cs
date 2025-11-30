using web.Middleware;
using web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Memory Cache
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// Add HttpClient for external services
builder.Services.AddHttpClient<ITwdiwService, TwdiwService>();
builder.Services.AddHttpClient<IMailService, MailgunService>();

// Add services
builder.Services.AddScoped<ITwdiwService, TwdiwService>();
builder.Services.AddScoped<IMailService, MailgunService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable static files
app.UseDefaultFiles();
app.UseStaticFiles();

// Add request logger middleware
app.UseMiddleware<RequestLoggerMiddleware>();

app.UseRouting();

app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
