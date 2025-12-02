using System.Text.Json.Serialization;

namespace web.Models;

public class CheckLogResponse
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
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
    
    [JsonPropertyName("meetingTime")]
    public string MeetingTime { get; set; } = string.Empty;
    
    [JsonPropertyName("meetingName")]
    public string? MeetingName { get; set; }
    
    [JsonPropertyName("meetingRoom")]
    public string MeetingRoom { get; set; } = string.Empty;
}

