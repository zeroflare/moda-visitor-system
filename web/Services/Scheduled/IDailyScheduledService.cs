namespace web.Services.Scheduled;

public interface IDailyScheduledService
{
    Task ExecuteDailyTaskAsync(CancellationToken cancellationToken = default);
}

