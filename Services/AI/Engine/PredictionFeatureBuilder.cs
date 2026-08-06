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
        _logger.LogDebug("Building prediction features...");

        var features = new PredictionFeatures
        {
            FixtureId = context.Fixture.FixtureId,
            HomeTeamId = context.Fixture.HomeTeamId,
            AwayTeamId = context.Fixture.AwayTeamId,

            HomeFormScore = CalculateFormScore(context.HomeTeam),
            AwayFormScore = CalculateFormScore(context.AwayTeam),

            HomeAttackStrength = CalculateAttackStrength(context.HomeTeam),
            AwayAttackStrength = CalculateAttackStrength(context.AwayTeam),

            HomeDefenseStrength = CalculateDefenseStrength(context.HomeTeam),
            AwayDefenseStrength = CalculateDefenseStrength(context.AwayTeam),

            HomeAdvantageScore = CalculateHomeAdvantage(
                context.HomeTeam,
                context.LeagueProfile),

            AwayWeaknessScore = CalculateAwayWeakness(context.AwayTeam),

            H2HHomeAdvantage = CalculateH2HAdvantage(
                context.HeadToHead,
                true),

            H2HGoalTrend = CalculateH2HGoalTrend(context.HeadToHead),

            MarketConfidence = CalculateMarketConfidence(context.MarketOdds),

            ImpliedHomeProbability = OddsToProbability(context.MarketOdds.HomeWinOdds),
            ImpliedDrawProbability = OddsToProbability(context.MarketOdds.DrawOdds),
            ImpliedAwayProbability = OddsToProbability(context.MarketOdds.AwayWinOdds),

            LeagueGoalAverage = context.LeagueProfile.AvgGoalsPerMatch,
            LeagueUnpredictability = context.LeagueProfile.UnpredictabilityFactor,

            DataQualityScore = context.DataQuality.OverallScore
        };

        _logger.LogDebug(
            $"Features built: Home Form {features.HomeFormScore:F1}, Away Form {features.AwayFormScore:F1}");

        return features;
    }


    private double CalculateFormScore(TeamAIProfile team)
    {
        if (team.Last10Matches.MatchCount == 0)
            return 50;

        double score = 0;

        double ppgScore =
            (team.Last10Matches.PointsPerGame / 3.0) * 50;

        score += ppgScore;


        int goalDiff =
            team.Last10Matches.GoalsScored -
            team.Last10Matches.GoalsConceded;

        score += Math.Min(
            Math.Max((goalDiff + 10) * 1.5, 0),
            30);


        score += team.FormTrend switch
        {
            "EXCELLENT" => 20,
            "IMPROVING" => 15,
            "STABLE" => 10,
            "DECLINING" => 5,
            "POOR" => 0,
            _ => 10
        };


        return Math.Min(Math.Round(score, 2), 100);
    }


    private double CalculateAttackStrength(TeamAIProfile team)
    {
        double score = 0;

        const double leagueAvg = 1.5;

        score += Math.Min(
            (team.AvgGoalsScored / leagueAvg),
            2.0) * 50;


        score += team.ShotAccuracy * 0.25;


        if (team.ExpectedGoals > 0 &&
            team.MatchesPlayed > 0)
        {
            double xg =
                team.ExpectedGoals /
                team.MatchesPlayed;

            score += Math.Min(
                (xg / leagueAvg),
                2.0) * 25;
        }
        else
        {
            score += 12.5;
        }


        return Math.Min(Math.Round(score, 2), 100);
    }


    private double CalculateDefenseStrength(TeamAIProfile team)
    {
        double score = 0;

        const double leagueAvg = 1.5;

        score += team.CleanSheetPercentage * 0.4;


        double concededRatio =
            Math.Max(
                0,
                (leagueAvg - team.AvgGoalsConceded)
                / leagueAvg);


        score += concededRatio * 40;


        if (team.ExpectedGoalsAgainst > 0 &&
            team.MatchesPlayed > 0)
        {
            double xga =
                team.ExpectedGoalsAgainst /
                team.MatchesPlayed;


            score += Math.Max(
                0,
                (leagueAvg - xga) / leagueAvg)
                * 20;
        }
        else
        {
            score += 10;
        }


        return Math.Min(Math.Round(score, 2), 100);
    }


    private double CalculateHomeAdvantage(
        TeamAIProfile team,
        LeagueAIProfile league)
    {
        if (team.Last5Home.MatchCount == 0)
            return 50;


        double score =
            (team.Last5Home.PointsPerGame / 3.0) * 70;


        score += league.HomeAdvantageStrength * 30;


        return Math.Min(Math.Round(score, 2), 100);
    }


    private double CalculateAwayWeakness(TeamAIProfile team)
    {
        if (team.Last5Away.MatchCount == 0)
            return 50;


        return Math.Round(
            Math.Max(
                0,
                (3.0 - team.Last5Away.PointsPerGame) / 3.0)
            * 100,
            2);
    }


    private double CalculateH2HAdvantage(
        HeadToHeadInfo h2h,
        bool isHome)
    {
        if (h2h.TotalMeetings == 0)
            return 50;


        int wins = isHome
            ? h2h.HomeWins
            : h2h.AwayWins;


        return Math.Round(
            ((double)wins / h2h.TotalMeetings) * 100,
            2);
    }


    private double CalculateH2HGoalTrend(HeadToHeadInfo h2h)
    {
        if (h2h.TotalMeetings == 0)
            return 50;


        return Math.Round(
            h2h.Over25Percentage,
            2);
    }


    private double CalculateMarketConfidence(
        MarketOddsInfo odds)
    {
        if (!odds.OddsAvailable)
            return 50;


        double minOdd =
            Math.Min(
                odds.HomeWinOdds,
                Math.Min(
                    odds.DrawOdds,
                    odds.AwayWinOdds));


        double confidence =
            Math.Max(
                0,
                (4.0 - minOdd) / 2.5)
            * 100;


        return Math.Min(
            Math.Round(confidence, 2),
            100);
    }


    private double OddsToProbability(double odds)
    {
        if (odds <= 1)
            return 0;


        return Math.Round(
            (1 / odds) * 100,
            2);
    }
}