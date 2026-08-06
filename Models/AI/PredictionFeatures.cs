namespace LiveKicks.Backend.Models.AI;

/// <summary>
/// Feature vector for prediction engine
/// </summary>
public class PredictionFeatures
{
    // Fixture Info
    public int FixtureId { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }

    // Form Scores
    public double HomeFormScore { get; set; }
    public double AwayFormScore { get; set; }
    public double FormScore { get; set; }

    // Attack Strength
    public double HomeAttackStrength { get; set; }
    public double AwayAttackStrength { get; set; }

    // Defense Strength
    public double HomeDefenseStrength { get; set; }
    public double AwayDefenseStrength { get; set; }

    // Home/Away Specific
    public double HomeAdvantageScore { get; set; }
    public double AwayWeaknessScore { get; set; }

    // Head-to-Head
    public double H2HHomeAdvantage { get; set; }
    public double H2HGoalTrend { get; set; }

    // League Context
    public double LeagueGoalAverage { get; set; }
    public double LeagueUnpredictability { get; set; }
    public double LeagueHomeAdvantage { get; set; }

    // Market Intelligence
    public double MarketConfidence { get; set; }
    public double ImpliedHomeProbability { get; set; }
    public double ImpliedDrawProbability { get; set; }
    public double ImpliedAwayProbability { get; set; }
    public bool MarketSupportsHome { get; set; }
    public bool MarketSupportsOver25 { get; set; }

    // Data Quality
    public double DataQualityScore { get; set; }
}

/// <summary>
/// Confidence calculation result
/// </summary>
public class ConfidenceResult
{
    public double Confidence { get; set; }
    public double Reliability { get; set; }
    public Dictionary<string, double> FactorContributions { get; set; } = new();
}
