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
        return await _context.CheckLogs
            .Join(
                _context.Meetings,
                cl => cl.MeetingId,
                m => m.Id,
                (cl, m) => new { CheckLog = cl, Meeting = m }
            )
            .Join(
                _context.MeetingRooms,
                cm => cm.Meeting.MeetingroomId,
                mr => mr.Id,
                (cm, mr) => new CheckLogResponse
                {
                    Timestamp = cm.CheckLog.CreatedAt,
                    Type = cm.CheckLog.Type,
                    InviterEmail = cm.Meeting.InviterEmail,
                    InviterName = cm.Meeting.InviterName,
                    InviterDept = cm.Meeting.InviterDept,
                    InviterTitle = cm.Meeting.InviterTitle,
                    VistorEmail = cm.CheckLog.VisitorEmail,
                    VistorName = cm.CheckLog.VisitorName,
                    VistorDept = cm.CheckLog.VisitorDept,
                    VistorPhone = cm.CheckLog.VisitorPhone,
                    MeetingTime = $"{cm.Meeting.StartAt:yyyy-MM-dd HH:mm} - {cm.Meeting.EndAt:yyyy-MM-dd HH:mm}",
                    MeetingName = cm.Meeting.MeetingName,
                    MeetingRoom = mr.Name
                }
            )
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();
    }
}

