namespace console.Services;

public interface IGooglePeopleService
{
    Task<List<Contact>> GetContactsAsync();
    Task SyncContactsToDatabaseAsync();
}

public record Contact(
    string Id,
    string? Name,
    string? Email,
    string? Phone,
    string? Company,
    string? Department,
    string? Title
);

