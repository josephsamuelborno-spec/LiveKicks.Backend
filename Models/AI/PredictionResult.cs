namespace LiveKicks.Backend.Models.AI;

/// <summary>
/// Prediction result (Phase 2C)
/// Matches MAUI client DTO exactly
/// </summary>
public class PredictionResult
{
    public string Prediction { get; set; } = string.Empty;
public int FixtureId { get; set; }

    public double Probability { get; set; }

    public Dictionary<string, object> RankingFactors { get; set; } = new();

    public string FixtureDescription { get; set; } = string.Empty;

    public string Market { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public double Reliability { get; set; }

    public double AIScore { get; set; }

    // All possible outcomes with percentages
    public Dictionary<string, double> Probabilities { get; set; } = new();

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

    public int FixtureId { get; set; }

    public string League { get; set; } = string.Empty;

    public string LeagueName
    {
        get => League;
        set => League = value;
    }

    public string HomeTeam { get; set; } = string.Empty;

    public string AwayTeam { get; set; } = string.Empty;

    public DateTime KickoffTime { get; set; }


    // Main AI prediction object
    public PredictionResult Prediction { get; set; } = new();


    // Compatibility alias
    public PredictionResult PredictionResult
    {
        get => Prediction;
        set => Prediction = value;
    }


    // Writable compatibility properties
    public string Market
    {
        get => Prediction.Market;
        set => Prediction.Market = value;
    }


    public double Confidence
    {
        get => Prediction.Confidence;
        set => Prediction.Confidence = value;
    }


    public double Reliability
    {
        get => Prediction.Reliability;
        set => Prediction.Reliability = value;
    }


    public double Probability
    {
        get => Prediction.Probability;
        set => Prediction.Probability = value;
    }


    public double QualityScore { get; set; }


    public string Risk
    {
        get => Prediction.Risk;
        set => Prediction.Risk = value;
    }


    public List<string> TopReasons
    {
        get => Prediction.TopReasons;
        set => Prediction.TopReasons = value;
    }


    public Dictionary<string, object> RankingFactors
    {
        get => Prediction.RankingFactors;
        set => Prediction.RankingFactors = value;
    }


    public string MatchDescription { get; set; } = string.Empty;


    public string PredictionText
    {
        get => Prediction.Prediction;
        set => Prediction.Prediction = value;
    }
}
