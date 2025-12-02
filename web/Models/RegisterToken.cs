namespace web.Models;

public class RegisterToken
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiredAt { get; set; }
    public string VisitorEmail { get; set; } = string.Empty;
    public bool Used { get; set; }
    public DateTime? UsedAt { get; set; }
}

