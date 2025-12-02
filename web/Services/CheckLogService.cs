using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class CheckLogService : ICheckLogService
{
    private readonly ApplicationDbContext _context;

    public CheckLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CheckLogResponse>> GetCheckLogsAsync()
    {
        // 使用 Left Join 確保即使 meeting_id 無法關聯，check_logs 的記錄也會顯示
        return await _context.CheckLogs
            .GroupJoin(
                _context.Meetings,
                cl => cl.MeetingId,
                m => m.Id,
                (cl, meetings) => new { CheckLog = cl, Meetings = meetings }
            )
            .SelectMany(
                cm => cm.Meetings.DefaultIfEmpty(),
                (cm, meeting) => new { cm.CheckLog, Meeting = meeting }
            )
            .GroupJoin(
                _context.MeetingRooms,
                cm => cm.Meeting != null ? cm.Meeting.MeetingroomId : string.Empty,
                mr => mr.Id,
                (cm, meetingRooms) => new { cm.CheckLog, cm.Meeting, MeetingRooms = meetingRooms }
            )
            .SelectMany(
                cmr => cmr.MeetingRooms.DefaultIfEmpty(),
                (cmr, meetingRoom) => new CheckLogResponse
                {
                    Timestamp = cmr.CheckLog.CreatedAt,
                    Type = cmr.CheckLog.Type,
                    InviterEmail = cmr.Meeting != null ? cmr.Meeting.InviterEmail : null,
                    InviterName = cmr.Meeting != null ? cmr.Meeting.InviterName : null,
                    InviterDept = cmr.Meeting != null ? cmr.Meeting.InviterDept : null,
                    InviterTitle = cmr.Meeting != null ? cmr.Meeting.InviterTitle : null,
                    VistorEmail = cmr.CheckLog.VisitorEmail,
                    VistorName = cmr.CheckLog.VisitorName,
                    VistorDept = cmr.CheckLog.VisitorDept,
                    VistorPhone = cmr.CheckLog.VisitorPhone,
                    MeetingTime = cmr.Meeting != null 
                        ? $"{cmr.Meeting.StartAt:yyyy.MM.dd HH:mm} - {cmr.Meeting.EndAt:HH:mm}" 
                        : null,
                    MeetingName = cmr.Meeting != null ? cmr.Meeting.MeetingName : "没有會議",
                    MeetingRoom = meetingRoom != null ? meetingRoom.Name : null
                }
            )
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();
    }
}

