using LiveKicks.Backend.Models.AI;
using LiveKicks.Backend.Services.AI;
using LiveKicks.Backend.Services.AI.Engine;
using Microsoft.AspNetCore.Mvc;

namespace LiveKicks.Backend.Controllers;

[ApiController]
[Route("api/football/ai")]
public class AIController : ControllerBase
{
    private readonly AIContextBuilder _contextBuilder;
    private readonly AIPredictionOrchestrator _predictionOrchestrator;
    private readonly ILogger<AIController> _logger;

    public AIController(
        AIContextBuilder contextBuilder,
        AIPredictionOrchestrator predictionOrchestrator,
        ILogger<AIController> logger)
    {
        _contextBuilder = contextBuilder;
        _predictionOrchestrator = predictionOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Get complete AI context for a fixture
    /// Single optimized request containing all data needed for prediction
    /// </summary>
    [HttpGet("context/{fixtureId}")]
    [ProducesResponseType(typeof(AIContextResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<AIContextResponse>> GetAIContext(
        int fixtureId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? AI context requested for fixture {FixtureId}", fixtureId);

        try
        {
            var context = await _contextBuilder.BuildContextAsync(fixtureId, cancellationToken);

            if (context.DataQuality.OverallScore == 0)
            {
                _logger.LogWarning("? Fixture {FixtureId} not found or no data available", fixtureId);
                return NotFound(new
                {
                    error = "Fixture not found or insufficient data",
                    fixtureId = fixtureId,
                    message = "Unable to build AI context for this fixture"
                });
            }

            _logger.LogInformation(
                "? AI context returned for fixture {FixtureId} - Quality: {Quality}% ({Reliability}), Cached: {Cached}",
                fixtureId,
                context.DataQuality.OverallScore,
                context.DataQuality.Reliability,
                context.FromCache);

            return Ok(context);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("? Request cancelled for fixture {FixtureId}", fixtureId);
            return StatusCode(499, new { message = "Request cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error getting AI context for fixture {FixtureId}", fixtureId);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "Failed to build AI context",
                fixtureId = fixtureId
            });
        }
    }

    /// <summary>
    /// Get top AI predictions for today's fixtures
    /// Phase 2C - Elite AI Prediction Engine (Backend)
    /// </summary>
    [HttpGet("top-predictions")]
    [ProducesResponseType(typeof(List<Models.AI.RankedPrediction>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Models.AI.RankedPrediction>>> GetTopPredictions(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("?? Top predictions requested (limit: {Limit})", limit);

        try
        {
            var topPredictions = await _predictionOrchestrator.GetTopPredictionsAsync(limit, cancellationToken);

            _logger.LogInformation("? Returning {Count} top predictions", topPredictions.Count);

            return Ok(topPredictions);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("? Top predictions request cancelled");
            return StatusCode(499, new { message = "Request cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error getting top predictions");
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "Failed to generate top predictions"
            });
        }
    }

    /// <summary>
    /// Get AI predictions for a specific fixture
    /// Phase 2C - Elite AI Prediction Engine (Backend)
    /// </summary>
    [HttpGet("predictions/{fixtureId}")]
    [ProducesResponseType(typeof(List<PredictionResult>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<PredictionResult>>> GetFixturePredictions(
        int fixtureId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("?? Predictions requested for fixture {FixtureId}", fixtureId);

        try
        {
            var predictions = await _predictionOrchestrator.GetFixturePredictionsAsync(fixtureId, cancellationToken);

            if (predictions.Count == 0)
            {
                _logger.LogWarning("? No predictions available for fixture {FixtureId}", fixtureId);
                return NotFound(new
                {
                    error = "No predictions available",
                    fixtureId = fixtureId,
                    message = "Unable to generate predictions for this fixture"
                });
            }

            _logger.LogInformation("? Returning {Count} predictions for fixture {FixtureId}",
                predictions.Count, fixtureId);

            return Ok(predictions);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("? Predictions request cancelled for fixture {FixtureId}", fixtureId);
            return StatusCode(499, new { message = "Request cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error getting predictions for fixture {FixtureId}", fixtureId);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "Failed to generate predictions",
                fixtureId = fixtureId
            });
        }
    }

    /// <summary>
    /// Health check for AI services
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(200)]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "AI Prediction Engine",
            version = "Phase 2C",
            timestamp = DateTime.UtcNow,
            features = new[]
            {
                "AI Context API",
                "Elite Prediction Engine",
                "Risk Assessment",
                "Prediction Ranking",
                "Top Predictions"
            }
        });
    }
}
