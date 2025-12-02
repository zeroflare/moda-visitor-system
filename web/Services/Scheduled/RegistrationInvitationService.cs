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
            // 計算36小時內的時間範圍
            var now = DateTime.UtcNow;
            var timeRangeEnd = now.AddHours(36);

            _logger.LogInformation("Querying meetings within 36 hours (UTC: {Now:yyyy-MM-dd HH:mm:ss} to {End:yyyy-MM-dd HH:mm:ss})", 
                now, timeRangeEnd);

            // 查詢36小時內且 notified = false 的會議，且 meetingroom_id 存在於 meeting_room 資料庫
            var eligibleMeetings = await _context.Meetings
                .Where(m => 
                    m.StartAt >= now && 
                    m.StartAt <= timeRangeEnd &&
                    !m.Notified &&
                    m.MeetingroomId != null)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} eligible meetings within 36 hours that need notification", eligibleMeetings.Count);

            // 獲取所有存在的 meetingroom_id
            var allMeetingRooms = await _meetingRoomService.GetAllMeetingRoomsAsync();
            var existingMeetingRoomIds = allMeetingRooms.Select(mr => mr.Id).ToHashSet();

            _logger.LogInformation("Found {Count} meeting rooms in database", existingMeetingRoomIds.Count);

            // 過濾出 meetingroom_id 存在於資料庫的會議
            var validMeetings = eligibleMeetings
                .Where(m => existingMeetingRoomIds.Contains(m.MeetingroomId))
                .ToList();

            _logger.LogInformation("Found {Count} meetings with valid meeting rooms", validMeetings.Count);

            // 取得 BaseUrl 配置
            var baseUrl = _configuration["BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("BaseUrl not configured. Skipping registration invitation emails.");
                return;
            }

            var emailsSent = 0;
            var emailsFailed = 0;
            var meetingsUpdated = 0;

            // 處理每個會議
            foreach (var meeting in validMeetings)
            {
                try
                {
                    // 獲取該會議的所有受邀人
                    var visitors = await _context.Visitors
                        .Where(v => v.MeetingId == meeting.Id)
                        .Select(v => v.VisitorEmail)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    if (!visitors.Any())
                    {
                        _logger.LogInformation("Meeting {MeetingId} has no visitors. Skipping.", meeting.Id);
                        continue;
                    }

                    _logger.LogInformation("Processing meeting {MeetingId} with {VisitorCount} visitors", meeting.Id, visitors.Count);

                    var meetingEmailsSent = 0;
                    var meetingEmailsFailed = 0;

                    // 為每個受邀人發送邀請信
                    foreach (var email in visitors)
                    {
                        try
                        {
                            // 產生 token (UUID)
                            var token = Guid.NewGuid().ToString();

                            // 將 token 和 email 儲存到 Redis，有效期兩天
                            var cacheKey = $"register:token:{token}";
                            await _cacheService.SetAsync(cacheKey, email, TimeSpan.FromDays(2));

                            // 構建註冊 URL
                            var registerUrl = $"{baseUrl}/register?token={token}";

                            // 發送註冊邀請信
                            await _mailService.SendRegisterInvitationAsync(email, token, registerUrl);

                            meetingEmailsSent++;
                            emailsSent++;
                            _logger.LogInformation("Registration invitation sent to {Email} for meeting {MeetingId}", email, meeting.Id);
                        }
                        catch (Exception ex)
                        {
                            meetingEmailsFailed++;
                            emailsFailed++;
                            _logger.LogError(ex, "Error sending registration invitation to {Email} for meeting {MeetingId}", email, meeting.Id);
                            // 繼續處理下一個受邀人
                        }
                    }

                    // 如果至少有一封郵件發送成功，則標記會議為已通知
                    if (meetingEmailsSent > 0)
                    {
                        meeting.Notified = true;
                        meetingsUpdated++;
                        _logger.LogInformation("Marked meeting {MeetingId} as notified. Sent: {Sent}, Failed: {Failed}", 
                            meeting.Id, meetingEmailsSent, meetingEmailsFailed);
                    }
                    else
                    {
                        _logger.LogWarning("No emails sent for meeting {MeetingId}. Not marking as notified.", meeting.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing meeting {MeetingId}", meeting.Id);
                    // 繼續處理下一個會議
                }
            }

            // 批量保存所有變更
            if (meetingsUpdated > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated {Count} meetings to notified=true", meetingsUpdated);
            }

            var invitationDuration = DateTime.UtcNow - invitationStartTime;
            _logger.LogInformation("--- Registration invitation emails completed in {Duration:mm\\:ss\\.fff} ---", invitationDuration);
            _logger.LogInformation("Sent: {Sent}, Failed: {Failed}, Meetings Updated: {Updated}", emailsSent, emailsFailed, meetingsUpdated);
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

