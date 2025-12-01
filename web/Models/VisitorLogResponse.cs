using System.Text.Json.Serialization;

namespace web.Models;

public class VisitorLogResponse
{
    [JsonPropertyName("checkinTimestamp")]
    public DateTime? CheckinTimestamp { get; set; }
    
    [JsonPropertyName("checkoutTimestamp")]
    public DateTime? CheckoutTimestamp { get; set; }
    
    [JsonPropertyName("inviterEmail")]
    public string InviterEmail { get; set; } = string.Empty;
    
    [JsonPropertyName("inviterName")]
    public string? InviterName { get; set; }
    
    [JsonPropertyName("inviterDept")]
    public string? InviterDept { get; set; }
    
    [JsonPropertyName("inviterTitle")]
    public string? InviterTitle { get; set; }
    
    [JsonPropertyName("vistorEmail")]
    public string VistorEmail { get; set; } = string.Empty;
    
    [JsonPropertyName("vistorName")]
    public string? VistorName { get; set; }
    
    [JsonPropertyName("vistorDept")]
    public string? VistorDept { get; set; }
    
    [JsonPropertyName("vistorPhone")]
    public string? VistorPhone { get; set; }
    
    [JsonPropertyName("meetingStart")]
    public string MeetingStart { get; set; } = string.Empty;
    
    [JsonPropertyName("meetingEnd")]
    public string MeetingEnd { get; set; } = string.Empty;
    
    [JsonPropertyName("meetingName")]
    public string? MeetingName { get; set; }
    
    [JsonPropertyName("meetingRoom")]
    public string MeetingRoom { get; set; } = string.Empty;
}

