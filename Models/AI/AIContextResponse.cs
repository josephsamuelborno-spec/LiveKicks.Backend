namespace LiveKicks.Backend.Models.AI;

/// <summary>
/// Complete AI intelligence package for a fixture
/// Matches MAUI client DTO exactly
/// </summary>
public class AIContextResponse
{
    public FixtureInfo Fixture { get; set; } = new();
    public TeamAIProfile HomeTeam { get; set; } = new();
    public TeamAIProfile AwayTeam { get; set; } = new();
    public HeadToHeadInfo HeadToHead { get; set; } = new();
    public LeagueAIProfile LeagueProfile { get; set; } = new();
    public MarketOddsInfo MarketOdds { get; set; } = new();
    public DataQualityInfo DataQuality { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool FromCache { get; set; }

    // Backward compatibility property for older AI services
    public LeagueAIProfile League => LeagueProfile;
}

public class FixtureInfo
{
    public int FixtureId { get; set; }
    public DateTime Date { get; set; }
    public string HomeTeamName { get; set; } = string.Empty;
    public string AwayTeamName { get; set; } = string.Empty;
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public string LeagueName { get; set; } = string.Empty;
    public int LeagueId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;

    // Backward compatibility properties for older AI services
    public string HomeTeam => HomeTeamName;
    public string AwayTeam => AwayTeamName;
    public DateTime KickoffTime => Date;
}

public class HeadToHeadInfo
{
    public int TotalMeetings { get; set; }
    public int HomeWins { get; set; }
    public int Draws { get; set; }
    public int AwayWins { get; set; }
    public double AvgTotalGoals { get; set; }
    public double BTTSPercentage { get; set; }
    public double Over25Percentage { get; set; }
    public List<string> Last5Results { get; set; } = new();
    public string RecentTrend { get; set; } = string.Empty;

    // Backward compatibility property for older AI services
    public int TotalMatches => HomeWins + AwayWins + Draws;
}

public class MarketOddsInfo
{
    public double HomeWinOdds { get; set; }
    public double DrawOdds { get; set; }
    public double AwayWinOdds { get; set; }
    public double Over25Odds { get; set; }
    public double Under25Odds { get; set; }
    public double BTTSYesOdds { get; set; }
    public bool OddsAvailable { get; set; }

    // Backward compatibility properties for older AI services
    public double HomeWin => HomeWinOdds;
    public double AwayWin => AwayWinOdds;
    public double Draw => DrawOdds;
    public double Over25 => Over25Odds;
    public double Under25 => Under25Odds;
}

public class DataQualityInfo
{
    public double OverallScore { get; set; } = 100.0;
    public bool HasRecentMatches { get; set; }
    public bool HasHomeAwayForm { get; set; }
    public bool HasHeadToHead { get; set; }
    public bool HasLeagueProfile { get; set; }
    public bool HasOdds { get; set; }
    public bool HasStandings { get; set; }
    public List<string> MissingData { get; set; } = new();
    public string Reliability { get; set; } = "UNKNOWN";

    // Backward compatibility properties for older AI services
    public double CompletenessScore => OverallScore;
    public double RecencyScore => HasRecentMatches ? 100.0 : 50.0;
    public double ReliabilityScore => Reliability == "HIGH" ? 100.0 : Reliability == "MEDIUM" ? 70.0 : 50.0;
    public double OverallQuality => OverallScore;
}
