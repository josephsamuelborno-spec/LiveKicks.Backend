using LiveKicks.Backend.Models.AI;
using LiveKicks.Backend.Services.AI.Engine;

namespace LiveKicks.Backend.Services.AI;

/// <summary>
/// Top-level orchestrator for AI prediction pipeline
/// Coordinates context aggregation, prediction generation, and ranking
/// </summary>
public class AIPredictionOrchestrator
{
    private readonly AIContextService _contextService;
    private readonly ElitePredictionEngine _predictionEngine;
    private readonly PredictionRankingService _rankingService;
    private readonly ILogger<AIPredictionOrchestrator> _logger;

    public AIPredictionOrchestrator(
        AIContextService contextService,
        ElitePredictionEngine predictionEngine,
        PredictionRankingService rankingService,
        ILogger<AIPredictionOrchestrator> logger)
    {
        _contextService = contextService;
        _predictionEngine = predictionEngine;
        _rankingService = rankingService;
        _logger = logger;
    }


    /// <summary>
    /// Generate top predictions for today's fixtures
    /// </summary>
    public async Task<List<Models.AI.RankedPrediction>> GetTopPredictionsAsync(
        int maxResults = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting top predictions pipeline (max: {Max})",
            maxResults);

        try
        {
            var contexts =
                await _contextService.GetTodayFixtureContextsAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} fixture contexts",
                contexts.Count);


            if (contexts.Count == 0)
            {
                _logger.LogWarning(
                    "No fixtures available for prediction");

                return new List<Models.AI.RankedPrediction>();
            }


            var allPredictions =
                new List<(AIContextResponse Context, List<PredictionResult> Predictions)>();


            foreach (var context in contexts)
            {
                try
                {
                    var predictions =
                        await _predictionEngine.GeneratePredictionsAsync(
                            context,
                            cancellationToken);


                    allPredictions.Add(
                        (context, predictions));


                    _logger.LogDebug(
                        "Generated {Count} predictions for fixture {Id}",
                        predictions.Count,
                        context.Fixture.FixtureId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed generating predictions for fixture {Id}",
                        context.Fixture.FixtureId);
                }
            }


            var rankedPredictions =
                _rankingService.RankPredictions(
                    allPredictions,
                    maxResults);



            var apiPredictions =
                rankedPredictions.Select(rp =>
                    new Models.AI.RankedPrediction
                    {
                        Rank = rp.Rank,

                        FixtureId = rp.FixtureId,

                        LeagueName = rp.League,

                        MatchDescription =
                            $"{rp.HomeTeam} vs {rp.AwayTeam}",


                        Market =
                            rp.Prediction.Market,


                        Prediction =
                            rp.Prediction,


                        // Updated model
                        Confidence =
                            rp.Prediction.Confidence,


                        // Updated model
                        Reliability =
                            rp.Prediction.Reliability,


                        QualityScore =
                            rp.QualityScore,

                        Probability =
                            rp.Prediction.Probability,


                        // Updated model
                        Risk =
                            rp.Prediction.Risk,


                        TopReasons =
                            rp.Prediction.TopReasons
                                .Take(3)
                                .ToList()

                    })
                    .ToList();



            _logger.LogInformation(
                "Top predictions complete: {Count} picks from {Total} fixtures",
                apiPredictions.Count,
                contexts.Count);


            return apiPredictions;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Top predictions pipeline failed");

            throw;
        }
    }



    /// <summary>
    /// Generate predictions for a specific fixture
    /// </summary>
    public async Task<List<PredictionResult>> GetFixturePredictionsAsync(
        int fixtureId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating predictions for fixture {Id}",
            fixtureId);


        var context =
            await _contextService.GetFixtureContextAsync(
                fixtureId,
                cancellationToken);



        if (context == null)
        {
            _logger.LogWarning(
                "No context found for fixture {Id}",
                fixtureId);

            return new List<PredictionResult>();
        }



        var predictions =
            await _predictionEngine.GeneratePredictionsAsync(
                context,
                cancellationToken);



        _logger.LogInformation(
            "Generated {Count} predictions for fixture {Id}",
            predictions.Count,
            fixtureId);



        return predictions;
    }
}




