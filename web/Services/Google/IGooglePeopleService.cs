namespace web.Services;

public interface IGooglePeopleService
{
    Task SyncContactsToDatabaseAsync(CancellationToken cancellationToken = default);
}

