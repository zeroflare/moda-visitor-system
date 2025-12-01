namespace web.Models;

public class CheckLog
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = string.Empty;
    public string VisitorEmail { get; set; } = string.Empty;
    public string? VisitorName { get; set; }
    public string? VisitorPhone { get; set; }
    public string? VisitorDept { get; set; }
    public string MeetingId { get; set; } = string.Empty;
}

