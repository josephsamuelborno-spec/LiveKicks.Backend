namespace LiveKicks.Backend.Models.AI;

/// <summary>
/// Team AI profile with complete statistics
/// Matches MAUI client DTO exactly
/// </summary>
public class TeamAIProfile
{
    // Basic Info
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;

    // League Context
    public int LeaguePosition { get; set; }
    public int Points { get; set; }
    public int MatchesPlayed { get; set; }
    public int GoalDifference { get; set; }

    // Overall Form (Last 10 matches)
    public FormStats Last10Matches { get; set; } = new();

    // Venue-Specific Form
    public FormStats Last5Home { get; set; } = new();
    public FormStats Last5Away { get; set; } = new();

    // Scoring Statistics
    public double AvgGoalsScored { get; set; }
    public double AvgGoalsConceded { get; set; }
    public double ExpectedGoals { get; set; }
    public double ExpectedGoalsAgainst { get; set; }

    // Shot Statistics
    public double AvgShotsPerMatch { get; set; }
    public double AvgShotsOnTarget { get; set; }
    public double ShotAccuracy { get; set; }

    // Possession & Control
    public double AvgPossession { get; set; }
    public double AvgPassAccuracy { get; set; }

    // Defensive Stats
    public int CleanSheets { get; set; }
    public double CleanSheetPercentage { get; set; }
    public int FailedToScoreCount { get; set; }
    public double FailedToScorePercentage { get; set; }

    // Form & Momentum
    public string FormString { get; set; } = string.Empty;
    public int CurrentWinStreak { get; set; }
    public int CurrentLoseStreak { get; set; }
    public string FormTrend { get; set; } = "STABLE";

    // Future-Ready Fields
    public List<InjuryInfo> Injuries { get; set; } = new();
    public LineupInfo? Lineup { get; set; }

    // Backward compatibility properties for older AI services
    public FormStats Form => Last10Matches;
    public int Standing => LeaguePosition;
    public int InjuryCount => Injuries?.Count ?? 0;
}

public class FormStats
{
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
    public int CleanSheets { get; set; }
    public int FailedToScore { get; set; }
    public double PointsPerGame { get; set; }
    public double GoalsPerGame { get; set; }
    public double GoalsConcededPerGame { get; set; }
    public string FormString { get; set; } = string.Empty;
    public int MatchCount { get; set; }
    public List<string> Last5Results { get; set; } = new();

    // Backward compatibility properties for older AI services
    public double GoalsForAvg => MatchCount > 0 ? (double)GoalsScored / MatchCount : 0;
    public double GoalsAgainstAvg => MatchCount > 0 ? (double)GoalsConceded / MatchCount : 0;
    public bool ScoredIn => GoalsScored > 0;
    public bool CleanSheet => CleanSheets > 0;
    public object? Home => null;
    public object? Away => null;
}

public class InjuryInfo
{
    public string PlayerName { get; set; } = string.Empty;
    public string InjuryType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class LineupInfo
{
    public string Formation { get; set; } = string.Empty;
    public List<string> StartingXI { get; set; } = new();
    public bool Confirmed { get; set; }
}
