using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckinController : ControllerBase
{
    private readonly ITwdiwService _twdiwService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CheckinController> _logger;

    public CheckinController(
        ITwdiwService twdiwService,
        ApplicationDbContext context,
        ILogger<CheckinController> logger)
    {
        _twdiwService = twdiwService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("qrcode")]
    public async Task<IActionResult> GetQRCode([FromQuery] string transactionId, [FromQuery] string? counter)
    {
        if (string.IsNullOrEmpty(transactionId) || !IsUUID(transactionId))
        {
            return BadRequest(new { error = true, message = "transactionId 格式錯誤，需為 UUID" });
        }

        try
        {
            var result = await _twdiwService.GetQRCodeAsync(transactionId, counter);
            return Ok(result);
        }
        catch (HttpRequestException)
        {
            return StatusCode(500, new { error = true, message = "取得 QR Code 失敗" });
        }
    }

    [HttpGet("result")]
    public async Task<IActionResult> GetResult([FromQuery] string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId) || !IsUUID(transactionId))
        {
            return BadRequest(new { error = true, message = "transactionId 格式錯誤，需為 UUID" });
        }

        try
        {
            // 先從外部 API 取得訪客資訊
            var twdiwResult = await _twdiwService.GetCheckinResultAsync(transactionId);

            if (string.IsNullOrEmpty(twdiwResult.VisitorEmail))
            {
                return BadRequest(new { message = "等待驗證" });
            }

            // 取得今天的日期範圍（從 00:00:00 到 23:59:59）
            var today = DateTime.Today;
            var todayStart = today;
            var todayEnd = today.AddDays(1).AddTicks(-1);

            // 從資料庫查詢今天是否有會議
            // 透過 visitor_email 查找 visitor，再關聯 meeting 表檢查 start_at 是否是今天
            // 使用 Select 來安全地處理可能為 NULL 的字段
            var visitorWithMeeting = await (from visitor in _context.Visitors
                                            where visitor.VisitorEmail == twdiwResult.VisitorEmail
                                            join meeting in _context.Meetings on visitor.MeetingId equals meeting.Id
                                            where meeting.StartAt >= todayStart && meeting.StartAt <= todayEnd
                                            join meetingRoom in _context.MeetingRooms on meeting.MeetingroomId equals meetingRoom.Id
                                            select new
                                            {
                                                Visitor = visitor,
                                                Meeting = meeting,
                                                MeetingRoom = meetingRoom
                                            })
                                           .FirstOrDefaultAsync();

            // 統一使用 "checkin" 作為類型
            string checkType = "checkin";
            string meetingId = "NO_MEETING"; // 如果找不到會議，使用特殊值

            if (visitorWithMeeting != null)
            {
                meetingId = visitorWithMeeting.Meeting.Id;
            }

            // 寫入 check_log（無論是否找到會議都要寫入）
            try
            {
                var checkLog = new CheckLog
                {
                    CreatedAt = DateTime.UtcNow,
                    Type = checkType,
                    VisitorEmail = twdiwResult.VisitorEmail,
                    VisitorName = twdiwResult.VisitorName,
                    VisitorPhone = twdiwResult.VisitorPhone,
                    VisitorDept = twdiwResult.VisitorDept,
                    MeetingId = meetingId
                };

                _context.CheckLogs.Add(checkLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "寫入 check_log 失敗");
                // 不影響主要流程，繼續執行
            }

            // 如果今天沒有會議，返回錯誤訊息
            if (visitorWithMeeting == null)
            {
                return Unauthorized(new { message = "今天沒有會議" });
            }

            // 確認有會議後，回寫資料到 visitors 表
            // 使用 email + meeting_id 篩選
            try
            {
                var visitor = await _context.Visitors
                    .FirstOrDefaultAsync(v =>
                        v.VisitorEmail == twdiwResult.VisitorEmail &&
                        v.MeetingId == visitorWithMeeting.Meeting.Id);

                if (visitor != null)
                {
                    // 更新 visitor_name
                    visitor.VisitorName = twdiwResult.VisitorName;
                    visitor.VisitorPhone = twdiwResult.VisitorPhone;
                    visitor.VisitorDept = twdiwResult.VisitorDept;


                    // 更新 checkin_at：如果是空白，就寫入現在時間；如果已有值，就不寫入
                    if (!visitor.CheckinAt.HasValue)
                    {
                        visitor.CheckinAt = DateTime.UtcNow;
                    }

                    _context.Visitors.Update(visitor);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "更新 visitors 表失敗");
                // 不影響主要流程，繼續執行
            }

            // 返回正確的邀請者、受邀人、會議資訊
            var result = new CheckinResultResponse(
                InviterEmail: visitorWithMeeting.Meeting.InviterEmail,
                InviterName: visitorWithMeeting.Meeting.InviterName ?? string.Empty,
                InviterDept: visitorWithMeeting.Meeting.InviterDept ?? string.Empty,
                InviterTitle: visitorWithMeeting.Meeting.InviterTitle ?? string.Empty,
                VisitorEmail: twdiwResult.VisitorEmail,
                VisitorName: twdiwResult.VisitorName ?? visitorWithMeeting.Visitor.VisitorName,
                VisitorDept: twdiwResult.VisitorDept ?? visitorWithMeeting.Visitor.VisitorDept,
                VisitorPhone: twdiwResult.VisitorPhone ?? visitorWithMeeting.Visitor.VisitorPhone,
                MeetingTime: $"{visitorWithMeeting.Meeting.StartAt:yyyy-MM-dd HH:mm} - {visitorWithMeeting.Meeting.EndAt:yyyy-MM-dd HH:mm}",
                MeetingName: visitorWithMeeting.Meeting.MeetingName ?? string.Empty,
                MeetingRoom: visitorWithMeeting.MeetingRoom.Name
            );

            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.Message == "等待驗證")
        {
            return BadRequest(new { message = "等待驗證" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetResult exception: {Message}", ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            Console.WriteLine($"GetResult exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            return StatusCode(500, new { error = true, message = "發生錯誤" });
        }
    }

    [HttpGet("info")]
    public IActionResult GetCounterInfo([FromQuery] string counter)
    {
        // TODO: 實作從資料庫或設c定檔取得櫃檯資訊
        return NotFound(new { message = "Counter not found" });
    }

    private static bool IsUUID(string value)
    {
        var uuidRegex = new Regex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$", RegexOptions.IgnoreCase);
        return uuidRegex.IsMatch(value);
    }

}

