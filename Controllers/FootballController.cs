using LiveKicks.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace LiveKicks.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FootballController : ControllerBase
{
    // CHANGED: FootballApiService -> IFootballApiService
    private readonly IFootballApiService _footballService;

    private readonly ILogger<FootballController> _logger;
    private readonly IConfiguration _configuration;


    // CHANGED: FootballApiService -> IFootballApiService
    public FootballController(
        IFootballApiService footballService,
        ILogger<FootballController> logger,
        IConfiguration configuration)
    {
        _footballService = footballService;
        _logger = logger;
        _configuration = configuration;
    }


    [HttpGet("diagnostics")]
    public IActionResult GetDiagnostics()
    {
        try
        {
            var apiToken = _configuration["FootballData:ApiToken"];
            var baseUrl = _configuration["FootballData:BaseUrl"];

            var tokenExists = !string.IsNullOrEmpty(apiToken);

            var maskedToken = tokenExists && apiToken!.Length >= 4
                ? $"{apiToken.Substring(0, 4)}****"
                : "Not configured";


            var diagnostics = new
            {
                timestamp = DateTime.UtcNow,

                provider = "Football-Data.org",

                configuration = new
                {
                    baseUrl = baseUrl ?? "Not configured",
                    apiTokenConfigured = tokenExists,
                    apiTokenMasked = maskedToken
                },

                environmentVariables = new
                {
                    footballDataToken =
                        !string.IsNullOrEmpty(
                            Environment.GetEnvironmentVariable("FootballData__ApiToken"))
                            ? "Set"
                            : "Not set",

                    footballDataBaseUrl =
                        !string.IsNullOrEmpty(
                            Environment.GetEnvironmentVariable("FootballData__BaseUrl"))
                            ? "Set"
                            : "Not set"
                }
            };


            _logger.LogInformation(
                "Football-Data diagnostics called. Token configured: {Configured}",
                tokenExists);


            return Ok(diagnostics);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Diagnostics failed");

            return StatusCode(500, new
            {
                message = "Diagnostics error",
                error = ex.Message
            });
        }
    }



    [HttpGet("fixtures/today")]
    public async Task<IActionResult> GetFixturesToday()
    {
        try
        {
            _logger.LogInformation(
                "GetFixturesToday endpoint called");


            var result =
                await _footballService.GetFixturesTodayAsync();


            if(result == null)
            {
                return StatusCode(500, new
                {
                    message =
                    "Failed to fetch fixtures from Football-Data.org"
                });
            }


            return Ok(result);
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching today's fixtures");


            return StatusCode(500, new
            {
                message = "Internal server error",
                error = ex.Message
            });
        }
    }



    [HttpGet("live")]
    public async Task<IActionResult> GetLiveFixtures()
    {
        var result =
            await _footballService.GetLiveFixturesAsync();


        return result == null
            ? StatusCode(500, new { message = "Failed to fetch live fixtures" })
            : Ok(result);
    }



    [HttpGet("fixture/{id}")]
    public async Task<IActionResult> GetFixtureById(int id)
    {
        if(id <= 0)
            return BadRequest(new { message = "Invalid fixture ID" });


        var result =
            await _footballService.GetFixtureByIdAsync(id);


        return result == null
            ? StatusCode(500, new { message = "Failed to fetch fixture" })
            : Ok(result);
    }



    [HttpGet("statistics/{fixtureId}")]
    public async Task<IActionResult> GetStatistics(int fixtureId)
    {
        var result =
            await _footballService.GetStatisticsAsync(fixtureId);


        return result == null
            ? StatusCode(500, new { message = "Failed to fetch statistics" })
            : Ok(result);
    }



    [HttpGet("events/{fixtureId}")]
    public async Task<IActionResult> GetEvents(int fixtureId)
    {
        var result =
            await _footballService.GetEventsAsync(fixtureId);


        return result == null
            ? StatusCode(500, new { message = "Failed to fetch events" })
            : Ok(result);
    }



    [HttpGet("standings/{leagueId}")]
    public async Task<IActionResult> GetStandings(
        int leagueId,
        [FromQuery] int season = 2024)
    {
        var result =
            await _footballService.GetStandingsAsync(
                leagueId,
                season);


        return result == null
            ? StatusCode(500, new { message = "Failed to fetch standings" })
            : Ok(result);
    }



    [HttpGet("headtohead/{team1}/{team2}")]
    public async Task<IActionResult> GetHeadToHead(
        int team1,
        int team2)
    {
        var result =
            await _footballService.GetHeadToHeadAsync(
                team1,
                team2);


        return result == null
            ? StatusCode(500, new { message = "Failed to fetch head-to-head" })
            : Ok(result);
    }



    [HttpGet("odds/{fixtureId}")]
    public async Task<IActionResult> GetOdds(int fixtureId)
    {
        var result =
            await _footballService.GetOddsAsync(fixtureId);


        return result == null
            ? StatusCode(500, new { message = "Failed to fetch odds" })
            : Ok(result);
    }



    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            provider = "Football-Data.org",
            timestamp = DateTime.UtcNow
        });
    }
}