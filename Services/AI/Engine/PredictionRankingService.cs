using LiveKicks.Backend.Models.AI;

namespace LiveKicks.Backend.Services.AI.Engine;

/// <summary>
/// Ranks and filters predictions based on quality, confidence, and risk
/// </summary>
public class PredictionRankingService
{
    private readonly RiskAssessmentService _riskService;

    public PredictionRankingService(RiskAssessmentService riskService)
    {
        _riskService = riskService;
    }

    /// <summary>
    /// Rank predictions across multiple fixtures and return top recommendations
    /// </summary>
    public List<RankedPrediction> RankPredictions(
        List<(AIContextResponse Context, List<PredictionResult> Predictions)> allPredictions,
        int maxResults = 10)
    {
        var ranked = new List<RankedPrediction>();

        foreach (var (context, predictions) in allPredictions)
        {
            foreach (var prediction in predictions)
            {
                // Skip low-confidence predictions
                if (prediction.Confidence < 0.60)
                    continue;

                // Calculate quality score
                var qualityScore = CalculateQualityScore(prediction, context);

                // Skip poor quality predictions
                if (qualityScore < 0.50)
                    continue;

                ranked.Add(new RankedPrediction
                {
                    FixtureId = context.Fixture.FixtureId,
                    HomeTeam = context.Fixture.HomeTeam,
                    AwayTeam = context.Fixture.AwayTeam,
                    KickoffTime = context.Fixture.KickoffTime,
                    League = context.League?.Name ?? "Unknown",
                    Prediction = prediction,
                    QualityScore = qualityScore,
                    RankingFactors = BuildRankingFactors(prediction, context, qualityScore)
                });
            }
        }

        // Sort by quality score descending
        var topPredictions = ranked
            .OrderByDescending(p => p.QualityScore)
            .Take(maxResults)
            .ToList();

        // Assign ranking positions
        for (int i = 0; i < topPredictions.Count; i++)
        {
            topPredictions[i].Rank = i + 1;
        }

        return topPredictions;
    }

    /// <summary>
    /// Calculate overall quality score for a prediction
    /// </summary>
    private double CalculateQualityScore(PredictionResult prediction, AIContextResponse context)
    {
        double score = 0.0;

        // Base confidence weight (40%)
        score += prediction.Confidence * 0.40;

        // Reliability weight (20%)
        score += prediction.Reliability * 0.20;

        // Data quality weight (15%)
        var dataQuality = context.DataQuality?.OverallQuality ?? 0.5;
        score += dataQuality * 0.15;

        // Market alignment weight (10%)
        var marketAlignment = CalculateMarketAlignment(prediction, context);
        score += marketAlignment * 0.10;

        // League quality weight (10%)
        var leagueQuality = CalculateLeagueQuality(context);
        score += leagueQuality * 0.10;

        // Recency boost (5%)
        var recencyBoost = CalculateRecencyBoost(context);
        score += recencyBoost * 0.05;

        // Penalty for high risk
        if (prediction.RiskLevel == "High")
            score *= 0.7; // 30% penalty
        else if (prediction.RiskLevel == "Medium")
            score *= 0.85; // 15% penalty

        return Math.Min(1.0, score);
    }

    private double CalculateMarketAlignment(PredictionResult prediction, AIContextResponse context)
    {
        // Check if market odds support our prediction
        var marketOdds = context.MarketOdds;
        if (marketOdds == null) return 0.5;

        // For match winner predictions
        if (prediction.Market == "Match Winner")
        {
            if (prediction.Prediction == "Home Win" && marketOdds.HomeWin > 0)
            {
                var impliedProb = 1.0 / marketOdds.HomeWin;
                return Math.Min(1.0, impliedProb * 1.2); // Boost if market agrees
            }
            if (prediction.Prediction == "Away Win" && marketOdds.AwayWin > 0)
            {
                var impliedProb = 1.0 / marketOdds.AwayWin;
                return Math.Min(1.0, impliedProb * 1.2);
            }
            if (prediction.Prediction == "Draw" && marketOdds.Draw > 0)
            {
                var impliedProb = 1.0 / marketOdds.Draw;
                return Math.Min(1.0, impliedProb * 1.2);
            }
        }

        // For Over/Under 2.5
        if (prediction.Market == "Over/Under 2.5 Goals")
        {
            if (prediction.Prediction == "Over 2.5" && marketOdds.Over25 > 0)
            {
                var impliedProb = 1.0 / marketOdds.Over25;
                return Math.Min(1.0, impliedProb * 1.2);
            }
            if (prediction.Prediction == "Under 2.5" && marketOdds.Under25 > 0)
            {
                var impliedProb = 1.0 / marketOdds.Under25;
                return Math.Min(1.0, impliedProb * 1.2);
            }
        }

        // For BTTS
        if (prediction.Market == "Both Teams To Score")
        {
            if (prediction.Prediction == "Yes" && marketOdds.BTTSYesOdds > 0)
            {
                var impliedProb = 1.0 / marketOdds.BTTSYesOdds;
                return Math.Min(1.0, impliedProb * 1.2);
            }
            if (prediction.Prediction == "No" && marketOdds.BTTSYesOdds > 0)
            {
                // Note: Using BTTSYesOdds for "No" as well since model doesn't have BTTS_No
                var impliedProb = 1.0 / marketOdds.BTTSYesOdds;
                return Math.Min(1.0, impliedProb * 1.2);
            }
        }

        return 0.6; // Neutral if no clear market data
    }

    private double CalculateLeagueQuality(AIContextResponse context)
    {
        var league = context.LeagueProfile;
        if (league == null) return 0.5;

        // Prioritize top leagues
        var topLeagues = new[] { "Premier League", "La Liga", "Serie A", "Bundesliga", "Ligue 1", "Champions League", "Europa League" };
        if (topLeagues.Any(l => league.Name.Contains(l, StringComparison.OrdinalIgnoreCase)))
            return 0.9;

        // Use data coverage as proxy for league quality
        if (league.DataCoverageQuality > 0.7)
            return 0.8;
        if (league.DataCoverageQuality > 0.5)
            return 0.6;

        return 0.5;
    }

    private double CalculateRecencyBoost(AIContextResponse context)
    {
        var kickoff = context.Fixture.Date;
        var now = DateTime.UtcNow;
        var hoursUntilKickoff = (kickoff - now).TotalHours;

        // Boost predictions for matches in next 2-6 hours
        if (hoursUntilKickoff >= 2 && hoursUntilKickoff <= 6)
            return 1.0;

        // Moderate boost for matches today
        if (hoursUntilKickoff >= 0 && hoursUntilKickoff <= 24)
            return 0.8;

        // Lower for matches further out
        if (hoursUntilKickoff > 24 && hoursUntilKickoff <= 48)
            return 0.6;

        return 0.4;
    }

    private Dictionary<string, object> BuildRankingFactors(
        PredictionResult prediction,
        AIContextResponse context,
        double qualityScore)
    {
        return new Dictionary<string, object>
        {
            ["quality_score"] = Math.Round(qualityScore, 3),
            ["confidence"] = Math.Round(prediction.Confidence, 3),
            ["reliability"] = Math.Round(prediction.Reliability, 3),
            ["risk"] = prediction.Risk,
            ["data_quality"] = Math.Round(context.DataQuality?.OverallScore ?? 0.5, 3),
            ["league"] = context.LeagueProfile?.Name ?? "Unknown",
            ["hours_until_kickoff"] = Math.Round((context.Fixture.Date - DateTime.UtcNow).TotalHours, 1)
        };
    }
}

public class RankedPrediction
{
    public int Rank { get; set; }
    public int FixtureId { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime KickoffTime { get; set; }
    public string League { get; set; } = string.Empty;
    public PredictionResult Prediction { get; set; } = null!;
    public double QualityScore { get; set; }
    public Dictionary<string, object> RankingFactors { get; set; } = new();
}
