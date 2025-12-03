using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using web.Data;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/vpwebhook")]
public class VpWebhookController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly INotifyWebhookService _notifyWebhookService;
    private readonly IGoogleChatService _googleChatService;
    private readonly ILogger<VpWebhookController> _logger;

    public VpWebhookController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ApplicationDbContext context,
        INotifyWebhookService notifyWebhookService,
        IGoogleChatService googleChatService,
        ILogger<VpWebhookController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _context = context;
        _notifyWebhookService = notifyWebhookService;
        _googleChatService = googleChatService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVpWebhook(string id)
    {
        try
        {
            var vpUrl = _configuration["Twdiw:VpUrl"];
            if (string.IsNullOrEmpty(vpUrl))
            {
                return BadRequest(new { error = "VpUrl 未配置" });
            }

            var url = $"{vpUrl}/api/verifier/deeplink/vp/{id}";
            
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return Ok(data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "取得 VP Webhook 資料失敗: {Id}", id);
            return StatusCode(500, new { error = "取得資料失敗" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetVpWebhook 發生錯誤: {Id}", id);
            return StatusCode(500, new { error = "發生錯誤" });
        }
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> PostVpWebhook(string id, [FromBody] JsonElement body)
    {
        try
        {
            // 解析 JSON body
            if (!body.TryGetProperty("data", out var dataArray) || 
                dataArray.ValueKind != JsonValueKind.Array || 
                dataArray.GetArrayLength() == 0)
            {
                return BadRequest(new { error = "無效的資料格式" });
            }

            var firstData = dataArray[0];
            if (!firstData.TryGetProperty("claims", out var claims) || 
                claims.ValueKind != JsonValueKind.Array)
            {
                return BadRequest(new { error = "無效的 claims 格式" });
            }

            // 提取 visitor 資訊
            string? visitorEmail = null;
            string? visitorName = null;
            string? visitorPhone = null;
            string? visitorDept = null;

            foreach (var claim in claims.EnumerateArray())
            {
                if (claim.TryGetProperty("ename", out var ename) && 
                    claim.TryGetProperty("value", out var value))
                {
                    var enameValue = ename.GetString();
                    var valueString = value.GetString();

                    switch (enameValue)
                    {
                        case "email":
                            visitorEmail = valueString;
                            break;
                        case "name":
                            visitorName = valueString;
                            break;
                        case "phone":
                            visitorPhone = valueString;
                            break;
                        case "company":
                            visitorDept = valueString;
                            break;
                    }
                }
            }

            if (string.IsNullOrEmpty(visitorEmail))
            {
                return BadRequest(new { error = "缺少 email 資訊" });
            }

            // 取得今天的日期範圍
            var today = DateTime.Today;
            var todayStart = today;
            var todayEnd = today.AddDays(1).AddTicks(-1);

            // 查找今天該 email 的會議（包含會議室資訊）
            var visitorWithMeeting = await _context.Meetings
                .Join(
                    _context.Visitors,
                    m => m.Id,
                    v => v.MeetingId,
                    (m, v) => new { Meeting = m, Visitor = v }
                )
                .Where(vm => 
                    vm.Visitor.VisitorEmail == visitorEmail &&
                    vm.Meeting.StartAt >= todayStart && 
                    vm.Meeting.StartAt <= todayEnd)
                .Join(
                    _context.MeetingRooms,
                    vm => vm.Meeting.MeetingroomId ?? string.Empty,
                    mr => mr.Id,
                    (vm, mr) => new { vm.Meeting, vm.Visitor, MeetingRoom = mr }
                )
                .FirstOrDefaultAsync();

            // 如果沒有找到會議室，再查一次不包含會議室的
            var meeting = visitorWithMeeting?.Meeting;
            if (meeting == null)
            {
                meeting = await _context.Meetings
                    .Join(
                        _context.Visitors,
                        m => m.Id,
                        v => v.MeetingId,
                        (m, v) => new { Meeting = m, Visitor = v }
                    )
                    .Where(vm => 
                        vm.Visitor.VisitorEmail == visitorEmail &&
                        vm.Meeting.StartAt >= todayStart && 
                        vm.Meeting.StartAt <= todayEnd)
                    .Select(vm => vm.Meeting)
                    .FirstOrDefaultAsync();
            }

            var meetingId = meeting?.Id ?? "NO_MEETING";

            // 寫入 check_logs 表
            try
            {
                var checkLog = new CheckLog
                {
                    // 加上8小時作為台灣時區
                    CreatedAt = DateTime.UtcNow.AddHours(8),
                    Type = "checkout",
                    VisitorEmail = visitorEmail,
                    VisitorName = visitorName,
                    VisitorPhone = visitorPhone,
                    VisitorDept = visitorDept,
                    MeetingId = meetingId
                };

                _context.CheckLogs.Add(checkLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("簽退記錄已寫入 check_logs: {Email}, MeetingId: {MeetingId}", visitorEmail, meetingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "寫入 check_logs 失敗");
                // 繼續執行，不影響後續流程
            }

            // 更新 visitors 表的 checkout_at（今天所有會議）
            try
            {
                var todayVisitors = await _context.Visitors
                    .Join(
                        _context.Meetings,
                        v => v.MeetingId,
                        m => m.Id,
                        (v, m) => new { Visitor = v, Meeting = m }
                    )
                    .Where(vm => 
                        vm.Visitor.VisitorEmail == visitorEmail &&
                        vm.Meeting.StartAt >= todayStart && 
                        vm.Meeting.StartAt <= todayEnd)
                    .Select(vm => vm.Visitor)
                    .ToListAsync();

                var checkoutTime = DateTime.UtcNow.AddHours(8);

                foreach (var visitor in todayVisitors)
                {
                    visitor.CheckoutAt = checkoutTime;
                }

                if (todayVisitors.Any())
                {
                    _context.Visitors.UpdateRange(todayVisitors);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("已更新 {Count} 筆訪客簽退時間: {Email}", todayVisitors.Count, visitorEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新 visitors 表 checkout_at 失敗");
                // 繼續執行，不影響主要流程
            }

            // 發送 Google Chat 通知給 admin
            try
            {
                var checkoutTime = DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm");
                var meetingName = meeting?.MeetingName ?? "未命名會議";
                var meetingRoom = visitorWithMeeting?.MeetingRoom?.Name ?? "未指定會議室";
                var inviterName = meeting?.InviterName ?? meeting?.InviterEmail ?? "未知";

                var message = $"🚪 訪客簽退通知\n\n" +
                             $"訪客姓名：{visitorName ?? "未知"}\n" +
                             $"訪客信箱：{visitorEmail}\n" +
                             $"會議名稱：{meetingName}\n" +
                             $"會議室：{meetingRoom}\n" +
                             $"邀請人：{inviterName}\n" +
                             $"簽退時間：{checkoutTime}";

                var adminWebhook = await _notifyWebhookService.GetNotifyWebhookByDeptAndTypeAsync("admin", "googlechat");
                if (adminWebhook != null && !string.IsNullOrEmpty(adminWebhook.Webhook))
                {
                    try
                    {
                        await _googleChatService.SendNotificationAsync(adminWebhook.Webhook, message);
                        _logger.LogInformation("已發送簽退通知給 admin: {Email}", visitorEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "發送 admin Google Chat 通知失敗");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "發送簽退通知失敗");
                // 不影響主要流程，繼續執行
            }

            return Ok(new { message = "簽退成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostVpWebhook 發生錯誤: {Id}", id);
            return StatusCode(500, new { error = "發生錯誤" });
        }
    }
}

