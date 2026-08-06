using LiveKicks.Backend.Models.AI;

namespace LiveKicks.Backend.Services.AI.Engine;

/// <summary>
/// Prediction Feature Builder - Phase 2C
/// Extracts prediction features from AIContextResponse
/// Converts raw data into normalized feature scores (0-100)
/// </summary>
public class PredictionFeatureBuilder
{
    private readonly ILogger<PredictionFeatureBuilder> _logger;

    public PredictionFeatureBuilder(ILogger<PredictionFeatureBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Build complete feature set from AI context
    /// </summary>
    public PredictionFeatures BuildFeatures(AIContextResponse context)
    {
        _logger.LogDebug("?? Building prediction features...");

        var features = new PredictionFeatures
        {
            FixtureId = context.Fixture.FixtureId,
            HomeTeamId = context.Fixture.HomeTeamId,
            AwayTeamId = context.Fixture.AwayTeamId
        };

        // Team Form Features
        features.HomeFormScore = CalculateFormScore(context.HomeTeam);
        features.AwayFormScore = CalculateFormScore(context.AwayTeam);

        // Attack Features
        features.HomeAttackStrength = CalculateAttackStrength(context.HomeTeam);
        features.AwayAttackStrength = CalculateAttackStrength(context.AwayTeam);

        // Defense Features
        features.HomeDefenseStrength = CalculateDefenseStrength(context.HomeTeam);
        features.AwayDefenseStrength = CalculateDefenseStrength(context.AwayTeam);

        // Home Advantage
        features.HomeAdvantageScore = CalculateHomeAdvantage(context.HomeTeam, context.LeagueProfile);
        features.AwayWeaknessScore = CalculateAwayWeakness(context.AwayTeam);

        // Head-to-Head Features
        features.H2HHomeAdvantage = CalculateH2HAdvantage(context.HeadToHead, isHome: true);
        features.H2HGoalTrend = CalculateH2HGoalTrend(context.HeadToHead);

        // Market Features
        features.MarketConfidence = CalculateMarketConfidence(context.MarketOdds);
        features.ImpliedHomeProbability = OddsToProbability(context.MarketOdds.HomeWinOdds);
        features.ImpliedDrawProbability = OddsToProbability(context.MarketOdds.DrawOdds);
        features.ImpliedAwayProbability = OddsToProbability(context.MarketOdds.AwayWinOdds);

        // League Intelligence
        features.LeagueGoalAverage = context.LeagueProfile.AvgGoalsPerMatch;
        features.LeagueUnpredictability = context.LeagueProfile.UnpredictabilityFactor;

        // Data Quality
        features.DataQualityScore = context.DataQuality.OverallScore;

        _logger.LogDebug($"  ? Features built: Home Form {features.HomeFormScore:F1}, Away Form {features.AwayFormScore:F1}");

        return features;
    }

    #region Form Scoring

    private double CalculateFormScore(TeamAIProfile team)
    {
        if (team.Last10Matches.MatchCount == 0) return 50;

        double score = 0;

        // Points per game (50%)
        double ppg = team.Last10Matches.PointsPerGame;
        double ppgScore = (ppg / 3.0) * 50;
        score += ppgScore;

        // Goal difference (30%)
        int goalDiff = team.Last10Matches.GoalsScored - team.Last10Matches.GoalsConceded;
        double gdScore = Math.Min(Math.Max((goalDiff + 10) * 1.5, 0), 30);
        score += gdScore;

        // Form trend bonus (20%)
        double trendScore = team.FormTrend switch
        {
            "EXCELLENT" => 20,
            "IMPROVING" => 15,
            "STABLE" => 10,
            "DECLINING" => 5,
            "POOR" => 0,
            _ => 10
        };
        score += trendScore;

        return Math.Min(Math.Round(score, 2), 100);
    }

    #endregion

    #region Attack Strength

    private double CalculateAttackStrength(TeamAIProfile team)
    {
        double score = 0;

        // Goals per game (50%)
        double leagueAvg = 1.5;
        double goalsScore = Math.Min((team.AvgGoalsScored / leagueAvg), 2.0) * 50;
        score += goalsScore;

        // Shot accuracy (25%)
        double shotScore = team.ShotAccuracy * 0.25;
        score += shotScore;

        // Expected goals (25%)
        if (team.ExpectedGoals > 0 && team.MatchesPlayed > 0)
        {
            double xgPerGame = team.ExpectedGoals / team.MatchesPlayed;
            double xgScore = Math.Min((xgPerGame / leagueAvg), 2.0) * 25;
            score += xgScore;
        }
        else
        {
            score += 12.5; // Neutral
        }

        return Math.Min(Math.Round(score, 2), 100);
    }

    #endregion

    #region Defense Strength

    private double CalculateDefenseStrength(TeamAIProfile team)
    {
        double score = 0;

        // Clean sheet percentage (40%)
        double cleanSheetScore = team.CleanSheetPercentage * 0.4;
        score += cleanSheetScore;

        // Goals conceded per game (40%)
        double leagueAvg = 1.5;
        double concededRatio = Math.Max(0, (leagueAvg - team.AvgGoalsConceded) / leagueAvg);
        double concededScore = concededRatio * 40;
        score += concededScore;

        // Expected goals against (20%)
        if (team.ExpectedGoalsAgainst > 0 && team.MatchesPlayed > 0)
        {
            double xgaPerGame = team.ExpectedGoalsAgainst / team.MatchesPlayed;
            double xgaRatio = Math.Max(0, (leagueAvg - xgaPerGame) / leagueAvg);
            score += xgaRatio * 20;
        }
        else
        {
            score += 10; // Neutral
        }

        return Math.Min(Math.Round(score, 2), 100);
    }

    #endregion

    #region Home/Away Factors

    private double CalculateHomeAdvantage(TeamAIProfile team, LeagueAIProfile league)
    {
        if (team.Last5Home.MatchCount == 0) return 50;

        double basescore = (team.Last5Home.PointsPerGame / 3.0) * 70;
        double leagueBonus = league.HomeAdvantageStrength * 30;

        return Math.Min(Math.Round(basescore + leagueBonus, 2), 100);
    }

    private double CalculateAwayWeakness(TeamAIProfile team)
    {
        if (team.Last5Away.MatchCount == 0) return 50;

        double awayPPG = team.Last5Away.PointsPerGame;
        double weaknessScore = Math.Max(0, (3.0 - awayPPG) / 3.0) * 100;

        return Math.Round(weaknessScore, 2);
    }

    #endregion

    #region Head-to-Head

    private double CalculateH2HAdvantage(HeadToHeadInfo h2h, bool isHome)
    {
        if (h2h.TotalMeetings == 0) return 50;

        int wins = isHome ? h2h.HomeWins : h2h.AwayWins;
        int total = h2h.TotalMeetings;

        double winRate = (double)wins / total;
        double score = winRate * 100;

        return Math.Round(score, 2);
    }

    private double CalculateH2HGoalTrend(HeadToHeadInfo h2h)
    {
        if (h2h.TotalMeetings == 0) return 50;

        // Over 2.5 likelihood
        return Math.Round(h2h.Over25Percentage, 2);
    }

    #endregion

    #region Market Intelligence

    private double CalculateMarketConfidence(MarketOddsInfo odds)
    {
        if (!odds.OddsAvailable) return 50;

        // Lower odds = higher confidence from market
        double minOdd = Math.Min(odds.HomeWinOdds, Math.Min(odds.DrawOdds, odds.AwayWinOdds));

        // Convert odds to confidence (1.5 = high confidence, 4.0 = low)
        double confidence = Math.Max(0, (4.0 - minOdd) / 2.5) * 100;

        return Math.Min(Math.Round(confidence, 2), 100);
    }

    private double OddsToProbability(double odds)
    {
        if (odds <= 1.0) return 0;
        return Math.Round((1.0 / odds) * 100, 2);
    }

    #endregion
}

/// <summary>
/// Prediction features model
/// </summary>
public class PredictionFeatures
{
    public int FixtureId { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }

    // Form Features (0-100)
    public double HomeFormScore { get; set; }
    public double AwayFormScore { get; set; }

    // Attack Features (0-100)
    public double HomeAttackStrength { get; set; }
    public double AwayAttackStrength { get; set; }

    // Defense Features (0-100)
    public double HomeDefenseStrength { get; set; }
    public double AwayDefenseStrength { get; set; }

    // Home Advantage (0-100)
    public double HomeAdvantageScore { get; set; }
    public double AwayWeaknessScore { get; set; }

    // Head-to-Head (0-100)
    public double H2HHomeAdvantage { get; set; }
    public double H2HGoalTrend { get; set; }

    // Market Intelligence (0-100)
    public double MarketConfidence { get; set; }
    public double ImpliedHomeProbability { get; set; }
    public double ImpliedDrawProbability { get; set; }
    public double ImpliedAwayProbability { get; set; }

    // League Intelligence
    public double LeagueGoalAverage { get; set; }
    public double LeagueUnpredictability { get; set; }

    // Data Quality
    public double DataQualityScore { get; set; }
}
