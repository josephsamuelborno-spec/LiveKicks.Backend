using LiveKicks.Backend.Services;
using LiveKicks.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace LiveKicks.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FootballController : ControllerBase
{
    private readonly FootballApiService _footballService;
    private readonly ILogger<FootballController> _logger;
    private readonly IConfiguration _configuration;

    public FootballController(FootballApiService footballService, ILogger<FootballController> logger, IConfiguration configuration)
    {
        _footballService = footballService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Diagnostic endpoint to verify API configuration and connectivity
    /// </summary>
    [HttpGet("diagnostics")]
    public IActionResult GetDiagnostics()
    {
        try
        {
            var apiKey = _configuration["FootballApi:ApiKey"];
            var baseUrl = _configuration["FootballApi:BaseUrl"];
            var cacheDuration = _configuration["FootballApi:CacheDurationMinutes"];

            var apiKeyExists = !string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_API_KEY_HERE";
            var maskedKey = apiKeyExists && apiKey!.Length >= 4 ? $"{apiKey.Substring(0, 4)}****" : "Not configured";

            var diagnostics = new
            {
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                configuration = new
                {
                    baseUrl = baseUrl ?? "Not configured",
                    apiKeyConfigured = apiKeyExists,
                    apiKeyMasked = maskedKey,
                    cacheDurationMinutes = cacheDuration ?? "Not configured"
                },
                environmentVariables = new
                {
                    footballApiKeyEnvVar = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FootballApi__ApiKey")) ? "Set" : "Not set",
                    footballApiBaseUrlEnvVar = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FootballApi__BaseUrl")) ? "Set" : "Not set"
                },
                notes = new[]
                {
                    "If API key shows 'Not configured', set FootballApi__ApiKey environment variable on Render",
                    "BaseUrl should be: https://v3.football.api-sports.io",
                    "Check Render logs for detailed API request/response information"
                }
            };

            _logger.LogInformation("Diagnostics endpoint called - API Key Configured: {ApiKeyConfigured}, BaseUrl: {BaseUrl}", 
                apiKeyExists, baseUrl);

            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in diagnostics endpoint");
            return StatusCode(500, new { message = "Diagnostics error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all fixtures for today
    /// </summary>
    [HttpGet("fixtures/today")]
    public async Task<IActionResult> GetFixturesToday()
    {
        try
        {
            _logger.LogInformation("GetFixturesToday endpoint called");
            var result = await _footballService.GetFixturesTodayAsync();

            if (result == null)
            {
                _logger.LogWarning("FootballApiService returned null for today's fixtures");

                // During debugging, provide helpful diagnostic info
                var apiKey = _configuration["FootballApi:ApiKey"];
                var apiKeyConfigured = !string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_API_KEY_HERE";

                return StatusCode(500, new 
                { 
                    message = "Failed to fetch fixtures from API-FOOTBALL",
                    debugInfo = new
                    {
                        apiKeyConfigured = apiKeyConfigured,
                        suggestion = apiKeyConfigured 
                            ? "Check Render logs for detailed API error. The API may be rate-limited, returning errors, or the endpoint URL may be incorrect."
                            : "API key not configured. Set FootballApi__ApiKey environment variable in Render.",
                        diagnosticsEndpoint = "/api/football/diagnostics"
                    }
                });
            }

            _logger.LogInformation("Successfully retrieved {Count} fixtures for today", result.Results);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFixturesToday");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all live fixtures
    /// </summary>
    [HttpGet("live")]
    public async Task<IActionResult> GetLiveFixtures()
    {
        try
        {
            var result = await _footballService.GetLiveFixturesAsync();
            if (result == null)
            {
                return StatusCode(500, new { message = "Failed to fetch live fixtures" });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetLiveFixtures");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get fixture details by ID
    /// </summary>
    [HttpGet("fixture/{id}")]
    public async Task<IActionResult> GetFixtureById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid fixture ID" });
            }

            var result = await _footballService.GetFixtureByIdAsync(id);
            if (result == null)
            {
                return StatusCode(500, new { message = "Failed to fetch fixture" });
            }

            if (result.Results == 0)
            {
                return NotFound(new { message = "Fixture not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFixtureById for ID: {FixtureId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get match statistics for a fixture
    /// </summary>
    [HttpGet("statistics/{fixtureId}")]
    public async Task<IActionResult> GetStatistics(int fixtureId)
    {
        try
        {
            if (fixtureId <= 0)
            {
                return BadRequest(new { message = "Invalid fixture ID" });
            }

            var result = await _footballService.GetStatisticsAsync(fixtureId);
            if (result == null)
            {
                return StatusCode(500, new { message = "Failed to fetch statistics" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetStatistics for fixture: {FixtureId}", fixtureId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get match events for a fixture
    /// </summary>
    [HttpGet("events/{fixtureId}")]
    public async Task<IActionResult> GetEvents(int fixtureId)
    {
        try
        {
            if (fixtureId <= 0)
            {
                return BadRequest(new { message = "Invalid fixture ID" });
            }

            var result = await _footballService.GetEventsAsync(fixtureId);
            if (result == null)
            {
                return StatusCode(500, new { message = "Failed to fetch events" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEvents for fixture: {FixtureId}", fixtureId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get league standings
    /// </summary>
    [HttpGet("standings/{leagueId}")]
    public async Task<IActionResult> GetStandings(int leagueId, [FromQuery] int season = 2024)
    {
        try
        {
            if (leagueId <= 0)
            {
                return BadRequest(new { message = "Invalid league ID" });
            }

            var result = await _footballService.GetStandingsAsync(leagueId, season);
            if (result == null)
            {
                return StatusCode(500, new { message = "Failed to fetch standings" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetStandings for league: {LeagueId}, season: {Season}", leagueId, season);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get head-to-head matches between two teams
    /// </summary>
    [HttpGet("headtohead/{team1}/{team2}")]
    public async Task<IActionResult> GetHeadToHead(int team1, int team2)
    {
        try
        {
            if (team1 <= 0 || team2 <= 0)
            {
                return BadRequest(new { message = "Invalid team IDs" });
            }

            var result = await _footballService.GetHeadToHeadAsync(team1, team2);
            if (result == null)
            {
                return StatusCode(500, new { message = "Failed to fetch head-to-head" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetHeadToHead for teams: {Team1} vs {Team2}", team1, team2);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get odds for a fixture
    /// </summary>
    [HttpGet("odds/{fixtureId}")]
    public async Task<IActionResult> GetOdds(int fixtureId)
    {
        try
        {
            if (fixtureId <= 0)
            {
                return BadRequest(new { message = "Invalid fixture ID" });
            }

            var result = await _footballService.GetOddsAsync(fixtureId);
            if (result == null)
            {
                return StatusCode(500, new { message = "Failed to fetch odds" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOdds for fixture: {FixtureId}", fixtureId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
