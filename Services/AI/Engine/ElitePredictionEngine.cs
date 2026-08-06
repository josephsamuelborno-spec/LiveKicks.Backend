using LiveKicks.Backend.Models.AI;

namespace LiveKicks.Backend.Services.AI.Engine;

/// <summary>
/// Elite Prediction Engine - Phase 2C
/// Main orchestrator for generating AI predictions
/// Produces predictions for all major markets
/// </summary>
public class ElitePredictionEngine
{
    private readonly PredictionFeatureBuilder _featureBuilder;
    private readonly EliteConfidenceCalculator _confidenceCalculator;
    private readonly RiskAssessmentService _riskAssessment;
    private readonly ILogger<ElitePredictionEngine> _logger;

    public ElitePredictionEngine(
        PredictionFeatureBuilder featureBuilder,
        EliteConfidenceCalculator confidenceCalculator,
        RiskAssessmentService riskAssessment,
        ILogger<ElitePredictionEngine> logger)
    {
        _featureBuilder = featureBuilder;
        _confidenceCalculator = confidenceCalculator;
        _riskAssessment = riskAssessment;
        _logger = logger;
    }

    /// <summary>
    /// Generate all predictions for a fixture
    /// </summary>
    public async Task<List<PredictionResult>> GeneratePredictionsAsync(
        AIContextResponse context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("?? Generating predictions for {Home} vs {Away}",
            context.Fixture.HomeTeamName, context.Fixture.AwayTeamName);

        var predictions = new List<PredictionResult>();

        try
        {
            // Extract features
            var features = _featureBuilder.BuildFeatures(context);

            // Generate predictions for each market
            predictions.Add(await GenerateMatchWinnerPrediction(context, features, cancellationToken));
            predictions.Add(await GenerateOverUnderPrediction(context, features, cancellationToken));
            predictions.Add(await GenerateBTTSPrediction(context, features, cancellationToken));
            predictions.Add(await GenerateDoubleChancePrediction(context, features, cancellationToken));
            predictions.Add(await GenerateCleanSheetPrediction(context, features, cancellationToken));

            _logger.LogInformation("? Generated {Count} predictions", predictions.Count);

            return predictions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error generating predictions for fixture {FixtureId}", context.Fixture.FixtureId);
            return predictions;
        }
    }

    #region Match Winner Prediction

    private async Task<PredictionResult> GenerateMatchWinnerPrediction(
        AIContextResponse context,
        PredictionFeatures features,
        CancellationToken cancellationToken)
    {
        var prediction = new PredictionResult
        {
            FixtureId = context.Fixture.FixtureId,
            FixtureDescription = $"{context.Fixture.HomeTeamName} vs {context.Fixture.AwayTeamName}",
            Market = "Match Winner"
        };

        // Calculate scores for each outcome
        double homeScore = CalculateHomeWinScore(features);
        double drawScore = CalculateDrawScore(features);
        double awayScore = CalculateAwayWinScore(features);

        // Determine prediction
        if (homeScore > drawScore && homeScore > awayScore)
        {
            prediction.Prediction = "HOME";
            prediction.AIScore = homeScore;
        }
        else if (awayScore > homeScore && awayScore > drawScore)
        {
            prediction.Prediction = "AWAY";
            prediction.AIScore = awayScore;
        }
        else
        {
            prediction.Prediction = "DRAW";
            prediction.AIScore = drawScore;
        }

        // Calculate confidence
        var factorScores = new Dictionary<string, double>
        {
            ["Form"] = prediction.Prediction == "HOME" ? features.HomeFormScore : features.AwayFormScore,
            ["Attack"] = prediction.Prediction == "HOME" ? features.HomeAttackStrength : features.AwayAttackStrength,
            ["Defense"] = prediction.Prediction == "HOME" ? features.HomeDefenseStrength : features.AwayDefenseStrength,
            ["HomeAdvantage"] = prediction.Prediction == "HOME" ? features.HomeAdvantageScore : features.AwayWeaknessScore,
            ["H2H"] = features.H2HHomeAdvantage,
            ["Market"] = features.MarketConfidence
        };

        var confidenceResult = _confidenceCalculator.CalculateConfidence(
            factorScores,
            features.DataQualityScore);

        prediction.Confidence = confidenceResult.Confidence;
        prediction.Reliability = confidenceResult.Reliability;

        // Calculate risk
        prediction.Risk = await _riskAssessment.AssessRiskAsync(features, prediction.Confidence, confidenceResult.Reliability);

        // Generate reasons
        prediction.TopReasons = GenerateMatchWinnerReasons(features, prediction.Prediction);

        return prediction;
    }

    private double CalculateHomeWinScore(PredictionFeatures features)
    {
        double score = 0;
        score += features.HomeFormScore * 0.25;
        score += features.HomeAttackStrength * 0.20;
        score += features.AwayDefenseStrength * 0.15;
        score += features.HomeAdvantageScore * 0.20;
        score += features.H2HHomeAdvantage * 0.10;
        score += features.MarketConfidence * 0.10;
        return Math.Round(score, 2);
    }

    private double CalculateAwayWinScore(PredictionFeatures features)
    {
        double score = 0;
        score += features.AwayFormScore * 0.25;
        score += features.AwayAttackStrength * 0.20;
        score += features.HomeDefenseStrength * 0.15;
        score += features.AwayWeaknessScore * 0.15;
        score += (100 - features.H2HHomeAdvantage) * 0.15;
        score += features.MarketConfidence * 0.10;
        return Math.Round(score, 2);
    }

    private double CalculateDrawScore(PredictionFeatures features)
    {
        double score = 50; // Base
        double formDiff = Math.Abs(features.HomeFormScore - features.AwayFormScore);
        score -= formDiff * 0.3;
        score += features.LeagueUnpredictability * 0.2;
        return Math.Max(Math.Round(score, 2), 0);
    }

    private List<string> GenerateMatchWinnerReasons(PredictionFeatures features, string prediction)
    {
        var reasons = new List<string>();

        if (prediction == "HOME")
        {
            if (features.HomeFormScore >= 70) reasons.Add("Strong home form");
            if (features.HomeAdvantageScore >= 70) reasons.Add("Excellent home record");
            if (features.AwayDefenseStrength < 50) reasons.Add("Weak away defense");
        }
        else if (prediction == "AWAY")
        {
            if (features.AwayFormScore >= 70) reasons.Add("Strong away form");
            if (features.AwayAttackStrength >= 70) reasons.Add("Powerful attack");
            if (features.HomeDefenseStrength < 50) reasons.Add("Vulnerable home defense");
        }

        return reasons.Take(3).ToList();
    }

    #endregion

    #region Over/Under 2.5 Prediction

    private async Task<PredictionResult> GenerateOverUnderPrediction(
        AIContextResponse context,
        PredictionFeatures features,
        CancellationToken cancellationToken)
    {
        var prediction = new PredictionResult
        {
            FixtureId = context.Fixture.FixtureId,
            FixtureDescription = $"{context.Fixture.HomeTeamName} vs {context.Fixture.AwayTeamName}",
            Market = "Over/Under 2.5"
        };

        // Calculate expected goals
        double homeExpectedGoals = (features.HomeAttackStrength + (100 - features.AwayDefenseStrength)) / 100 * features.LeagueGoalAverage;
        double awayExpectedGoals = (features.AwayAttackStrength + (100 - features.HomeDefenseStrength)) / 100 * features.LeagueGoalAverage;
        double totalExpectedGoals = homeExpectedGoals + awayExpectedGoals;

        prediction.ExpectedGoals = Math.Round(totalExpectedGoals, 2);

        // Determine prediction
        if (totalExpectedGoals > 2.5)
        {
            prediction.Prediction = "OVER 2.5";
            prediction.AIScore = Math.Min((totalExpectedGoals / 4.0) * 100, 100);
        }
        else
        {
            prediction.Prediction = "UNDER 2.5";
            prediction.AIScore = Math.Min(((4.0 - totalExpectedGoals) / 4.0) * 100, 100);
        }

        // Calculate confidence
        var factorScores = new Dictionary<string, double>
        {
            ["HomeAttack"] = features.HomeAttackStrength,
            ["AwayAttack"] = features.AwayAttackStrength,
            ["HomeDefense"] = 100 - features.HomeDefenseStrength,
            ["AwayDefense"] = 100 - features.AwayDefenseStrength,
            ["H2HGoalTrend"] = features.H2HGoalTrend,
            ["LeagueGoals"] = (features.LeagueGoalAverage / 3.0) * 100
        };

        var confidenceResult = _confidenceCalculator.CalculateConfidence(
            factorScores,
            features.DataQualityScore);

        prediction.Confidence = confidenceResult.Confidence;
        prediction.Reliability = confidenceResult.Reliability;
        prediction.Risk = await _riskAssessment.AssessRiskAsync(features, prediction.Confidence, confidenceResult.Reliability);

        prediction.TopReasons = GenerateOverUnderReasons(features, prediction.Prediction, totalExpectedGoals);

        return prediction;
    }

    private List<string> GenerateOverUnderReasons(PredictionFeatures features, string prediction, double expectedGoals)
    {
        var reasons = new List<string>();

        if (prediction.StartsWith("OVER"))
        {
            reasons.Add($"Expected goals: {expectedGoals:F1}");
            if (features.HomeAttackStrength >= 70 || features.AwayAttackStrength >= 70)
                reasons.Add("Strong attacking teams");
            if (features.H2HGoalTrend >= 60)
                reasons.Add("High-scoring H2H history");
        }
        else
        {
            reasons.Add($"Expected goals: {expectedGoals:F1}");
            if (features.HomeDefenseStrength >= 70 || features.AwayDefenseStrength >= 70)
                reasons.Add("Strong defensive records");
        }

        return reasons.Take(3).ToList();
    }

    #endregion

    #region BTTS Prediction

    private async Task<PredictionResult> GenerateBTTSPrediction(
        AIContextResponse context,
        PredictionFeatures features,
        CancellationToken cancellationToken)
    {
        var prediction = new PredictionResult
        {
            FixtureId = context.Fixture.FixtureId,
            FixtureDescription = $"{context.Fixture.HomeTeamName} vs {context.Fixture.AwayTeamName}",
            Market = "Both Teams to Score"
        };

        // Calculate BTTS probability
        double homeAttackVsAwayDefense = (features.HomeAttackStrength + (100 - features.AwayDefenseStrength)) / 2;
        double awayAttackVsHomeDefense = (features.AwayAttackStrength + (100 - features.HomeDefenseStrength)) / 2;

        double bttsScore = (homeAttackVsAwayDefense + awayAttackVsHomeDefense) / 2;

        if (bttsScore >= 60)
        {
            prediction.Prediction = "YES";
            prediction.AIScore = bttsScore;
        }
        else
        {
            prediction.Prediction = "NO";
            prediction.AIScore = 100 - bttsScore;
        }

        var factorScores = new Dictionary<string, double>
        {
            ["HomeAttack"] = features.HomeAttackStrength,
            ["AwayAttack"] = features.AwayAttackStrength,
            ["HomeDefense"] = 100 - features.HomeDefenseStrength,
            ["AwayDefense"] = 100 - features.AwayDefenseStrength
        };

        var confidenceResult = _confidenceCalculator.CalculateConfidence(
            factorScores,
            features.DataQualityScore);

        prediction.Confidence = confidenceResult.Confidence;
        prediction.Reliability = confidenceResult.Reliability;
        prediction.Risk = await _riskAssessment.AssessRiskAsync(features, prediction.Confidence, confidenceResult.Reliability);

        prediction.TopReasons = new List<string>
        {
            prediction.Prediction == "YES" ? "Both teams have attacking capability" : "Strong defensive records",
            $"Home attack: {features.HomeAttackStrength:F0}/100",
            $"Away attack: {features.AwayAttackStrength:F0}/100"
        };

        return prediction;
    }

    #endregion

    #region Double Chance & Clean Sheet Predictions

    private async Task<PredictionResult> GenerateDoubleChancePrediction(
        AIContextResponse context,
        PredictionFeatures features,
        CancellationToken cancellationToken)
    {
        var prediction = new PredictionResult
        {
            FixtureId = context.Fixture.FixtureId,
            FixtureDescription = $"{context.Fixture.HomeTeamName} vs {context.Fixture.AwayTeamName}",
            Market = "Double Chance"
        };

        // Calculate scores
        double homeScore = CalculateHomeWinScore(features);
        double awayScore = CalculateAwayWinScore(features);

        if (homeScore > awayScore)
        {
            prediction.Prediction = "HOME or DRAW";
            prediction.AIScore = Math.Max(homeScore, 60);
        }
        else
        {
            prediction.Prediction = "AWAY or DRAW";
            prediction.AIScore = Math.Max(awayScore, 60);
        }

        var factorScores = new Dictionary<string, double>
        {
            ["Form"] = prediction.Prediction.StartsWith("HOME") ? features.HomeFormScore : features.AwayFormScore,
            ["Attack"] = prediction.Prediction.StartsWith("HOME") ? features.HomeAttackStrength : features.AwayAttackStrength
        };

        var confidenceResult = _confidenceCalculator.CalculateConfidence(
            factorScores,
            features.DataQualityScore);

        prediction.Confidence = confidenceResult.Confidence;
        prediction.Reliability = confidenceResult.Reliability;
        prediction.Risk = await _riskAssessment.AssessRiskAsync(features, prediction.Confidence, confidenceResult.Reliability);

        prediction.TopReasons = new List<string> { "Safer betting option", "Covers two outcomes" };

        return prediction;
    }

    private async Task<PredictionResult> GenerateCleanSheetPrediction(
        AIContextResponse context,
        PredictionFeatures features,
        CancellationToken cancellationToken)
    {
        var prediction = new PredictionResult
        {
            FixtureId = context.Fixture.FixtureId,
            FixtureDescription = $"{context.Fixture.HomeTeamName} vs {context.Fixture.AwayTeamName}",
            Market = "Clean Sheet"
        };

        double homeCleanSheetProb = (features.HomeDefenseStrength + (100 - features.AwayAttackStrength)) / 2;
        double awayCleanSheetProb = (features.AwayDefenseStrength + (100 - features.HomeAttackStrength)) / 2;

        if (homeCleanSheetProb > awayCleanSheetProb && homeCleanSheetProb >= 60)
        {
            prediction.Prediction = "HOME Clean Sheet";
            prediction.AIScore = homeCleanSheetProb;
        }
        else if (awayCleanSheetProb >= 60)
        {
            prediction.Prediction = "AWAY Clean Sheet";
            prediction.AIScore = awayCleanSheetProb;
        }
        else
        {
            prediction.Prediction = "NO Clean Sheet";
            prediction.AIScore = 50;
        }

        var factorScores = new Dictionary<string, double>
        {
            ["Defense"] = prediction.Prediction.StartsWith("HOME") ? features.HomeDefenseStrength : features.AwayDefenseStrength,
            ["OpponentAttack"] = prediction.Prediction.StartsWith("HOME") ? 100 - features.AwayAttackStrength : 100 - features.HomeAttackStrength
        };

        var confidenceResult = _confidenceCalculator.CalculateConfidence(
            factorScores,
            features.DataQualityScore);

        prediction.Confidence = confidenceResult.Confidence;
        prediction.Reliability = confidenceResult.Reliability;
        prediction.Risk = await _riskAssessment.AssessRiskAsync(features, prediction.Confidence, confidenceResult.Reliability);

        prediction.TopReasons = new List<string>
        {
            $"{(prediction.Prediction.StartsWith("HOME") ? "Home" : "Away")} defensive strength",
            "Weak opponent attack"
        };

        return prediction;
    }

    #endregion
}
