namespace LiveKicks.Backend.Models.DTOs;

public class StandingsDto
{
    public League League { get; set; } = new();
}

public class StandingsLeague
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Flag { get; set; }
    public int Season { get; set; }
    public List<List<Standing>> Standings { get; set; } = new();
}

public class Standing
{
    public int Rank { get; set; }
    public Team Team { get; set; } = new();
    public int Points { get; set; }
    public int GoalsDiff { get; set; }
    public string Group { get; set; } = string.Empty;
    public string Form { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AllStats All { get; set; } = new();
    public AllStats Home { get; set; } = new();
    public AllStats Away { get; set; } = new();
    public DateTime Update { get; set; }
}

public class AllStats
{
    public int Played { get; set; }
    public int Win { get; set; }
    public int Draw { get; set; }
    public int Lose { get; set; }
    public GoalsFor Goals { get; set; } = new();
}

public class GoalsFor
{
    public int For { get; set; }
    public int Against { get; set; }
}
