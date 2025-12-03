using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class VisitorLogService : IVisitorLogService
{
    private readonly ApplicationDbContext _context;

    public VisitorLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<VisitorLogResponse>> GetVisitorLogsAsync()
    {
        // 使用 Left Join 確保即使沒有會議室資料，會議也會顯示
        return await _context.Visitors
            .Join(
                _context.Meetings,
                v => v.MeetingId,
                m => m.Id,
                (v, m) => new { Visitor = v, Meeting = m }
            )
            .GroupJoin(
                _context.MeetingRooms,
                vm => vm.Meeting.MeetingroomId ?? string.Empty,
                mr => mr.Id,
                (vm, meetingRooms) => new { vm.Visitor, vm.Meeting, MeetingRooms = meetingRooms }
            )
            .SelectMany(
                vmr => vmr.MeetingRooms.DefaultIfEmpty(),
                (vmr, meetingRoom) => new
                {
                    Visitor = vmr.Visitor,
                    Meeting = vmr.Meeting,
                    MeetingRoom = meetingRoom
                }
            )
            .OrderByDescending(x => x.Meeting.StartAt)
            .Select(x => new VisitorLogResponse
            {
                CheckinTimestamp = x.Visitor.CheckinAt,
                CheckoutTimestamp = x.Visitor.CheckoutAt,
                InviterEmail = x.Meeting.InviterEmail,
                InviterName = x.Meeting.InviterName,
                InviterDept = x.Meeting.InviterDept,
                InviterTitle = x.Meeting.InviterTitle,
                VistorEmail = x.Visitor.VisitorEmail,
                VistorName = x.Visitor.VisitorName,
                VistorDept = x.Visitor.VisitorDept,
                VistorPhone = x.Visitor.VisitorPhone,
                MeetingStart = x.Meeting.StartAt.ToString("yyyy-MM-dd HH:mm"),
                MeetingEnd = x.Meeting.EndAt.ToString("yyyy-MM-dd HH:mm"),
                MeetingName = x.Meeting.MeetingName,
                MeetingRoom = x.MeetingRoom != null ? x.MeetingRoom.Name : null
            })
            .ToListAsync();
    }
}

