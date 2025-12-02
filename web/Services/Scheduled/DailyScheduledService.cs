using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.Services.Scheduled;

public class DailyScheduledService : BackgroundService, IDailyScheduledService
{
    private readonly ILogger<DailyScheduledService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly string _instanceId;

    public DailyScheduledService(
        ILogger<DailyScheduledService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        // 生成唯一實例 ID（用於鎖的值）
        _instanceId = $"{Environment.MachineName}-{Guid.NewGuid()}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily Scheduled Service started");

        // 等待一小段時間再開始，讓應用程式完全啟動
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 計算到下一個 UTC+0 0:00 的時間
                var now = DateTime.UtcNow;
                var nextMidnight = now.Date.AddDays(1); // 下一個 UTC 午夜
                var delay = nextMidnight - now;

                _logger.LogInformation("Next scheduled execution at UTC: {NextExecution:yyyy-MM-dd HH:mm:ss} (in {Delay:hh\\:mm\\:ss})", 
                    nextMidnight, delay);

                // 等待到下一個 UTC 午夜
                await Task.Delay(delay, stoppingToken);

                // 執行排程任務
                await ExecuteDailyTaskAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 正常取消，退出循環
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during daily scheduled task");
                // 發生錯誤時，等待 1 小時後重試
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Daily Scheduled Service stopped");
    }

    public async Task ExecuteDailyTaskAsync(CancellationToken cancellationToken = default)
    {
        const string lockKey = "daily_scheduled_task:lock";
        const int lockExpirationSeconds = 300; // 鎖過期時間 5 分鐘（防止死鎖）
        
        // 使用 scope 來取得 Scoped 服務
        using var scope = _serviceScopeFactory.CreateScope();
        var cacheService = scope.ServiceProvider.GetService<ICacheService>();
        
        // 如果 Redis 不可用，記錄警告但繼續執行（單實例環境）
        if (cacheService == null)
        {
            _logger.LogWarning("Redis cache service not available. Proceeding without distributed lock (single instance mode).");
            await ExecuteTaskInternalAsync(scope, cancellationToken);
            return;
        }

        // 嘗試獲取分布式鎖
        var lockAcquired = await cacheService.TryAcquireLockAsync(
            lockKey, 
            _instanceId, 
            TimeSpan.FromSeconds(lockExpirationSeconds)
        );

        if (!lockAcquired)
        {
            _logger.LogInformation("Failed to acquire lock. Another instance is already executing the daily scheduled task. Skipping execution.");
            return;
        }

        try
        {
            _logger.LogInformation("Lock acquired successfully. Instance ID: {InstanceId}", _instanceId);
            await ExecuteTaskInternalAsync(scope, cancellationToken);
        }
        finally
        {
            // 釋放鎖
            var released = await cacheService.ReleaseLockAsync(lockKey, _instanceId);
            if (released)
            {
                _logger.LogInformation("Lock released successfully. Instance ID: {InstanceId}", _instanceId);
            }
            else
            {
                _logger.LogWarning("Failed to release lock. It may have expired. Instance ID: {InstanceId}", _instanceId);
            }
        }
    }

    private async Task ExecuteTaskInternalAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var startTimeLocal = DateTime.UtcNow.AddHours(8); // UTC+8

        _logger.LogInformation("========== Starting daily scheduled task ==========");
        _logger.LogInformation("Task started at UTC: {UtcTime:yyyy-MM-dd HH:mm:ss}", startTime);
        _logger.LogInformation("Task started at UTC+8: {LocalTime:yyyy-MM-dd HH:mm:ss}", startTimeLocal);
        _logger.LogInformation("Instance ID: {InstanceId}", _instanceId);

        var notifyWebhookService = scope.ServiceProvider.GetRequiredService<INotifyWebhookService>();
        var googleChatService = scope.ServiceProvider.GetRequiredService<IGoogleChatService>();
        var googlePeopleService = scope.ServiceProvider.GetService<IGooglePeopleService>();
        var googleCalendarService = scope.ServiceProvider.GetService<IGoogleCalendarService>();
        var registrationInvitationService = scope.ServiceProvider.GetService<IRegistrationInvitationService>();

        try
        {
            // 1. 從 Google People 取得聯絡人
            if (googlePeopleService != null)
            {
                _logger.LogInformation("--- Starting Google People sync ---");
                var peopleSyncStartTime = DateTime.UtcNow;
                try
                {
                    await googlePeopleService.SyncContactsToDatabaseAsync(cancellationToken);
                    var peopleSyncDuration = DateTime.UtcNow - peopleSyncStartTime;
                    _logger.LogInformation("--- Google People sync completed in {Duration:mm\\:ss\\.fff} ---", peopleSyncDuration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Google People sync");
                    // 不中斷整個排程，繼續執行其他任務
                }
            }
            else
            {
                _logger.LogWarning("Google People Service not available. Skipping contact sync.");
            }

            // 2. 從 Google 日曆取得會議
            if (googleCalendarService != null)
            {
                _logger.LogInformation("--- Starting Google Calendar sync ---");
                var calendarSyncStartTime = DateTime.UtcNow;
                try
                {
                    await googleCalendarService.SyncEventsToDatabaseAsync(cancellationToken);
                    var calendarSyncDuration = DateTime.UtcNow - calendarSyncStartTime;
                    _logger.LogInformation("--- Google Calendar sync completed in {Duration:mm\\:ss\\.fff} ---", calendarSyncDuration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Google Calendar sync");
                    // 不中斷整個排程，繼續執行其他任務
                }
            }
            else
            {
                _logger.LogWarning("Google Calendar Service not available. Skipping calendar sync.");
            }

            // 3. 排程寄送註冊信
            if (registrationInvitationService != null)
            {
                _logger.LogInformation("--- Starting registration invitation emails ---");
                var invitationStartTime = DateTime.UtcNow;
                try
                {
                    await registrationInvitationService.SendInvitationsToTomorrowVisitorsAsync(cancellationToken);
                    var invitationDuration = DateTime.UtcNow - invitationStartTime;
                    _logger.LogInformation("--- Registration invitation emails completed in {Duration:mm\\:ss\\.fff} ---", invitationDuration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during registration invitation emails");
                    // 不中斷整個排程，繼續執行其他任務
                }
            }
            else
            {
                _logger.LogWarning("Registration Invitation Service not available. Skipping invitation emails.");
            }

            // 取得 admin webhook 並發送通知
            var adminWebhook = await notifyWebhookService.GetNotifyWebhookByDeptAndTypeAsync("admin", "googlechat");
            
            if (adminWebhook != null && !string.IsNullOrEmpty(adminWebhook.Webhook))
            {
                var message = $"⏰ 每日排程已啟動\n\n" +
                             $"執行時間 (UTC): {startTime:yyyy-MM-dd HH:mm:ss}\n" +
                             $"執行時間 (UTC+8): {startTimeLocal:yyyy-MM-dd HH:mm:ss}\n" +
                             $"執行實例: {_instanceId}";

                await googleChatService.SendNotificationAsync(adminWebhook.Webhook, message);
                _logger.LogInformation("Daily scheduled task notification sent successfully to admin webhook");
            }
            else
            {
                _logger.LogWarning("Admin webhook not found or webhook URL is empty. Skipping notification.");
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("========== Daily scheduled task completed successfully ==========");
            _logger.LogInformation("Task duration: {Duration:mm\\:ss\\.fff}", duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Error occurred during daily scheduled task execution");
            _logger.LogError("Error after {Duration:mm\\:ss\\.fff}", duration);
            throw;
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Daily Scheduled Service is starting");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Daily Scheduled Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

