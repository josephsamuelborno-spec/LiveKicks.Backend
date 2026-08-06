using LiveKicks.Backend.Models.AI;

namespace LiveKicks.Backend.Services.AI.Engine;

/// <summary>
/// Assesses prediction risk based on volatility, uncertainty, and external factors
/// </summary>
public class RiskAssessmentService
{
    // Backward compatibility - async wrapper for older AI services
    public Task<RiskAssessment> AssessRiskAsync(
        PredictionFeatures features,
        ConfidenceResult confidence,
        AIContextResponse context)
    {
        return Task.FromResult(AssessRisk(features, confidence, context));
    }

    public RiskAssessment AssessRisk(
        PredictionFeatures features,
        ConfidenceResult confidence,
        AIContextResponse context)
    {
        var riskFactors = new List<RiskFactor>();
        double totalRiskScore = 0.0;

        // Form volatility risk
        var formRisk = AssessFormVolatility(features, context);
        if (formRisk.Score > 0.3)
        {
            riskFactors.Add(formRisk);
            totalRiskScore += formRisk.Score * 0.25;
        }

        // Injury/suspension risk
        var injuryRisk = AssessInjuryRisk(context);
        if (injuryRisk.Score > 0.3)
        {
            riskFactors.Add(injuryRisk);
            totalRiskScore += injuryRisk.Score * 0.20;
        }

        // Head-to-head unpredictability
        var h2hRisk = AssessH2HUnpredictability(features, context);
        if (h2hRisk.Score > 0.3)
        {
            riskFactors.Add(h2hRisk);
            totalRiskScore += h2hRisk.Score * 0.15;
        }

        // League competitiveness risk
        var leagueRisk = AssessLeagueCompetitiveness(context);
        if (leagueRisk.Score > 0.3)
        {
            riskFactors.Add(leagueRisk);
            totalRiskScore += leagueRisk.Score * 0.15;
        }

        // Market disagreement risk
        var marketRisk = AssessMarketDisagreement(features, confidence);
        if (marketRisk.Score > 0.3)
        {
            riskFactors.Add(marketRisk);
            totalRiskScore += marketRisk.Score * 0.15;
        }

        // Data quality risk
        var dataRisk = AssessDataQuality(context);
        if (dataRisk.Score > 0.3)
        {
            riskFactors.Add(dataRisk);
            totalRiskScore += dataRisk.Score * 0.10;
        }

        // Normalize risk score (0-1 scale)
        totalRiskScore = Math.Min(1.0, totalRiskScore);

        var riskLevel = DetermineRiskLevel(totalRiskScore);
        var recommendation = GenerateRecommendation(riskLevel, totalRiskScore, riskFactors);

        return new RiskAssessment
        {
            RiskScore = totalRiskScore,
            RiskLevel = riskLevel,
            RiskFactors = riskFactors,
            Recommendation = recommendation
        };
    }

    private RiskFactor AssessFormVolatility(PredictionFeatures features, AIContextResponse context)
    {
        double volatility = 0.0;
        var reasons = new List<string>();

        // Check home team form consistency
        var homeForm = context.HomeTeam?.Form;
        if (homeForm != null)
        {
            var formVariance = CalculateFormVariance(homeForm.Last5Results ?? new List<string>());
            if (formVariance > 0.6)
            {
                volatility += 0.3;
                reasons.Add($"Home team inconsistent form (variance: {formVariance:F2})");
            }
        }

        // Check away team form consistency
        var awayForm = context.AwayTeam?.Form;
        if (awayForm != null)
        {
            var formVariance = CalculateFormVariance(awayForm.Last5Results ?? new List<string>());
            if (formVariance > 0.6)
            {
                volatility += 0.3;
                reasons.Add($"Away team inconsistent form (variance: {formVariance:F2})");
            }
        }

        // Recent momentum shift
        if (Math.Abs(features.FormScore) < 0.1)
        {
            volatility += 0.2;
            reasons.Add("Closely matched recent form");
        }

        return new RiskFactor
        {
            Name = "Form Volatility",
            Score = Math.Min(1.0, volatility),
            Description = string.Join("; ", reasons)
        };
    }

    private RiskFactor AssessInjuryRisk(AIContextResponse context)
    {
        double risk = 0.0;
        var reasons = new List<string>();

        var homeInjuries = context.HomeTeam?.Injuries ?? 0;
        var awayInjuries = context.AwayTeam?.Injuries ?? 0;

        if (homeInjuries >= 3)
        {
            risk += 0.4;
            reasons.Add($"Home team has {homeInjuries} injuries");
        }
        else if (homeInjuries >= 2)
        {
            risk += 0.2;
            reasons.Add($"Home team has {homeInjuries} injuries");
        }

        if (awayInjuries >= 3)
        {
            risk += 0.4;
            reasons.Add($"Away team has {awayInjuries} injuries");
        }
        else if (awayInjuries >= 2)
        {
            risk += 0.2;
            reasons.Add($"Away team has {awayInjuries} injuries");
        }

        return new RiskFactor
        {
            Name = "Injury/Suspension Risk",
            Score = Math.Min(1.0, risk),
            Description = reasons.Count > 0 ? string.Join("; ", reasons) : "No significant injury concerns"
        };
    }

    private RiskFactor AssessH2HUnpredictability(PredictionFeatures features, AIContextResponse context)
    {
        double unpredictability = 0.0;
        var reasons = new List<string>();

        var h2h = context.HeadToHead;
        if (h2h != null && h2h.TotalMatches >= 3)
        {
            // Check for balanced head-to-head
            double homeWinRate = h2h.TotalMatches > 0 ? (double)h2h.HomeWins / h2h.TotalMatches : 0;
            double awayWinRate = h2h.TotalMatches > 0 ? (double)h2h.AwayWins / h2h.TotalMatches : 0;
            double drawRate = h2h.TotalMatches > 0 ? (double)h2h.Draws / h2h.TotalMatches : 0;

            if (Math.Abs(homeWinRate - awayWinRate) < 0.2)
            {
                unpredictability += 0.5;
                reasons.Add($"Balanced H2H record ({h2h.HomeWins}W-{h2h.Draws}D-{h2h.AwayWins}L)");
            }

            if (drawRate > 0.4)
            {
                unpredictability += 0.3;
                reasons.Add($"High draw rate in H2H ({drawRate:P0})");
            }
        }
        else if (h2h == null || h2h.TotalMatches < 3)
        {
            unpredictability += 0.4;
            reasons.Add("Limited head-to-head history");
        }

        return new RiskFactor
        {
            Name = "H2H Unpredictability",
            Score = Math.Min(1.0, unpredictability),
            Description = reasons.Count > 0 ? string.Join("; ", reasons) : "Predictable H2H pattern"
        };
    }

    private RiskFactor AssessLeagueCompetitiveness(AIContextResponse context)
    {
        double competitiveness = 0.0;
        var reasons = new List<string>();

        var league = context.League;
        if (league != null)
        {
            // High-variance league (close standings)
            if (league.CompetitivenessRating > 0.7)
            {
                competitiveness += 0.4;
                reasons.Add($"Highly competitive league (rating: {league.CompetitivenessRating:F2})");
            }

            // Mid-table clash (positions 6-14)
            var homePos = context.HomeTeam?.Standing ?? 0;
            var awayPos = context.AwayTeam?.Standing ?? 0;
            if (homePos >= 6 && homePos <= 14 && awayPos >= 6 && awayPos <= 14)
            {
                competitiveness += 0.3;
                reasons.Add("Mid-table clash increases unpredictability");
            }
        }

        return new RiskFactor
        {
            Name = "League Competitiveness",
            Score = Math.Min(1.0, competitiveness),
            Description = reasons.Count > 0 ? string.Join("; ", reasons) : "Standard league dynamics"
        };
    }

    private RiskFactor AssessMarketDisagreement(
        PredictionFeatures features,
        ConfidenceResult confidence)
    {
        double disagreement = 0.0;
        var reasons = new List<string>();

        // If our confidence is low but market is very confident (or vice versa)
        if (confidence.ConfidenceScore < 0.5 && features.MarketConfidence > 0.7)
        {
            disagreement += 0.5;
            reasons.Add("Market is confident but model is uncertain");
        }
        else if (confidence.ConfidenceScore > 0.7 && features.MarketConfidence < 0.5)
        {
            disagreement += 0.4;
            reasons.Add("Model confident but market is uncertain");
        }

        // Check for balanced market odds
        if (features.MarketConfidence < 0.6)
        {
            disagreement += 0.3;
            reasons.Add("Market odds indicate uncertainty");
        }

        return new RiskFactor
        {
            Name = "Market Disagreement",
            Score = Math.Min(1.0, disagreement),
            Description = reasons.Count > 0 ? string.Join("; ", reasons) : "Model-market alignment"
        };
    }

    private RiskFactor AssessDataQuality(AIContextResponse context)
    {
        double risk = 0.0;
        var reasons = new List<string>();

        var quality = context.DataQuality;
        if (quality != null)
        {
            if (quality.CompletenessScore < 0.6)
            {
                risk += 0.5;
                reasons.Add($"Low data completeness ({quality.CompletenessScore:P0})");
            }

            if (quality.RecencyScore < 0.5)
            {
                risk += 0.3;
                reasons.Add($"Stale data (recency: {quality.RecencyScore:P0})");
            }

            if (quality.ReliabilityScore < 0.6)
            {
                risk += 0.4;
                reasons.Add($"Low data reliability ({quality.ReliabilityScore:P0})");
            }
        }
        else
        {
            risk = 0.5;
            reasons.Add("Data quality information unavailable");
        }

        return new RiskFactor
        {
            Name = "Data Quality",
            Score = Math.Min(1.0, risk),
            Description = reasons.Count > 0 ? string.Join("; ", reasons) : "Good data quality"
        };
    }

    private double CalculateFormVariance(List<string> results)
    {
        if (results.Count == 0) return 0.5;

        int wins = results.Count(r => r == "W");
        int draws = results.Count(r => r == "D");
        int losses = results.Count(r => r == "L");

        // Perfect consistency = all same result (variance 0)
        // Maximum variance = evenly split results
        double total = results.Count;
        double winRate = wins / total;
        double drawRate = draws / total;
        double lossRate = losses / total;

        // Calculate entropy as variance measure
        double variance = 0;
        if (winRate > 0) variance -= winRate * Math.Log(winRate);
        if (drawRate > 0) variance -= drawRate * Math.Log(drawRate);
        if (lossRate > 0) variance -= lossRate * Math.Log(lossRate);

        return Math.Min(1.0, variance / 1.1); // Normalize
    }

    private string DetermineRiskLevel(double riskScore)
    {
        if (riskScore >= 0.7) return "High";
        if (riskScore >= 0.4) return "Medium";
        return "Low";
    }

    private string GenerateRecommendation(string riskLevel, double riskScore, List<RiskFactor> factors)
    {
        if (riskLevel == "High")
        {
            return $"?? High risk ({riskScore:P0}). Avoid betting or reduce stake significantly. Key concerns: {string.Join(", ", factors.Take(2).Select(f => f.Name))}";
        }
        if (riskLevel == "Medium")
        {
            return $"? Medium risk ({riskScore:P0}). Consider reduced stake or safer markets. Monitor: {string.Join(", ", factors.Take(2).Select(f => f.Name))}";
        }
        return $"? Low risk ({riskScore:P0}). Standard stake recommended.";
    }
}

public class RiskAssessment
{
    public double RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<RiskFactor> RiskFactors { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
}

public class RiskFactor
{
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Description { get; set; } = string.Empty;
}
