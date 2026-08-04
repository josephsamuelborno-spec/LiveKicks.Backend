namespace LiveKicks.Backend.Models.DTOs;

public class StatisticsDto
{
    public Team Team { get; set; } = new();
    public List<Statistic> Statistics { get; set; } = new();
}

public class Statistic
{
    public string Type { get; set; } = string.Empty;
    public object? Value { get; set; }
}
