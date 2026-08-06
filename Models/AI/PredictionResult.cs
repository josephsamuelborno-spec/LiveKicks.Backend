namespace LiveKicks.Backend.Models.AI;

/// <summary>
/// Prediction result (Phase 2C)
/// Matches MAUI client DTO exactly
/// </summary>
public class PredictionResult
{
    public int FixtureId { get; set; }
    public string FixtureDescription { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public string Prediction { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double Reliability { get; set; }
    public double AIScore { get; set; }
    public string Risk { get; set; } = "UNKNOWN";
    public bool ValueBet { get; set; }
    public double ExpectedGoals { get; set; }
    public List<string> TopReasons { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Backward compatibility property for older AI services
    public string RiskLevel => Risk;
}

public class TopPredictionsResponse
{
    public List<PredictionResult> Predictions { get; set; } = new();
    public int TotalFixturesAnalyzed { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool FromCache { get; set; }
}

/// <summary>
/// Ranked prediction with additional metadata
/// </summary>
public class RankedPrediction
{
    public int Rank { get; set; }
    public string LeagueName { get; set; } = string.Empty;
    public string MatchDescription { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public string Prediction { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double Reliability { get; set; }
    public double QualityScore { get; set; }
    public string Risk { get; set; } = string.Empty;
    public List<string> TopReasons { get; set; } = new();
    public int FixtureId { get; set; }
}
