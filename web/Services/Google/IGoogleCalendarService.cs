namespace web.Services;

public interface IGoogleCalendarService
{
    Task SyncEventsToDatabaseAsync(CancellationToken cancellationToken = default);
}

