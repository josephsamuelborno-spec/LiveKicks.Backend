namespace LiveKicks.Backend.Models.DTOs;

public class FixtureDto
{
    public Fixture Fixture { get; set; } = new();
    public League League { get; set; } = new();
    public Teams Teams { get; set; } = new();
    public Goals Goals { get; set; } = new();
    public Score Score { get; set; } = new();
}

public class Fixture
{
    public int Id { get; set; }
    public string? Referee { get; set; }
    public string Timezone { get; set; } = "UTC";
    public DateTime Date { get; set; }
    public long Timestamp { get; set; }
    public Periods? Periods { get; set; }
    public Venue Venue { get; set; } = new();
    public Status Status { get; set; } = new();
}

public class Periods
{
    public long? First { get; set; }
    public long? Second { get; set; }
}

public class Venue
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? City { get; set; }
}

public class Status
{
    public string Long { get; set; } = string.Empty;
    public string Short { get; set; } = string.Empty;
    public int? Elapsed { get; set; }
}

public class League
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Flag { get; set; }
    public int Season { get; set; }
    public string Round { get; set; } = string.Empty;
}

public class Teams
{
    public Team Home { get; set; } = new();
    public Team Away { get; set; } = new();
}

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public bool? Winner { get; set; }
}

public class Goals
{
    public int? Home { get; set; }
    public int? Away { get; set; }
}

public class Score
{
    public GoalDetail Halftime { get; set; } = new();
    public GoalDetail Fulltime { get; set; } = new();
    public GoalDetail? Extratime { get; set; }
    public GoalDetail? Penalty { get; set; }
}

public class GoalDetail
{
    public int? Home { get; set; }
    public int? Away { get; set; }
}
