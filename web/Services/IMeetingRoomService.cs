using web.Models;

namespace web.Services;

public interface IMeetingRoomService
{
    Task<IEnumerable<MeetingRoom>> GetAllMeetingRoomsAsync();
    Task<MeetingRoom?> GetMeetingRoomByIdAsync(string id);
    Task<IEnumerable<MeetingRoom>> GetMeetingRoomsByCounterIdAsync(string counterId);
    Task<MeetingRoom> CreateMeetingRoomAsync(MeetingRoom meetingRoom);
    Task<MeetingRoom?> UpdateMeetingRoomAsync(string id, MeetingRoom meetingRoom);
    Task<bool> DeleteMeetingRoomAsync(string id);
    
    // 包含櫃檯名稱的方法
    Task<IEnumerable<MeetingRoomResponse>> GetAllMeetingRoomsWithCounterNameAsync();
    Task<MeetingRoomResponse?> GetMeetingRoomByIdWithCounterNameAsync(string id);
    Task<IEnumerable<MeetingRoomResponse>> GetMeetingRoomsByCounterIdWithCounterNameAsync(string counterId);
}

