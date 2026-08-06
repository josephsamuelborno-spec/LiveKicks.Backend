using LiveKicks.Backend.Models.DTOs;

namespace LiveKicks.Backend.Services;

public interface IFootballApiService
{
    Task<ApiResponse<FixtureDto>?> GetFixturesTodayAsync();
    Task<ApiResponse<FixtureDto>?> GetLiveFixturesAsync();
    Task<ApiResponse<FixtureDto>?> GetFixtureByIdAsync(int fixtureId);
    Task<ApiResponse<StatisticsDto>?> GetStatisticsAsync(int fixtureId);
    Task<ApiResponse<EventDto>?> GetEventsAsync(int fixtureId);
    Task<ApiResponse<StandingsDto>?> GetStandingsAsync(int leagueId, int season);
    Task<ApiResponse<FixtureDto>?> GetHeadToHeadAsync(int team1, int team2);
    Task<ApiResponse<OddsDto>?> GetOddsAsync(int fixtureId);
}
