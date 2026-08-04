namespace LiveKicks.Backend.Models.DTOs;

public class EventDto
{
    public Time Time { get; set; } = new();
    public Team Team { get; set; } = new();
    public Player Player { get; set; } = new();
    public Player? Assist { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

public class Time
{
    public int Elapsed { get; set; }
    public int? Extra { get; set; }
}

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
