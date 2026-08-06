using LiveKicks.Backend.Models.AI;

namespace LiveKicks.Backend.Services.AI.Engine;

/// <summary>
/// Backend team rating calculator - migrated from MAUI EliteTeamRatingCalculator
/// Calculates 12 team ratings and overall strength from AIContextResponse data
/// </summary>
public class EliteTeamRatingCalculator
{
    /// <summary>
    /// Calculate comprehensive team ratings from AI context data
    /// </summary>
    public TeamRatings CalculateTeamRatings(TeamAIProfile team, bool isHome)
    {
        return new TeamRatings
        {
            Attack = CalculateAttackRating(team),
            Defense = CalculateDefenseRating(team),
            HomeStrength = isHome ? CalculateHomeRating(team) : 50,
            AwayStrength = !isHome ? CalculateAwayRating(team) : 50,
            Form = CalculateFormRating(team),
            Momentum = CalculateMomentumRating(team),
            Consistency = CalculateConsistencyRating(team),
            MentalStrength = CalculateMentalStrengthRating(team),
            Fitness = CalculateFitnessRating(team),
            AvgGoals = CalculateAvgGoalsRating(team),
            ExpectedGoals = CalculateExpectedGoalsRating(team),
            DefensiveStability = CalculateDefensiveStabilityRating(team),
            Overall = 0 // Calculated below
        };
    }

    private double CalculateAttackRating(TeamAIProfile team)
    {
        if (team.Form == null) return 50;

        double rating = 50;

        // Goals scored weight
        double avgGoals = team.Form.GoalsForAvg;
        rating += (avgGoals - 1.3) * 15; // 1.3 is average baseline

        // Scoring efficiency
        if (team.Form.ScoredIn != null)
        {
            double scoringRate = team.Form.ScoredIn.Percentage / 100.0;
            rating += (scoringRate - 0.65) * 20;
        }

        return Math.Max(0, Math.Min(100, Math.Round(rating, 1)));
    }

    private double CalculateDefenseRating(TeamAIProfile team)
    {
        if (team.Form == null) return 50;

        double rating = 50;

        // Goals conceded weight (inverse - lower is better)
        double avgConceded = team.Form.GoalsAgainstAvg;
        rating += (1.0 - avgConceded) * 15;

        // Clean sheet rate
        if (team.Form.CleanSheets > 0)
        {
            double cleanSheetRate = team.Form.CleanSheets / 100.0;
            rating += cleanSheetRate * 30;
        }

        return Math.Max(0, Math.Min(100, Math.Round(rating, 1)));
    }

    private double CalculateHomeRating(TeamAIProfile team)
    {
        if (team.Form == null) return 50;

        double rating = 50;

        // Home win rate
        var homeStats = team.Form.Home;
        if (homeStats != null && homeStats.Played > 0)
        {
            double winRate = (double)homeStats.Win / homeStats.Played;
            rating += winRate * 40;

            // Home goals scored
            double homeGoalsAvg = homeStats.GoalsForAvg;
            rating += (homeGoalsAvg - 1.5) * 10;
        }

        return Math.Max(0, Math.Min(100, Math.Round(rating, 1)));
    }

    private double CalculateAwayRating(TeamAIProfile team)
    {
        if (team.Form == null) return 50;

        double rating = 50;

        // Away performance
        var awayStats = team.Form.Away;
        if (awayStats != null && awayStats.Played > 0)
        {
            double winRate = (double)awayStats.Win / awayStats.Played;
            rating += winRate * 35;

            // Away goals
            double awayGoalsAvg = awayStats.GoalsForAvg;
            rating += (awayGoalsAvg - 1.0) * 15;
        }

        return Math.Max(0, Math.Min(100, Math.Round(rating, 1)));
    }

    private double CalculateFormRating(TeamAIProfile team)
    {
        if (team.Form?.Last5Results == null) return 50;

        var results = team.Form.Last5Results;
        int points = 0;

        foreach (var result in results)
        {
            if (result == "W") points += 3;
            else if (result == "D") points += 1;
        }

        // Max 15 points possible
        double rating = (points / 15.0) * 100;
        return Math.Round(rating, 1);
    }

    private double CalculateMomentumRating(TeamAIProfile team)
    {
        if (team.Form?.Last5Results == null || team.Form.Last5Results.Count < 3)
            return 50;

        var results = team.Form.Last5Results;

        // Recent results weighted more heavily
        double momentum = 0;
        var weights = new[] { 1.5, 1.3, 1.1, 0.9, 0.7 }; // Most recent first

        for (int i = 0; i < Math.Min(results.Count, 5); i++)
        {
            if (results[i] == "W")
                momentum += 3 * weights[i];
            else if (results[i] == "D")
                momentum += 1 * weights[i];
        }

        // Normalize to 0-100
        double maxMomentum = 3 * weights.Take(Math.Min(results.Count, 5)).Sum();
        double rating = (momentum / maxMomentum) * 100;

        return Math.Round(rating, 1);
    }

    private double CalculateConsistencyRating(TeamAIProfile team)
    {
        if (team.Form?.Last5Results == null || team.Form.Last5Results.Count < 3)
            return 50;

        var results = team.Form.Last5Results;

        // Calculate variance in results
        int wins = results.Count(r => r == "W");
        int draws = results.Count(r => r == "D");
        int losses = results.Count(r => r == "L");

        // Perfect consistency = all same result
        // Maximum inconsistency = evenly split
        double total = results.Count;
        double maxCount = Math.Max(wins, Math.Max(draws, losses));
        double consistency = (maxCount / total) * 100;

        return Math.Round(consistency, 1);
    }

    private double CalculateMentalStrengthRating(TeamAIProfile team)
    {
        if (team.Form == null) return 50;

        double rating = 50;

        // Ability to bounce back from losses
        var results = team.Form.Last5Results;
        if (results != null && results.Count >= 3)
        {
            for (int i = 0; i < results.Count - 1; i++)
            {
                if (results[i] == "L" && results[i + 1] == "W")
                    rating += 15; // Bounced back
            }
        }

        // Win streaks indicate mental strength
        if (results != null)
        {
            int currentStreak = 0;
            foreach (var result in results)
            {
                if (result == "W")
                    currentStreak++;
                else
                    break;
            }
            rating += currentStreak * 8;
        }

        return Math.Max(0, Math.Min(100, Math.Round(rating, 1)));
    }

    private double CalculateFitnessRating(TeamAIProfile team)
    {
        double rating = 100;

        // Penalty for injuries
        int injuries = team.InjuryCount;
        rating -= injuries * 8; // Each injury reduces fitness by 8 points

        return Math.Max(0, Math.Min(100, Math.Round(rating, 1)));
    }

    private double CalculateAvgGoalsRating(TeamAIProfile team)
    {
        if (team.Form == null) return 50;

        double avgGoals = team.Form.GoalsForAvg;

        // Convert to 0-100 scale (2.5 goals = 100)
        double rating = (avgGoals / 2.5) * 100;

        return Math.Max(0, Math.Min(100, Math.Round(rating, 1)));
    }

    private double CalculateExpectedGoalsRating(TeamAIProfile team)
    {
        if (team.Form == null) return 50;

        // Use actual goals as proxy if xG not available
        double xG = team.Form.GoalsForAvg;

        double rating = (xG / 2.0) * 100;

        return Math.Max(0, Math.Min(100, Math.Round(rating, 1)));
    }

    private double CalculateDefensiveStabilityRating(TeamAIProfile team)
    {
        if (team.Form == null) return 50;

        double rating = 50;

        // Clean sheet rate
        if (team.Form.CleanSheets > 0)
        {
            rating += (team.Form.CleanSheets / 100.0) * 30;
        }

        // Low goals conceded
        double avgConceded = team.Form.GoalsAgainstAvg;
        rating += (1.5 - avgConceded) * 20;

        return Math.Max(0, Math.Min(100, Math.Round(rating, 1)));
    }

    /// <summary>
    /// Calculate overall team rating from all components
    /// </summary>
    public double CalculateOverallRating(TeamRatings ratings)
    {
        // Weighted average of all ratings
        double overall = 0;
        overall += ratings.Attack * 0.15;
        overall += ratings.Defense * 0.15;
        overall += ratings.HomeStrength * 0.08;
        overall += ratings.AwayStrength * 0.08;
        overall += ratings.Form * 0.15;
        overall += ratings.Momentum * 0.10;
        overall += ratings.Consistency * 0.08;
        overall += ratings.MentalStrength * 0.05;
        overall += ratings.Fitness * 0.05;
        overall += ratings.AvgGoals * 0.05;
        overall += ratings.ExpectedGoals * 0.03;
        overall += ratings.DefensiveStability * 0.03;

        return Math.Round(overall, 1);
    }
}

public class TeamRatings
{
    public double Attack { get; set; }
    public double Defense { get; set; }
    public double HomeStrength { get; set; }
    public double AwayStrength { get; set; }
    public double Form { get; set; }
    public double Momentum { get; set; }
    public double Consistency { get; set; }
    public double MentalStrength { get; set; }
    public double Fitness { get; set; }
    public double AvgGoals { get; set; }
    public double ExpectedGoals { get; set; }
    public double DefensiveStability { get; set; }
    public double Overall { get; set; }
}
