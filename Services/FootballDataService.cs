using System.Text.Json;
using LiveKicks.Backend.Models.DTOs;

namespace LiveKicks.Backend.Services;

public class FootballDataService : IFootballApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FootballDataService> _logger;


    public FootballDataService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FootballDataService> logger)
    {
        _logger = logger;

        _httpClient = httpClientFactory.CreateClient("FootballData");


        var token = configuration["FootballData:ApiToken"];


        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Remove("X-Auth-Token");

            _httpClient.DefaultRequestHeaders.Add(
                "X-Auth-Token",
                token);
        }


        _logger.LogInformation(
            "FootballData BaseAddress: {BaseAddress}",
            _httpClient.BaseAddress);
    }



    public async Task<ApiResponse<FixtureDto>?> GetFixturesTodayAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string endpoint = "matches";


            _logger.LogInformation(
                "Calling FootballData Endpoint: {Endpoint}",
                endpoint);


            var response = await _httpClient.GetAsync(
                endpoint,
                cancellationToken);



            var json = await response.Content.ReadAsStringAsync(
                cancellationToken);



            _logger.LogInformation(
                "FootballData Status: {Status}",
                response.StatusCode);



            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "FootballData Error: {Response}",
                    json);

                return null;
            }



            var data =
                JsonSerializer.Deserialize<FootballDataMatchesResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });



            var result = new ApiResponse<FixtureDto>
            {
                Get = "matches",

                Results = data?.Matches?.Count ?? 0,

                Response = new List<FixtureDto>()
            };



            if (data?.Matches == null)
                return result;



            foreach(var match in data.Matches)
            {
                result.Response.Add(new FixtureDto
                {
                    Fixture = new Fixture
                    {
                        Id = match.Id,

                        Date = match.UtcDate,

                        Timezone = "UTC",

                        Status = new Status
                        {
                            Long = match.Status ?? "",

                            Short = match.Status ?? ""
                        }
                    },


                    League = new League
                    {
                        Id = match.Competition?.Id ?? 0,

                        Name = match.Competition?.Name ?? "",

                        Country = match.Competition?.Area?.Name ?? ""
                    },


                    Teams = new Teams
                    {
                        Home = new Team
                        {
                            Id = match.HomeTeam?.Id ?? 0,

                            Name = match.HomeTeam?.Name ?? ""
                        },


                        Away = new Team
                        {
                            Id = match.AwayTeam?.Id ?? 0,

                            Name = match.AwayTeam?.Name ?? ""
                        }
                    },


                    Goals = new Goals
                    {
                        Home = match.Score?.FullTime?.Home,

                        Away = match.Score?.FullTime?.Away
                    },


                    Score = new Score
                    {
                        Fulltime = new GoalDetail
                        {
                            Home = match.Score?.FullTime?.Home,

                            Away = match.Score?.FullTime?.Away
                        }
                    }

                });
            }



            return result;

        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "FootballData fixtures request failed");

            return null;
        }
    }





    public Task<ApiResponse<FixtureDto>?> GetLiveFixturesAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<ApiResponse<FixtureDto>?>(null);



    public Task<ApiResponse<FixtureDto>?> GetFixtureByIdAsync(
        int fixtureId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ApiResponse<FixtureDto>?>(null);



    public Task<ApiResponse<StatisticsDto>?> GetStatisticsAsync(
        int fixtureId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ApiResponse<StatisticsDto>?>(null);



    public Task<ApiResponse<EventDto>?> GetEventsAsync(
        int fixtureId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ApiResponse<EventDto>?>(null);



    public Task<ApiResponse<StandingsDto>?> GetStandingsAsync(
        int leagueId,
        int season,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ApiResponse<StandingsDto>?>(null);



    public Task<ApiResponse<FixtureDto>?> GetHeadToHeadAsync(
        int team1,
        int team2,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ApiResponse<FixtureDto>?>(null);



    public Task<ApiResponse<OddsDto>?> GetOddsAsync(
        int fixtureId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ApiResponse<OddsDto>?>(null);

}






// =====================================
// Football-data.org Models
// =====================================


public class FootballDataMatchesResponse
{
    public List<FootballDataMatch>? Matches { get; set; }
}




public class FootballDataMatch
{
    public int Id { get; set; }


    public DateTime UtcDate { get; set; }


    public string? Status { get; set; }


    public FootballDataCompetition? Competition { get; set; }


    public FootballDataTeam? HomeTeam { get; set; }


    public FootballDataTeam? AwayTeam { get; set; }


    public FootballDataScore? Score { get; set; }

}



public class FootballDataCompetition
{
    public int Id { get; set; }


    public string? Name { get; set; }


    public FootballDataArea? Area { get; set; }
}



public class FootballDataArea
{
    public string? Name { get; set; }
}



public class FootballDataTeam
{
    public int Id { get; set; }


    public string? Name { get; set; }
}



public class FootballDataScore
{
    public FootballDataFullTime? FullTime { get; set; }
}



public class FootballDataFullTime
{
    public int? Home { get; set; }


    public int? Away { get; set; }
}