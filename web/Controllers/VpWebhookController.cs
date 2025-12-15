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
    private readonly ICacheService _cacheService;
    private readonly ILogger<VpWebhookController> _logger;

    public VpWebhookController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ApplicationDbContext context,
        INotifyWebhookService notifyWebhookService,
        IGoogleChatService googleChatService,
        ICacheService cacheService,
        ILogger<VpWebhookController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _context = context;
        _notifyWebhookService = notifyWebhookService;
        _googleChatService = googleChatService;
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVpWebhook(string id)
    {
        try
        {
            var vpUrl = _configuration["Twdiw:VpUrl"];
            var vpToken = _configuration["Twdiw:VpToken"];
            
            if (string.IsNullOrEmpty(vpUrl))
            {
                return BadRequest(new { code = "1001", message = "缺少參數或參數不合法" });
            }

            if (string.IsNullOrEmpty(vpToken))
            {
                return StatusCode(500, new { code = "500", message = "伺服器內部錯誤，請聯絡客服人員處理" });
            }

            // 產生 UUID 作為 transactionId
            var transactionId = Guid.NewGuid().ToString();
            
            // 將 transactionId 存入 Redis 白名單，有效期1分鐘
            var whitelistKey = $"vpwebhook:whitelist:{transactionId}";
            await _cacheService.SetAsync(whitelistKey, "1", TimeSpan.FromMinutes(1));
            
            var url = $"{vpUrl}/api/oidvp/qrcode?ref={Uri.EscapeDataString(id)}&transactionId={transactionId}&isCallback=Y";
            
            var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Access-Token", vpToken);

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            // 提取 authUri
            string? authUri = null;
            if (data.TryGetProperty("authUri", out var authUriElement))
            {
                authUri = authUriElement.GetString();
            }

            if (string.IsNullOrEmpty(authUri))
            {
                _logger.LogWarning("API 回應中缺少 authUri: {Response}", json);
                return StatusCode(500, new { code = "500", message = "伺服器內部錯誤，請聯絡客服人員處理" });
            }

            // 整理回傳資料
            return Ok(new
            {
                code = "0",
                message = "SUCCESS",
                data = new
                {
                    deepLink = authUri
                }
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "取得 VP Webhook 資料失敗: {Id}", id?.Replace("\r", "").Replace("\n", "").Replace("\t", ""));
            return StatusCode(500, new { code = "500", message = "伺服器內部錯誤，請聯絡客服人員處理" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetVpWebhook 發生錯誤: {Id}", id?.Replace("\r", "").Replace("\n", "").Replace("\t", ""));
            return StatusCode(500, new { code = "500", message = "伺服器內部錯誤，請聯絡客服人員處理" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> PostVpWebhook([FromBody] JsonElement body)
    {
        try
        {
            // 檢查 transactionId 是否在白名單中
            string? transactionId = null;
            if (body.TryGetProperty("transactionId", out var transactionIdElement))
            {
                transactionId = transactionIdElement.GetString();
            }

            if (string.IsNullOrEmpty(transactionId))
            {
                return BadRequest(new { code = "1001", message = "缺少參數或參數不合法" });
            }

            var whitelistKey = $"vpwebhook:whitelist:{transactionId}";
            var isInWhitelist = await _cacheService.GetAsync(whitelistKey);
            
            if (string.IsNullOrEmpty(isInWhitelist))
            {
                return BadRequest(new { code = "1001", message = "缺少參數或參數不合法" });
            }

            // 確認後從 Redis 移除
            await _cacheService.DeleteAsync(whitelistKey);

            // 解析 JSON body
            if (!body.TryGetProperty("data", out var dataArray) || 
                dataArray.ValueKind != JsonValueKind.Array || 
                dataArray.GetArrayLength() == 0)
            {
                return BadRequest(new { code = "1001", message = "缺少參數或參數不合法" });
            }

            var firstData = dataArray[0];
            if (!firstData.TryGetProperty("claims", out var claims) || 
                claims.ValueKind != JsonValueKind.Array)
            {
                return BadRequest(new { code = "1001", message = "缺少參數或參數不合法" });
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
                return BadRequest(new { code = "1001", message = "缺少參數或參數不合法" });
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新 visitors 表 checkout_at 失敗");
                // 繼續執行，不影響主要流程
            }

            // 發送 Google Chat 通知
            try
            {
                var checkoutTime = DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm");
                var meetingName = meeting?.MeetingName ?? "未命名會議";
                var meetingRoom = visitorWithMeeting?.MeetingRoom?.Name ?? "未指定會議室";
                var inviterName = meeting?.InviterName ?? meeting?.InviterEmail ?? "未知";
                var inviterDept = meeting?.InviterDept;
                var inviterEmail = meeting?.InviterEmail;

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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "發送 admin Google Chat 通知失敗");
                    }
                }

                // 通知邀請人同單位的 Google Chat
                if (!string.IsNullOrEmpty(inviterDept))
                {
                    var deptWebhook = await _notifyWebhookService.GetNotifyWebhookByDeptAndTypeAsync(inviterDept, "googlechat");
                    if (deptWebhook != null && !string.IsNullOrEmpty(deptWebhook.Webhook))
                    {
                        try
                        {
                            await _googleChatService.SendNotificationAsync(deptWebhook.Webhook, message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "發送 dept Google Chat 通知失敗: {Dept}", inviterDept);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "發送簽退通知失敗");
                // 不影響主要流程，繼續執行
            }

            return Ok(new { code = "0", message = "SUCCESS" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostVpWebhook 發生錯誤");
            return StatusCode(500, new { code = "500", message = "伺服器內部錯誤，請聯絡客服人員處理" });
        }
    }
}

