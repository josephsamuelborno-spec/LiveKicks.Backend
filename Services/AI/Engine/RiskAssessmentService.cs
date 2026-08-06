using LiveKicks.Backend.Models.AI;

namespace LiveKicks.Backend.Services.AI.Engine;

/// <summary>
/// Assesses prediction risk based on volatility, uncertainty, and external factors
/// </summary>
public class RiskAssessmentService
{
    public Task<RiskAssessment> AssessRiskAsync(
        PredictionFeatures features,
        ConfidenceResult confidence,
        AIContextResponse context)
    {
        return Task.FromResult(
            AssessRisk(features, confidence, context));
    }


    public RiskAssessment AssessRisk(
        PredictionFeatures features,
        ConfidenceResult confidence,
        AIContextResponse context)
    {
        var riskFactors = new List<RiskFactor>();
        double totalRiskScore = 0;


        var formRisk = AssessFormVolatility(features, context);
        AddRisk(formRisk, 0.25, riskFactors, ref totalRiskScore);


        var injuryRisk = AssessInjuryRisk(context);
        AddRisk(injuryRisk, 0.20, riskFactors, ref totalRiskScore);


        var h2hRisk = AssessH2HUnpredictability(features, context);
        AddRisk(h2hRisk, 0.15, riskFactors, ref totalRiskScore);


        var leagueRisk = AssessLeagueCompetitiveness(context);
        AddRisk(leagueRisk, 0.15, riskFactors, ref totalRiskScore);


        var marketRisk = AssessMarketDisagreement(features, confidence);
        AddRisk(marketRisk, 0.15, riskFactors, ref totalRiskScore);


        var dataRisk = AssessDataQuality(context);
        AddRisk(dataRisk, 0.10, riskFactors, ref totalRiskScore);



        totalRiskScore = Math.Min(1, totalRiskScore);


        return new RiskAssessment
        {
            RiskScore = totalRiskScore,
            RiskLevel = DetermineRiskLevel(totalRiskScore),
            RiskFactors = riskFactors,
            Recommendation =
                GenerateRecommendation(
                    DetermineRiskLevel(totalRiskScore),
                    totalRiskScore,
                    riskFactors)
        };
    }



    private void AddRisk(
        RiskFactor factor,
        double weight,
        List<RiskFactor> factors,
        ref double score)
    {
        if (factor.Score > 0.3)
        {
            factors.Add(factor);
            score += factor.Score * weight;
        }
    }



    private RiskFactor AssessFormVolatility(
        PredictionFeatures features,
        AIContextResponse context)
    {
        double volatility = 0;
        var reasons = new List<string>();


        var homeResults =
            context.HomeTeam?.Form?.Last5Results 
            ?? new List<string>();

        var awayResults =
            context.AwayTeam?.Form?.Last5Results
            ?? new List<string>();


        var homeVariance = CalculateFormVariance(homeResults);

        if (homeVariance > 0.6)
        {
            volatility += 0.3;
            reasons.Add(
                $"Home team inconsistent form ({homeVariance:F2})");
        }



        var awayVariance = CalculateFormVariance(awayResults);

        if (awayVariance > 0.6)
        {
            volatility += 0.3;
            reasons.Add(
                $"Away team inconsistent form ({awayVariance:F2})");
        }



        if (Math.Abs(features.FormScore) < 0.1)
        {
            volatility += 0.2;
            reasons.Add("Closely matched recent form");
        }



        return new RiskFactor
        {
            Name = "Form Volatility",
            Score = Math.Min(1, volatility),
            Description = string.Join("; ", reasons)
        };
    }



    private RiskFactor AssessInjuryRisk(
        AIContextResponse context)
    {
        double risk = 0;
        var reasons = new List<string>();


        // FIXED: Injuries is List<InjuryInfo>
        int homeInjuries =
            context.HomeTeam?.Injuries?.Count ?? 0;


        int awayInjuries =
            context.AwayTeam?.Injuries?.Count ?? 0;



        if(homeInjuries >= 3)
        {
            risk += 0.4;
            reasons.Add(
                $"Home team has {homeInjuries} injuries");
        }
        else if(homeInjuries >=2)
        {
            risk +=0.2;
            reasons.Add(
                $"Home team has {homeInjuries} injuries");
        }



        if(awayInjuries >=3)
        {
            risk +=0.4;
            reasons.Add(
                $"Away team has {awayInjuries} injuries");
        }
        else if(awayInjuries>=2)
        {
            risk+=0.2;
            reasons.Add(
                $"Away team has {awayInjuries} injuries");
        }



        return new RiskFactor
        {
            Name="Injury/Suspension Risk",
            Score=Math.Min(1,risk),
            Description =
                reasons.Count>0
                ? string.Join("; ", reasons)
                : "No significant injury concerns"
        };
    }



    private RiskFactor AssessH2HUnpredictability(
        PredictionFeatures features,
        AIContextResponse context)
    {
        double risk=0;
        var reasons=new List<string>();

        var h2h=context.HeadToHead;


        if(h2h == null || h2h.TotalMatches <3)
        {
            risk=0.4;
            reasons.Add(
                "Limited head-to-head history");
        }
        else
        {
            double homeRate =
                (double)h2h.HomeWins /
                h2h.TotalMatches;

            double awayRate =
                (double)h2h.AwayWins /
                h2h.TotalMatches;


            double drawRate =
                (double)h2h.Draws /
                h2h.TotalMatches;


            if(Math.Abs(homeRate-awayRate)<0.2)
            {
                risk+=0.5;
                reasons.Add("Balanced H2H record");
            }


            if(drawRate>0.4)
            {
                risk+=0.3;
                reasons.Add(
                    $"High draw rate ({drawRate:P0})");
            }
        }


        return new RiskFactor
        {
            Name="H2H Unpredictability",
            Score=Math.Min(1,risk),
            Description=string.Join("; ",reasons)
        };
    }



    private RiskFactor AssessLeagueCompetitiveness(
        AIContextResponse context)
    {
        double score=0;
        var reasons=new List<string>();

        var league=context.League;


        if(league!=null)
        {
            if(league.CompetitivenessRating>0.7)
            {
                score+=0.4;
                reasons.Add(
                    "Highly competitive league");
            }
        }


        return new RiskFactor
        {
            Name="League Competitiveness",
            Score=Math.Min(1,score),
            Description=
                reasons.Count>0
                ? string.Join("; ",reasons)
                : "Normal league dynamics"
        };
    }



    private RiskFactor AssessMarketDisagreement(
        PredictionFeatures features,
        ConfidenceResult confidence)
    {
        double score=0;
        var reasons=new List<string>();


        if(confidence.ConfidenceScore <0.5 &&
           features.MarketConfidence>0.7)
        {
            score+=0.5;
            reasons.Add(
                "Market confident but model uncertain");
        }


        if(features.MarketConfidence<0.6)
        {
            score+=0.3;
            reasons.Add(
                "Market uncertainty detected");
        }


        return new RiskFactor
        {
            Name="Market Disagreement",
            Score=Math.Min(1,score),
            Description=string.Join("; ",reasons)
        };
    }



    private RiskFactor AssessDataQuality(
        AIContextResponse context)
    {
        double risk=0;
        var reasons=new List<string>();

        var quality=context.DataQuality;


        if(quality==null)
        {
            risk=.5;
            reasons.Add(
                "Data quality unavailable");
        }
        else
        {
            if(quality.CompletenessScore<0.6)
                risk+=.5;

            if(quality.ReliabilityScore<0.6)
                risk+=.4;
        }


        return new RiskFactor
        {
            Name="Data Quality",
            Score=Math.Min(1,risk),
            Description=string.Join("; ",reasons)
        };
    }



    private double CalculateFormVariance(List<string> results)
    {
        if(results.Count==0)
            return .5;


        double wins =
            results.Count(x=>x=="W");

        double draws =
            results.Count(x=>x=="D");

        double losses =
            results.Count(x=>x=="L");


        double total=results.Count;


        double entropy=0;


        foreach(var value in new[]
        {
            wins/total,
            draws/total,
            losses/total
        })
        {
            if(value>0)
                entropy-=value*Math.Log(value);
        }


        return Math.Min(1,entropy/1.1);
    }



    private string DetermineRiskLevel(double score)
    {
        if(score>=0.7)
            return "High";

        if(score>=0.4)
            return "Medium";

        return "Low";
    }



    private string GenerateRecommendation(
        string level,
        double score,
        List<RiskFactor> factors)
    {
        return level switch
        {
            "High" =>
            $"High risk ({score:P0}). Reduce stake. " +
            $"Concerns: {string.Join(", ",factors.Take(2).Select(x=>x.Name))}",

            "Medium" =>
            $"Medium risk ({score:P0}). Consider safer markets.",

            _ =>
            $"Low risk ({score:P0}). Standard confidence."
        };
    }
}



public class RiskAssessment
{
    public double RiskScore {get;set;}

    public string RiskLevel {get;set;} = "";

    public List<RiskFactor> RiskFactors {get;set;} = new();

    public string Recommendation {get;set;}="";
}



public class RiskFactor
{
    public string Name {get;set;}="";

    public double Score {get;set;}

    public string Description {get;set;}="";
}