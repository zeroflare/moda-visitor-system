namespace web.Services;

public interface IDailyScheduledService
{
    Task ExecuteDailyTaskAsync(CancellationToken cancellationToken = default);
}

