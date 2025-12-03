using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using web.Data;
using web.Models;

namespace web.Services;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly IGoogleAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly IEmployeeService _employeeService;
    private readonly string _calendarId;
    private CalendarService? _cachedService;
    private readonly SemaphoreSlim _serviceLock = new(1, 1);

    public GoogleCalendarService(
        IConfiguration configuration,
        ILogger<GoogleCalendarService> logger,
        IGoogleAuthService authService,
        ApplicationDbContext context,
        IEmployeeService employeeService)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
        _calendarId = _configuration["Google:CalendarId"] ?? "primary";
    }

    private async Task<CalendarService> GetCalendarServiceAsync(CancellationToken cancellationToken = default)
    {
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
                ApplicationName = "TWDIW Visitor System"
            });

            return _cachedService;
        }
        finally
        {
            _serviceLock.Release();
        }
    }

    public async Task SyncEventsToDatabaseAsync(CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var service = await GetCalendarServiceAsync(cancellationToken);
                
                // 獲取今天和明天的時間範圍（台灣時區）
                var (todayStart, tomorrowEnd) = GetTodayAndTomorrowRange();
                
                _logger.LogInformation(
                    "Fetching calendar events from {StartDate:yyyy-MM-dd HH:mm:ss} to {EndDate:yyyy-MM-dd HH:mm:ss} (Taiwan time)", 
                    todayStart, tomorrowEnd);

                // 獲取所有日曆列表（包括被邀請的日曆）
                var calendarList = await GetCalendarListAsync(service, cancellationToken);
                _logger.LogInformation("Found {Count} calendars to check", calendarList.Count);

                var allEvents = new List<Event>();
                
                // 從每個日曆獲取事件
                foreach (var calendar in calendarList)
                {
                    try
                    {
                        var request = service.Events.List(calendar.Id);
                        request.TimeMinDateTimeOffset = todayStart;
                        request.TimeMaxDateTimeOffset = tomorrowEnd;
                        request.SingleEvents = true;
                        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
                        // 包含所有事件，不管是否接受邀請
                        request.ShowDeleted = false;

                        var eventsResult = await request.ExecuteAsync(cancellationToken);
                        var events = eventsResult.Items ?? new List<Event>();
                        
                        if (events.Any())
                        {
                            _logger.LogInformation("Found {Count} events in calendar: {CalendarSummary}", events.Count, calendar.Summary ?? calendar.Id);
                            allEvents.AddRange(events);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error fetching events from calendar: {CalendarId}", calendar.Id);
                        // 繼續處理其他日曆
                    }
                }

                _logger.LogInformation("Total events found: {Count}", allEvents.Count);

                // 去重（同一個事件可能出現在多個日曆中）
                var uniqueEvents = allEvents
                    .GroupBy(e => e.Id)
                    .Select(g => g.First())
                    .ToList();

                _logger.LogInformation("Unique events after deduplication: {Count}", uniqueEvents.Count);

                // 同步到資料庫
                await SyncEventsToDatabaseInternalAsync(uniqueEvents, cancellationToken);

                return;
            }
            catch (Exception ex) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                _logger.LogWarning(ex, 
                    "Error syncing calendar events (attempt {Attempt}/{MaxRetries}), retrying...", 
                    retryCount, maxRetries);
                
                if (ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                {
                    _cachedService = null;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing calendar events after {Attempt} attempts", retryCount + 1);
                throw;
            }
        }

        throw new InvalidOperationException("Failed to sync calendar events after maximum retries");
    }

    private async Task<List<CalendarListEntry>> GetCalendarListAsync(CalendarService service, CancellationToken cancellationToken)
    {
        var calendarList = new List<CalendarListEntry>();
        string? pageToken = null;

        do
        {
            var request = service.CalendarList.List();
            request.MinAccessRole = CalendarListResource.ListRequest.MinAccessRoleEnum.Reader;
            request.ShowHidden = false;
            if (!string.IsNullOrEmpty(pageToken))
            {
                request.PageToken = pageToken;
            }

            var result = await request.ExecuteAsync(cancellationToken);
            if (result.Items != null)
            {
                calendarList.AddRange(result.Items);
            }
            pageToken = result.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return calendarList;
    }

    private async Task SyncEventsToDatabaseInternalAsync(List<Event> events, CancellationToken cancellationToken)
    {
        var syncedMeetings = 0;
        var syncedVisitors = 0;

        // 批量獲取所有現有的 meeting IDs 和 visitor 組合
        var eventIds = events.Where(e => !string.IsNullOrEmpty(e.Id)).Select(e => e.Id!).ToList();
        var existingMeetings = await _context.Meetings
            .Where(m => eventIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);
        
        var existingVisitorKeyList = await _context.Visitors
            .Where(v => eventIds.Contains(v.MeetingId))
            .Select(v => $"{v.MeetingId}|{v.VisitorEmail}")
            .ToListAsync(cancellationToken);
        var existingVisitorKeys = existingVisitorKeyList.ToHashSet();

        var now = DateTime.UtcNow;

        foreach (var evt in events)
        {
            try
            {
                if (string.IsNullOrEmpty(evt.Id))
                {
                    _logger.LogWarning("Skipping event with empty ID");
                    continue;
                }

                // 解析事件資訊
                var organizerEmail = evt.Creator?.Email;
                if (string.IsNullOrEmpty(organizerEmail))
                {
                    _logger.LogWarning("Skipping event {EventId} with no creator", evt.Id);
                    continue;
                }

                var startTime = ParseDateTime(evt.Start);
                var endTime = ParseDateTime(evt.End);
                
                if (startTime == DateTime.MinValue || endTime == DateTime.MinValue)
                {
                    _logger.LogWarning("Skipping event {EventId} with invalid start/end time", evt.Id);
                    continue;
                }

                // 從 employees 表獲取 inviter 的 dept 和 title
                var inviterEmployee = await _employeeService.GetEmployeeByEmailAsync(organizerEmail);
                var inviterDept = inviterEmployee?.Dept;
                var inviterTitle = inviterEmployee?.Title;
                var inviterName = inviterEmployee?.Name;

                // 識別會議室（resource calendar email）
                string? meetingRoomId = null;
                var attendees = evt.Attendees ?? new List<EventAttendee>();
                
                // 查找 resource calendar（格式：c_xxx@resource.calendar.google.com）
                var resourceAttendee = attendees.FirstOrDefault(a => 
                    !string.IsNullOrEmpty(a.Email) && 
                    a.Email.EndsWith("@resource.calendar.google.com", StringComparison.OrdinalIgnoreCase));
                
                if (resourceAttendee != null && !string.IsNullOrEmpty(resourceAttendee.Email))
                {
                    meetingRoomId = resourceAttendee.Email;
                }

                // 同步或更新 meeting
                if (existingMeetings.TryGetValue(evt.Id, out var existingMeeting))
                {
                    // 更新現有會議
                    existingMeeting.MeetingName = evt.Summary ?? "無標題會議";
                    existingMeeting.InviterEmail = organizerEmail;
                    existingMeeting.InviterName = inviterName;
                    existingMeeting.InviterDept = inviterDept;
                    existingMeeting.InviterTitle = inviterTitle;
                    existingMeeting.StartAt = startTime;
                    existingMeeting.EndAt = endTime;
                    existingMeeting.MeetingroomId = meetingRoomId;
                }
                else
                {
                    // 創建新會議
                    var meeting = new Meeting
                    {
                        Id = evt.Id,
                        MeetingName = evt.Summary ?? "無標題會議",
                        InviterEmail = organizerEmail,
                        InviterName = inviterName,
                        InviterDept = inviterDept,
                        InviterTitle = inviterTitle,
                        StartAt = startTime,
                        EndAt = endTime,
                        MeetingroomId = meetingRoomId
                    };
                    _context.Meetings.Add(meeting);
                    existingMeetings[evt.Id] = meeting; // 加入字典以便後續使用
                }

                syncedMeetings++;

                // 為每個受邀請的人新增 visitor 記錄（排除會議室和發起人）
                var attendeeEmails = attendees
                    .Where(a => !string.IsNullOrEmpty(a.Email) && 
                               !a.Email.EndsWith("@resource.calendar.google.com", StringComparison.OrdinalIgnoreCase) &&
                               a.Email != organizerEmail)
                    .Select(a => a.Email!)
                    .Distinct()
                    .ToList();

                foreach (var attendeeEmail in attendeeEmails)
                {
                    var visitorKey = $"{evt.Id}|{attendeeEmail}";
                    if (!existingVisitorKeys.Contains(visitorKey))
                    {
                        var visitor = new Visitor
                        {
                            VisitorEmail = attendeeEmail,
                            MeetingId = evt.Id,
                            CreatedAt = now
                        };
                        _context.Visitors.Add(visitor);
                        existingVisitorKeys.Add(visitorKey);
                        syncedVisitors++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing event {EventId}: {Message}", evt.Id, ex.Message);
                // 繼續處理下一個事件
            }
        }

        // 批量保存所有變更
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Calendar sync completed: {Meetings} meetings synced, {Visitors} visitors added", 
            syncedMeetings, syncedVisitors);
    }

    private static (DateTime Start, DateTime End) GetTodayAndTomorrowRange()
    {
        // 取得台灣時區
        var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
        
        // 取得台灣時區的現在時間
        var nowTaiwan = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
        
        // 今天的開始時間（00:00:00）
        var today = nowTaiwan.Date;
        var todayStart = TimeZoneInfo.ConvertTimeToUtc(today, taiwanTimeZone);
        
        // 明天的結束時間（23:59:59.999）
        var tomorrow = today.AddDays(2); // 後天的開始
        var tomorrowEnd = TimeZoneInfo.ConvertTimeToUtc(tomorrow.AddSeconds(-1), taiwanTimeZone);
        
        return (todayStart, tomorrowEnd);
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
}

