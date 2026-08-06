namespace LiveKicks.Backend.Models.AI;

/// <summary>
/// League behavioral profile
/// Matches MAUI client DTO exactly
/// </summary>
public class LeagueAIProfile
{
    public int LeagueId { get; set; }
    public string LeagueName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    // Scoring Characteristics
    public double AvgGoalsPerMatch { get; set; }
    public double Over25Percentage { get; set; }
    public double Under25Percentage { get; set; }
    public double BTTSPercentage { get; set; }

    // Result Patterns
    public double HomeWinPercentage { get; set; }
    public double DrawPercentage { get; set; }
    public double AwayWinPercentage { get; set; }

    // Tactical Characteristics
    public double DefensiveTendency { get; set; }
    public double HomeAdvantageStrength { get; set; }
    public double UpsetFrequency { get; set; }
    public double UnpredictabilityFactor { get; set; }

    // Data Quality
    public int MatchesAnalyzed { get; set; }
    public double ProfileReliability { get; set; }
    public double CompetitivenessRating { get; set; }

    // Backward compatibility properties for older AI services
    public string Name => LeagueName;
    public double DataCoverageQuality => ProfileReliability;
}
