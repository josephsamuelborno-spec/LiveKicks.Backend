using LiveKicks.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LiveKicks.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FootballController : ControllerBase
{
    private readonly FootballApiService _footballService;
    private readonly ILogger<FootballController> _logger;

    public FootballController(FootballApiService footballService, ILogger<FootballController> logger)
    {
        _footballService = footballService;
        _logger = logger;
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
                return StatusCode(500, new { message = "Failed to fetch fixtures" });
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
