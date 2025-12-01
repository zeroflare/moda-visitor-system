using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace console.Services;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly IGoogleAuthService _authService;
    private readonly string _calendarId;
    private CalendarService? _cachedService;
    private readonly SemaphoreSlim _serviceLock = new(1, 1);

    public GoogleCalendarService(
        IConfiguration configuration,
        ILogger<GoogleCalendarService> logger,
        IGoogleAuthService authService)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _calendarId = _configuration["Google:CalendarId"] ?? "primary";
    }

    private async Task<CalendarService> GetCalendarServiceAsync(CancellationToken cancellationToken = default)
    {
        // 使用雙重檢查鎖定模式來快取 service
        if (_cachedService != null)
        {
            return _cachedService;
        }

        await _serviceLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedService != null)
            {
                return _cachedService;
            }

            var credential = await _authService.GetCredentialAsync(cancellationToken);
            
            _cachedService = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "TWDIW Visitor System Console"
            });

            return _cachedService;
        }
        finally
        {
            _serviceLock.Release();
        }
    }

    public async Task<List<CalendarEvent>> GetEventsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var service = await GetCalendarServiceAsync();
                
                // 如果未指定日期，使用台灣時區的明天
                var (tomorrowStart, tomorrowEnd) = GetTaiwanTomorrowRange();
                var timeMin = startDate ?? tomorrowStart;
                var timeMax = endDate ?? tomorrowEnd;

                _logger.LogInformation(
                    "Fetching calendar events from {StartDate:yyyy-MM-dd HH:mm:ss} (Taiwan) to {EndDate:yyyy-MM-dd HH:mm:ss} (Taiwan)", 
                    timeMin, timeMax);

                var request = service.Events.List(_calendarId);
                request.TimeMinDateTimeOffset = timeMin;
                request.TimeMaxDateTimeOffset = timeMax;
                request.SingleEvents = true;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                var eventsResult = await request.ExecuteAsync();
                var events = eventsResult.Items ?? new List<Event>();

                _logger.LogInformation("Found {Count} calendar events", events.Count);

                var calendarEvents = events.Select(e => MapToCalendarEvent(e)).ToList();

                return calendarEvents;
            }
            catch (Exception ex) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                _logger.LogWarning(ex, 
                    "Error fetching calendar events (attempt {Attempt}/{MaxRetries}), retrying...", 
                    retryCount, maxRetries);
                
                // 如果認證失敗，清除快取的 service
                if (ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                {
                    _cachedService = null;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching calendar events after {Attempt} attempts", retryCount + 1);
                throw;
            }
        }

        throw new InvalidOperationException("Failed to fetch calendar events after maximum retries");
    }

    private static CalendarEvent MapToCalendarEvent(Event e)
    {
        var start = ParseDateTime(e.Start);
        var end = ParseDateTime(e.End);
        
        // 取得邀請人（organizer）的 email
        var organizerEmail = e.Organizer?.Email;
        
        // 取得所有參加者的 email（包括資源、邀請人、一般參加者）
        // 包括所有 attendees，不管是否有 organizer 標記
        var attendees = e.Attendees?
            .Where(a => !string.IsNullOrWhiteSpace(a.Email))
            .Select(a => a.Email!)
            .ToList() ?? new List<string>();
        
        // 如果 organizer 不在 attendees 列表中，也加入
        if (!string.IsNullOrEmpty(organizerEmail) && !attendees.Contains(organizerEmail))
        {
            attendees.Add(organizerEmail);
        }

        return new CalendarEvent(
            Id: e.Id ?? string.Empty,
            Summary: e.Summary ?? "無標題會議",
            Description: e.Description,
            Start: start,
            End: end,
            Location: e.Location,
            OrganizerEmail: organizerEmail,
            Attendees: attendees
        );
    }

    private static (DateTime Start, DateTime End) GetTaiwanTomorrowRange()
    {
        // 取得台灣時區
        var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
        
        // 取得台灣時區的現在時間
        var nowTaiwan = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
        
        // 明天的開始時間（00:00:00）
        var tomorrow = nowTaiwan.AddDays(1);
        var tomorrowStart = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 0, 0, 0, DateTimeKind.Unspecified);
        tomorrowStart = TimeZoneInfo.ConvertTimeToUtc(tomorrowStart, taiwanTimeZone);
        
        // 明天的結束時間（23:59:59.999）
        var tomorrowEnd = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 23, 59, 59, 999, DateTimeKind.Unspecified);
        tomorrowEnd = TimeZoneInfo.ConvertTimeToUtc(tomorrowEnd, taiwanTimeZone);
        
        return (tomorrowStart, tomorrowEnd);
    }

    private static DateTime ParseDateTime(EventDateTime? eventDateTime)
    {
        if (eventDateTime?.DateTimeDateTimeOffset != null)
        {
            return eventDateTime.DateTimeDateTimeOffset.Value.DateTime;
        }
        
        if (eventDateTime?.Date != null && DateTime.TryParse(eventDateTime.Date, out var date))
        {
            return date;
        }
        
        return DateTime.MinValue;
    }

    public async Task SyncEventsToDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Fetching calendar events from Google Calendar API...");
            var events = await GetEventsAsync();
            
            if (events.Count == 0)
            {
                _logger.LogInformation("No calendar events found to sync");
                return;
            }

            _logger.LogInformation("Found {Count} calendar events to sync", events.Count);
            
            // 記錄所有事件的詳細資訊
            foreach (var evt in events)
            {
                var attendeeList = evt.Attendees != null && evt.Attendees.Any() 
                    ? string.Join(", ", evt.Attendees) 
                    : "無";
                
                _logger.LogInformation("  - Event: {Summary}", evt.Summary);
                _logger.LogInformation("    Start: {Start:yyyy-MM-dd HH:mm:ss} | End: {End:yyyy-MM-dd HH:mm:ss}", 
                    evt.Start, evt.End);
                _logger.LogInformation("    Organizer: {OrganizerEmail}", evt.OrganizerEmail ?? "N/A");
                _logger.LogInformation("    Attendees ({Count}): {Attendees}", 
                    evt.Attendees?.Count ?? 0, attendeeList);
            }

            // TODO: 實作寫入資料庫邏輯
            // 1. 檢查事件是否已存在
            // 2. 新增或更新事件資料
            // 3. 處理參與者資訊

            await Task.CompletedTask;
            
            _logger.LogInformation("Successfully processed {Count} calendar events (database sync pending)", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing calendar events to database: {Message}", ex.Message);
            throw;
        }
    }
}
