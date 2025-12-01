namespace web.Models;

public class Visitor
{
    public string VisitorEmail { get; set; } = string.Empty;
    public string? VisitorName { get; set; }
    public string? VisitorPhone { get; set; }
    public string? VisitorDept { get; set; }
    public DateTime? CheckinAt { get; set; }
    public DateTime? CheckoutAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string MeetingId { get; set; } = string.Empty;
}

