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

        _httpClient =
            httpClientFactory.CreateClient("FootballData");


        var token =
            configuration["FootballData:ApiToken"];


        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Remove(
                "X-Auth-Token");


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



            var response =
                await _httpClient.GetAsync(
                    endpoint,
                    cancellationToken);



            var json =
                await response.Content.ReadAsStringAsync(
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



            return MapMatches(data?.Matches);
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "FootballData fixtures failed");

            return null;
        }
    }





    public async Task<ApiResponse<FixtureDto>?> GetLiveFixturesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var today =
                await GetFixturesTodayAsync(
                    cancellationToken);


            if(today == null)
                return null;



            var liveStatuses = new[]
            {
                "IN_PLAY",
                "PAUSED"
            };



            today.Response =
                today.Response
                .Where(x =>
                    liveStatuses.Contains(
                        x.Fixture.Status.Short))
                .ToList();



            today.Results =
                today.Response.Count;


            today.Get = "live";


            return today;
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "FootballData live fixtures failed");

            return null;
        }
    }





    public async Task<ApiResponse<FixtureDto>?> GetFixtureByIdAsync(
        int fixtureId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint =
                $"matches/{fixtureId}";


            _logger.LogInformation(
                "Calling FootballData Fixture: {Endpoint}",
                endpoint);



            var response =
                await _httpClient.GetAsync(
                    endpoint,
                    cancellationToken);



            var json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);



            if(!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Fixture API Error: {Response}",
                    json);

                return null;
            }



            var data =
                JsonSerializer.Deserialize<FootballDataSingleMatchResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });



            return MapMatches(
                new List<FootballDataMatch>
                {
                    data!.Match!
                });
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "Fixture lookup failed");

            return null;
        }
    }





    public async Task<ApiResponse<StandingsDto>?> GetStandingsAsync(
        int leagueId,
        int season,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Football-Data.org uses competition codes
            // Premier League example:
            // PL

            string competition = "PL";


            var endpoint =
                $"competitions/{competition}/standings";



            var response =
                await _httpClient.GetAsync(
                    endpoint,
                    cancellationToken);



            var json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);



            if(!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Standings API Error: {Response}",
                    json);

                return null;
            }



            _logger.LogInformation(
                "Standings received");


            // Temporary empty mapping
            // until Standings DTO is expanded
            return new ApiResponse<StandingsDto>
            {
                Get = endpoint,
                Results = 0,
                Response = new List<StandingsDto>()
            };
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "Standings failed");

            return null;
        }
    }






    private ApiResponse<FixtureDto> MapMatches(
        List<FootballDataMatch>? matches)
    {
        var result =
            new ApiResponse<FixtureDto>
            {
                Get = "matches",

                Results = matches?.Count ?? 0,

                Response = new List<FixtureDto>()
            };



        if(matches == null)
            return result;



        foreach(var match in matches)
        {
            result.Response.Add(
                new FixtureDto
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

                        Country =
                        match.Competition?.Area?.Name ?? ""
                    },


                    Teams = new Teams
                    {
                        Home = new Team
                        {
                            Id = match.HomeTeam?.Id ?? 0,

                            Name =
                            match.HomeTeam?.Name ?? ""
                        },


                        Away = new Team
                        {
                            Id = match.AwayTeam?.Id ?? 0,

                            Name =
                            match.AwayTeam?.Name ?? ""
                        }
                    },


                    Goals = new Goals
                    {
                        Home =
                        match.Score?.FullTime?.Home,

                        Away =
                        match.Score?.FullTime?.Away
                    }
                });
        }


        return result;
    }





    public Task<ApiResponse<StatisticsDto>?> GetStatisticsAsync(
        int fixtureId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ApiResponse<StatisticsDto>?>(null);



    public Task<ApiResponse<EventDto>?> GetEventsAsync(
        int fixtureId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ApiResponse<EventDto>?>(null);



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





// ================================
// Football Data Models
// ================================

public class FootballDataMatchesResponse
{
    public List<FootballDataMatch>? Matches { get; set; }
}


public class FootballDataSingleMatchResponse
{
    public FootballDataMatch? Match { get; set; }
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