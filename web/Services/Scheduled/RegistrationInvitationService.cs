using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using web.Data;

namespace web.Services.Scheduled;

public class RegistrationInvitationService : IRegistrationInvitationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IMailService _mailService;
    private readonly IConfiguration _configuration;
    private readonly IMeetingRoomService _meetingRoomService;
    private readonly ILogger<RegistrationInvitationService> _logger;

    public RegistrationInvitationService(
        ApplicationDbContext context,
        ICacheService cacheService,
        IMailService mailService,
        IConfiguration configuration,
        IMeetingRoomService meetingRoomService,
        ILogger<RegistrationInvitationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _meetingRoomService = meetingRoomService ?? throw new ArgumentNullException(nameof(meetingRoomService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendInvitationsToTomorrowVisitorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("--- Starting registration invitation emails ---");
        var invitationStartTime = DateTime.UtcNow;

        try
        {
            // 取得台灣時區的明天時間範圍
            var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
            var nowTaiwan = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
            var tomorrow = nowTaiwan.Date.AddDays(1);
            var tomorrowStart = TimeZoneInfo.ConvertTimeToUtc(tomorrow, taiwanTimeZone);
            var tomorrowEnd = TimeZoneInfo.ConvertTimeToUtc(tomorrow.AddDays(1).AddSeconds(-1), taiwanTimeZone);

            _logger.LogInformation("Querying visitors with meetings tomorrow (UTC: {Start:yyyy-MM-dd HH:mm:ss} to {End:yyyy-MM-dd HH:mm:ss})", 
                tomorrowStart, tomorrowEnd);

            // 查詢隔天有會議的受邀人，且會議的 meetingroom_id 存在於 meeting_room 資料庫
            var validVisitors = await _context.Visitors
                .Join(
                    _context.Meetings,
                    v => v.MeetingId,
                    m => m.Id,
                    (v, m) => new { Visitor = v, Meeting = m }
                )
                .Where(vm => 
                    vm.Meeting.StartAt >= tomorrowStart && 
                    vm.Meeting.StartAt <= tomorrowEnd &&
                    vm.Meeting.MeetingroomId != null)
                .Select(vm => new 
                { 
                    vm.Visitor.VisitorEmail, 
                    vm.Meeting.MeetingroomId,
                    vm.Meeting.Id
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} visitors with meetings tomorrow", validVisitors.Count);

            // 獲取所有存在的 meetingroom_id
            var allMeetingRooms = await _meetingRoomService.GetAllMeetingRoomsAsync();
            var existingMeetingRoomIds = allMeetingRooms.Select(mr => mr.Id).ToHashSet();

            _logger.LogInformation("Found {Count} meeting rooms in database", existingMeetingRoomIds.Count);

            // 過濾出 meetingroom_id 存在於資料庫的受邀人
            var eligibleVisitors = validVisitors
                .Where(v => v.MeetingroomId != null && existingMeetingRoomIds.Contains(v.MeetingroomId))
                .Select(v => v.VisitorEmail)
                .Distinct()
                .ToList();

            _logger.LogInformation("Found {Count} eligible visitors with valid meeting rooms", eligibleVisitors.Count);

            // 取得 BaseUrl 配置
            var baseUrl = _configuration["BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("BaseUrl not configured. Skipping registration invitation emails.");
                return;
            }

            var emailsSent = 0;
            var emailsFailed = 0;

            foreach (var email in eligibleVisitors)
            {
                try
                {
                    // 檢查是否已經發送過（避免重複發送）
                    var sentKey = $"register:invitation:sent:{email}:{tomorrow:yyyyMMdd}";
                    var alreadySent = await _cacheService.GetAsync(sentKey);
                    
                    if (!string.IsNullOrEmpty(alreadySent))
                    {
                        _logger.LogInformation("Registration invitation already sent to {Email} for {Date}. Skipping.", email, tomorrow.ToString("yyyy-MM-dd"));
                        continue;
                    }

                    // 產生 token (UUID)
                    var token = Guid.NewGuid().ToString();

                    // 將 token 和 email 儲存到 Redis，有效期兩天
                    var cacheKey = $"register:token:{token}";
                    await _cacheService.SetAsync(cacheKey, email, TimeSpan.FromDays(2));

                    // 構建註冊 URL
                    var registerUrl = $"{baseUrl}/register?token={token}";

                    // 發送註冊邀請信
                    await _mailService.SendRegisterInvitationAsync(email, token, registerUrl);

                    // 標記為已發送（有效期到明天結束）
                    var expiresAt = tomorrowEnd - DateTime.UtcNow;
                    await _cacheService.SetAsync(sentKey, "1", expiresAt);

                    emailsSent++;
                    _logger.LogInformation("Registration invitation sent to {Email}", email);
                }
                catch (Exception ex)
                {
                    emailsFailed++;
                    _logger.LogError(ex, "Error sending registration invitation to {Email}", email);
                    // 繼續處理下一個
                }
            }

            var invitationDuration = DateTime.UtcNow - invitationStartTime;
            _logger.LogInformation("--- Registration invitation emails completed in {Duration:mm\\:ss\\.fff} ---", invitationDuration);
            _logger.LogInformation("Sent: {Sent}, Failed: {Failed}", emailsSent, emailsFailed);
        }
        catch (Exception ex)
        {
            var invitationDuration = DateTime.UtcNow - invitationStartTime;
            _logger.LogError(ex, "Error during registration invitation emails");
            _logger.LogError("Error after {Duration:mm\\:ss\\.fff}", invitationDuration);
            throw;
        }
    }
}

