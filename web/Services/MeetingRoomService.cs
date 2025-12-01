using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Models;

namespace web.Services;

public class MeetingRoomService : IMeetingRoomService
{
    private readonly ApplicationDbContext _context;

    public MeetingRoomService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MeetingRoom>> GetAllMeetingRoomsAsync()
    {
        return await _context.MeetingRooms.ToListAsync();
    }

    public async Task<MeetingRoom?> GetMeetingRoomByIdAsync(string id)
    {
        return await _context.MeetingRooms.FindAsync(id);
    }

    public async Task<IEnumerable<MeetingRoom>> GetMeetingRoomsByCounterIdAsync(string counterId)
    {
        return await _context.MeetingRooms
            .Where(m => m.CounterId == counterId)
            .ToListAsync();
    }

    public async Task<MeetingRoom> CreateMeetingRoomAsync(MeetingRoom meetingRoom)
    {
        _context.MeetingRooms.Add(meetingRoom);
        await _context.SaveChangesAsync();
        return meetingRoom;
    }

    public async Task<MeetingRoom?> UpdateMeetingRoomAsync(string id, MeetingRoom meetingRoom)
    {
        var existingMeetingRoom = await _context.MeetingRooms.FindAsync(id);
        if (existingMeetingRoom == null)
        {
            return null;
        }

        existingMeetingRoom.Name = meetingRoom.Name;
        existingMeetingRoom.CounterId = meetingRoom.CounterId;
        await _context.SaveChangesAsync();
        return existingMeetingRoom;
    }

    public async Task<bool> DeleteMeetingRoomAsync(string id)
    {
        var meetingRoom = await _context.MeetingRooms.FindAsync(id);
        if (meetingRoom == null)
        {
            return false;
        }

        _context.MeetingRooms.Remove(meetingRoom);
        await _context.SaveChangesAsync();
        return true;
    }
}

