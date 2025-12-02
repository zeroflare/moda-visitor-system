using console.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add services
builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddSingleton<IGoogleCalendarService, GoogleCalendarService>();
builder.Services.AddSingleton<IGooglePeopleService, GooglePeopleService>();
builder.Services.AddHostedService<ScheduledJobService>();

var host = builder.Build();
host.Run();

