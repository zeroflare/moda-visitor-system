namespace web.Models;

public class Meeting
{
    public string Id { get; set; } = string.Empty;
    public string InviterEmail { get; set; } = string.Empty;
    public string? InviterName { get; set; }
    public string? InviterDept { get; set; }
    public string? InviterTitle { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string MeetingroomId { get; set; } = string.Empty;
}

