namespace web.Models;

public class Employee
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Dept { get; set; }
    public string? Costcenter { get; set; }
    public string? Title { get; set; }
}

