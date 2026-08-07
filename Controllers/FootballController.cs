using LiveKicks.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiveKicks.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FootballController : ControllerBase
{
    private readonly IFootballApiService _footballService;
    private readonly ILogger<FootballController> _logger;
    private readonly IConfiguration _configuration;


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
            var apiToken =
                _configuration["FootballData:ApiToken"];

            var baseUrl =
                _configuration["FootballData:BaseUrl"];


            var tokenExists =
                !string.IsNullOrEmpty(apiToken);


            var maskedToken =
                tokenExists && apiToken!.Length >= 4
                ? $"{apiToken.Substring(0, 4)}****"
                : "Not configured";


            return Ok(new
            {
                timestamp = DateTime.UtcNow,

                provider = "Football-Data.org",

                configuration = new
                {
                    baseUrl =
                        baseUrl ?? "Not configured",

                    apiTokenConfigured =
                        tokenExists,

                    apiTokenMasked =
                        maskedToken
                }
            });
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "Diagnostics failed");

            return StatusCode(500,new
            {
                message="Diagnostics error"
            });
        }
    }





    [HttpGet("fixtures/today")]
    public async Task<IActionResult> GetFixturesToday(
        CancellationToken cancellationToken)
    {
        var result =
            await _footballService.GetFixturesTodayAsync(
                cancellationToken);


        return result == null
            ? StatusCode(500,new
            {
                message="Failed to fetch today's fixtures"
            })
            : Ok(result);
    }





    [HttpGet("fixtures/tomorrow")]
    public async Task<IActionResult> GetFixturesTomorrow(
        CancellationToken cancellationToken)
    {
        var result =
            await _footballService.GetFixturesTomorrowAsync(
                cancellationToken);


        return result == null
            ? StatusCode(500,new
            {
                message="Failed to fetch tomorrow fixtures"
            })
            : Ok(result);
    }





    [HttpGet("live")]
    public async Task<IActionResult> GetLiveFixtures(
        CancellationToken cancellationToken)
    {
        var result =
            await _footballService.GetLiveFixturesAsync(
                cancellationToken);


        return result == null
            ? Ok(new
            {
                get="live",
                results=0,
                response=new List<object>()
            })
            : Ok(result);
    }





    [HttpGet("fixture/{id}")]
    public async Task<IActionResult> GetFixtureById(
        int id,
        CancellationToken cancellationToken)
    {
        if(id <= 0)
        {
            return BadRequest(new
            {
                message="Invalid fixture ID"
            });
        }


        var result =
            await _footballService.GetFixtureByIdAsync(
                id,
                cancellationToken);


        return result == null
            ? StatusCode(500,new
            {
                message="Failed to fetch fixture"
            })
            : Ok(result);
    }





    [HttpGet("team/{id}/history")]
    public async Task<IActionResult> GetTeamHistory(
        int id,
        CancellationToken cancellationToken)
    {
        if(id <= 0)
        {
            return BadRequest(new
            {
                message="Invalid team ID"
            });
        }


        var result =
            await _footballService.GetTeamHistoryAsync(
                id,
                cancellationToken);


        return result == null
            ? StatusCode(500,new
            {
                message="Failed to fetch team history"
            })
            : Ok(result);
    }





    [HttpGet("statistics/{fixtureId}")]
    public async Task<IActionResult> GetStatistics(
        int fixtureId,
        CancellationToken cancellationToken)
    {
        var result =
            await _footballService.GetStatisticsAsync(
                fixtureId,
                cancellationToken);


        return result == null
            ? StatusCode(500,new
            {
                message="Failed to fetch statistics"
            })
            : Ok(result);
    }





    [HttpGet("events/{fixtureId}")]
    public async Task<IActionResult> GetEvents(
        int fixtureId,
        CancellationToken cancellationToken)
    {
        var result =
            await _footballService.GetEventsAsync(
                fixtureId,
                cancellationToken);


        return result == null
            ? StatusCode(500,new
            {
                message="Failed to fetch events"
            })
            : Ok(result);
    }





    [HttpGet("standings/{leagueId}")]
    public async Task<IActionResult> GetStandings(
        int leagueId,
        [FromQuery]int season = 2024,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _footballService.GetStandingsAsync(
                leagueId,
                season,
                cancellationToken);


        return result == null
            ? StatusCode(500,new
            {
                message="Failed to fetch standings"
            })
            : Ok(result);
    }





    [HttpGet("headtohead/{team1}/{team2}")]
    public async Task<IActionResult> GetHeadToHead(
        int team1,
        int team2,
        CancellationToken cancellationToken)
    {
        var result =
            await _footballService.GetHeadToHeadAsync(
                team1,
                team2,
                cancellationToken);


        return result == null
            ? StatusCode(500,new
            {
                message="Failed to fetch head-to-head"
            })
            : Ok(result);
    }





    [HttpGet("odds/{fixtureId}")]
    public async Task<IActionResult> GetOdds(
        int fixtureId,
        CancellationToken cancellationToken)
    {
        var result =
            await _footballService.GetOddsAsync(
                fixtureId,
                cancellationToken);


        return result == null
            ? StatusCode(500,new
            {
                message="Failed to fetch odds"
            })
            : Ok(result);
    }





    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status="healthy",
            provider="Football-Data.org",
            timestamp=DateTime.UtcNow
        });
    }
}