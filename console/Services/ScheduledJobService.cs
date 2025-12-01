using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace console.Services;

public class ScheduledJobService : BackgroundService
{
    private readonly ILogger<ScheduledJobService> _logger;
    private readonly IGoogleCalendarService _calendarService;
    private readonly IGooglePeopleService _peopleService;
    private readonly TimeSpan _syncInterval;

    public ScheduledJobService(
        ILogger<ScheduledJobService> logger,
        IGoogleCalendarService calendarService,
        IGooglePeopleService peopleService,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _calendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
        _peopleService = peopleService ?? throw new ArgumentNullException(nameof(peopleService));
        
        // 測試模式：每30秒執行一次
        // 可以從配置讀取：var syncIntervalSeconds = configuration.GetValue<int>("Google:SyncIntervalSeconds", 30);
        _syncInterval = TimeSpan.FromSeconds(30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled Job Service started. Sync interval: {Interval} seconds", _syncInterval.TotalSeconds);

        // 等待一小段時間再開始第一次執行，讓應用程式完全啟動
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var startTime = DateTime.UtcNow;
            var startTimeLocal = DateTime.Now;
            try
            {
                _logger.LogInformation("========== Starting scheduled sync ==========");
                _logger.LogInformation("Sync started at UTC: {UtcTime:yyyy-MM-dd HH:mm:ss}", startTime);
                _logger.LogInformation("Sync started at Local: {LocalTime:yyyy-MM-dd HH:mm:ss}", startTimeLocal);

                // 同步 Google Calendar 資料
                _logger.LogInformation("--- Starting Google Calendar sync ---");
                var calendarStartTime = DateTime.UtcNow;
                await _calendarService.SyncEventsToDatabaseAsync();
                var calendarDuration = DateTime.UtcNow - calendarStartTime;
                _logger.LogInformation("--- Google Calendar sync completed in {Duration:mm\\:ss\\.fff} ---", calendarDuration);

                // 同步 Google People 資料
                _logger.LogInformation("--- Starting Google People sync ---");
                var peopleStartTime = DateTime.UtcNow;
                await _peopleService.SyncContactsToDatabaseAsync();
                var peopleDuration = DateTime.UtcNow - peopleStartTime;
                _logger.LogInformation("--- Google People sync completed in {Duration:mm\\:ss\\.fff} ---", peopleDuration);

                var totalDuration = DateTime.UtcNow - startTime;
                _logger.LogInformation("========== Scheduled sync completed successfully ==========");
                _logger.LogInformation("Total sync duration: {Duration:mm\\:ss\\.fff}", totalDuration);
                _logger.LogInformation("Calendar sync: {CalendarDuration:mm\\:ss\\.fff}, People sync: {PeopleDuration:mm\\:ss\\.fff}", 
                    calendarDuration, peopleDuration);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, 
                    "========== Error occurred during scheduled sync ==========");
                _logger.LogError("Error after {Duration:mm\\:ss\\.fff}", duration);
                _logger.LogError("Exception: {ExceptionType}: {ExceptionMessage}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner Exception: {InnerExceptionType}: {InnerExceptionMessage}", 
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                
                // 發生錯誤時，等待較短時間再重試（避免連續失敗）
                _logger.LogWarning("Retrying in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            // 等待下次執行
            var nextSyncTime = DateTime.UtcNow.Add(_syncInterval);
            _logger.LogInformation("Next sync scheduled in {Interval} seconds (at UTC: {NextSyncTime:yyyy-MM-dd HH:mm:ss})", 
                _syncInterval.TotalSeconds, nextSyncTime);
            _logger.LogInformation("");
            await Task.Delay(_syncInterval, stoppingToken);
        }

        _logger.LogInformation("Scheduled Job Service stopped");
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled Job Service is starting");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled Job Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

