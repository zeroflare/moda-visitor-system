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
        return await _context.Visitors
            .Join(
                _context.Meetings,
                v => v.MeetingId,
                m => m.Id,
                (v, m) => new { Visitor = v, Meeting = m }
            )
            .Join(
                _context.MeetingRooms,
                vm => vm.Meeting.MeetingroomId,
                mr => mr.Id,
                (vm, mr) => new VisitorLogResponse
                {
                    CheckinTimestamp = vm.Visitor.CheckinAt,
                    CheckoutTimestamp = vm.Visitor.CheckoutAt,
                    InviterEmail = vm.Meeting.InviterEmail,
                    InviterName = vm.Meeting.InviterName,
                    InviterDept = vm.Meeting.InviterDept,
                    InviterTitle = vm.Meeting.InviterTitle,
                    VistorEmail = vm.Visitor.VisitorEmail,
                    VistorName = vm.Visitor.VisitorName,
                    VistorDept = vm.Visitor.VisitorDept,
                    VistorPhone = vm.Visitor.VisitorPhone,
                    MeetingStart = vm.Meeting.StartAt.ToString("yyyy-MM-dd HH:mm"),
                    MeetingEnd = vm.Meeting.EndAt.ToString("yyyy-MM-dd HH:mm"),
                    MeetingName = vm.Meeting.MeetingName,
                    MeetingRoom = mr.Name
                }
            )
            .OrderByDescending(x => x.CheckinTimestamp ?? DateTime.MinValue)
            .ToListAsync();
    }
}

