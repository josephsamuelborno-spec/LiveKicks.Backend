using LiveKicks.Backend.Models.AI;

namespace LiveKicks.Backend.Services.AI.Engine;

/// <summary>
/// Elite Confidence Calculator - Phase 2C
/// Implements STRICT confidence rules migrated from MAUI
/// Never allows overconfidence
/// </summary>
public class EliteConfidenceCalculator
{
    private readonly ILogger<EliteConfidenceCalculator> _logger;

    public EliteConfidenceCalculator(ILogger<EliteConfidenceCalculator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate confidence with strict rules
    /// </summary>
    public ConfidenceResult CalculateConfidence(
        Dictionary<string, double> factorScores,
        double dataQualityScore)
    {
        if (factorScores == null || !factorScores.Any())
        {
            return new ConfidenceResult
            {
                Confidence = 50,
                Reliability = 0,
                Quality = "Insufficient Data",
                FactorsConsidered = new List<string> { "No factors available" }
            };
        }

        // Calculate weighted confidence
        double totalConfidence = 0;
        double totalWeight = 0;
        var factorsConsidered = new List<string>();

        foreach (var factor in factorScores)
        {
            double weight = GetFactorWeight(factor.Key);
            if (weight > 0 && factor.Value > 0)
            {
                totalConfidence += factor.Value * weight;
                totalWeight += weight;
                factorsConsidered.Add($"{factor.Key}: {factor.Value:F1}/100 (weight: {weight:F2})");
            }
        }

        if (totalWeight == 0)
        {
            return new ConfidenceResult
            {
                Confidence = 50,
                Reliability = 0,
                Quality = "No Valid Factors",
                FactorsConsidered = factorsConsidered
            };
        }

        // Base confidence (weighted average)
        double baseConfidence = totalConfidence / totalWeight;

        // Apply data quality multiplier
        double dataQualityMultiplier = dataQualityScore / 100.0;
        double confidence = baseConfidence * dataQualityMultiplier;

        // Count how many factors agree (score >= 70)
        int strongFactors = factorScores.Count(f => f.Value >= 70);
        int totalFactors = factorScores.Count;
        double agreementRate = (double)strongFactors / totalFactors;

        // STRICT RULES ENFORCEMENT
        confidence = EnforceStrictRules(confidence, agreementRate, strongFactors, totalFactors, dataQualityScore);

        // Calculate reliability
        double reliability = CalculateReliability(dataQualityScore, totalFactors, agreementRate);

        // Determine quality
        string quality = DetermineConfidenceQuality(confidence, agreementRate, dataQualityScore);

        _logger.LogDebug("?? Confidence: {Confidence:F1}% ({Quality}), Reliability: {Reliability:F1}%",
            confidence, quality, reliability);

        return new ConfidenceResult
        {
            Confidence = Math.Round(confidence, 2),
            Reliability = Math.Round(reliability, 2),
            Quality = quality,
            FactorsConsidered = factorsConsidered,
            AgreementRate = agreementRate,
            StrongFactors = strongFactors
        };
    }

    /// <summary>
    /// Enforce STRICT confidence rules (migrated from MAUI)
    /// </summary>
    private double EnforceStrictRules(
        double baseConfidence,
        double agreementRate,
        int strongFactors,
        int totalFactors,
        double dataQuality)
    {
        double confidence = baseConfidence;

        // RULE 1: 95%+ requires almost perfect agreement
        if (confidence >= 95)
        {
            if (agreementRate < 0.9 || strongFactors < 6 || dataQuality < 90)
            {
                confidence = 90;
                _logger.LogDebug("  ?? Reduced from 95%+ to 90% (insufficient perfect agreement)");
            }
        }

        // RULE 2: 90%+ requires strong evidence
        if (confidence >= 90)
        {
            if (agreementRate < 0.75 || strongFactors < 5 || dataQuality < 80)
            {
                confidence = 85;
                _logger.LogDebug("  ?? Reduced from 90%+ to 85% (moderate evidence only)");
            }
        }

        // RULE 3: 85%+ requires good evidence
        if (confidence >= 85)
        {
            if (agreementRate < 0.65 || strongFactors < 4 || dataQuality < 70)
            {
                confidence = 80;
                _logger.LogDebug("  ?? Reduced from 85%+ to 80% (limited strong factors)");
            }
        }

        // RULE 4: 80%+ requires reasonable evidence
        if (confidence >= 80)
        {
            if (agreementRate < 0.50 || strongFactors < 3 || dataQuality < 60)
            {
                confidence = 75;
                _logger.LogDebug("  ?? Reduced from 80%+ to 75% (weak evidence)");
            }
        }

        // RULE 5: Below 65% = Don't recommend
        if (confidence < 65)
        {
            _logger.LogDebug("  ? Below recommendation threshold (65%)");
        }

        // RULE 6: Cap at 96% (never 99%+)
        if (confidence > 96)
        {
            confidence = 96;
            _logger.LogDebug("  ?? Capped at 96% (no prediction should be 99%+)");
        }

        return confidence;
    }

    /// <summary>
    /// Calculate prediction reliability score
    /// Separate from confidence - measures data trustworthiness
    /// </summary>
    private double CalculateReliability(double dataQuality, int factorsAvailable, double agreementRate)
    {
        double reliability = 0;

        // Data quality (50%)
        reliability += dataQuality * 0.5;

        // Factor availability (25%)
        double factorScore = Math.Min(factorsAvailable / 10.0, 1.0) * 25;
        reliability += factorScore;

        // Agreement rate (25%)
        reliability += agreementRate * 25;

        return Math.Min(reliability, 100);
    }

    /// <summary>
    /// Determine confidence quality tier
    /// </summary>
    private string DetermineConfidenceQuality(double confidence, double agreementRate, double dataQuality)
    {
        if (confidence >= 90 && agreementRate >= 0.8 && dataQuality >= 85)
            return "Excellent";

        if (confidence >= 80 && agreementRate >= 0.65 && dataQuality >= 75)
            return "Strong";

        if (confidence >= 70 && agreementRate >= 0.50 && dataQuality >= 65)
            return "Good";

        if (confidence >= 65 && dataQuality >= 50)
            return "Fair";

        return "Weak";
    }

    /// <summary>
    /// Get weight for each factor type
    /// </summary>
    private double GetFactorWeight(string factorName)
    {
        return factorName.ToLower() switch
        {
            "form" => 1.0,
            "attack" => 0.9,
            "defense" => 0.9,
            "homeadvantage" => 0.8,
            "h2h" => 0.7,
            "market" => 0.6,
            "league" => 0.5,
            "homeattack" => 0.8,
            "awayattack" => 0.8,
            "homedefense" => 0.8,
            "awaydefense" => 0.8,
            "h2hgoaltrend" => 0.7,
            "leaguegoals" => 0.5,
            "opponentattack" => 0.8,
            _ => 0.5
        };
    }
}

/// <summary>
/// Confidence calculation result
/// </summary>
public class ConfidenceResult
{
    public double Confidence { get; set; }
    public double Reliability { get; set; }
    public string Quality { get; set; } = "Unknown";
    public List<string> FactorsConsidered { get; set; } = new();
    public double AgreementRate { get; set; }
    public int StrongFactors { get; set; }
}
