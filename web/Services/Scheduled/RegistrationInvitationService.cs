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

            _logger.LogInformation("Querying visitors within 36 hours (UTC: {Now:yyyy-MM-dd HH:mm:ss} to {End:yyyy-MM-dd HH:mm:ss})", 
                now, timeRangeEnd);

            // 查詢36小時內的會議，並關聯 visitors（notified = false）
            var eligibleVisitors = await _context.Visitors
                .Join(
                    _context.Meetings,
                    v => v.MeetingId,
                    m => m.Id,
                    (v, m) => new { Visitor = v, Meeting = m }
                )
                .Where(vm => 
                    vm.Meeting.StartAt <= timeRangeEnd &&
                    !vm.Visitor.Notified)
                .Select(vm => vm.Visitor)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} visitors within 36 hours that need notification", eligibleVisitors.Count);

            // 取得 BaseUrl 配置
            var baseUrl = _configuration["BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("BaseUrl not configured. Skipping registration invitation emails.");
                return;
            }

            var emailsSent = 0;
            var emailsFailed = 0;
            var visitorsUpdated = 0;

            // 處理每個受邀人
            foreach (var visitor in eligibleVisitors)
            {
                try
                {
                    // 產生 token (UUID)
                    var token = Guid.NewGuid().ToString();

                    // 將 token 和 email 儲存到 Redis，有效期兩天
                    var cacheKey = $"register:token:{token}";
                    await _cacheService.SetAsync(cacheKey, visitor.VisitorEmail, TimeSpan.FromDays(2));

                    // 構建註冊 URL
                    var registerUrl = $"{baseUrl}/register?token={token}";

                    // 發送註冊邀請信
                    await _mailService.SendRegisterInvitationAsync(visitor.VisitorEmail, token, registerUrl);

                    // 標記該受邀人為已通知
                    visitor.Notified = true;
                    visitorsUpdated++;
                    emailsSent++;

                    _logger.LogInformation("Registration invitation sent to {Email} for meeting {MeetingId}", visitor.VisitorEmail, visitor.MeetingId);
                }
                catch (Exception ex)
                {
                    emailsFailed++;
                    _logger.LogError(ex, "Error sending registration invitation to {Email} for meeting {MeetingId}", visitor.VisitorEmail, visitor.MeetingId);
                    // 繼續處理下一個受邀人
                }
            }

            // 批量保存所有變更
            if (visitorsUpdated > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated {Count} visitors to notified=true", visitorsUpdated);
            }

            var invitationDuration = DateTime.UtcNow - invitationStartTime;
            _logger.LogInformation("--- Registration invitation emails completed in {Duration:mm\\:ss\\.fff} ---", invitationDuration);
            _logger.LogInformation("Sent: {Sent}, Failed: {Failed}, Visitors Updated: {Updated}", emailsSent, emailsFailed, visitorsUpdated);
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

