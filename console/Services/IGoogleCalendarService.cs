namespace console.Services;

public interface IGoogleCalendarService
{
    Task<List<CalendarEvent>> GetEventsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task SyncEventsToDatabaseAsync();
}

public record CalendarEvent(
    string Id,
    string Summary,
    string? Description,
    DateTime Start,
    DateTime End,
    string? Location,
    string? OrganizerEmail,
    List<string> Attendees
);

