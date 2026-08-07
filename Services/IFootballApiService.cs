using LiveKicks.Backend.Models.DTOs;

namespace LiveKicks.Backend.Services;

public interface IFootballApiService
{
    Task<ApiResponse<FixtureDto>?> GetFixturesTodayAsync(
        CancellationToken cancellationToken = default);


    Task<ApiResponse<FixtureDto>?> GetFixturesTomorrowAsync(
        CancellationToken cancellationToken = default);


    Task<ApiResponse<FixtureDto>?> GetLiveFixturesAsync(
        CancellationToken cancellationToken = default);


    Task<ApiResponse<FixtureDto>?> GetFixtureByIdAsync(
        int fixtureId,
        CancellationToken cancellationToken = default);


    Task<ApiResponse<FixtureDto>?> GetTeamHistoryAsync(
        int teamId,
        CancellationToken cancellationToken = default);


    Task<ApiResponse<StatisticsDto>?> GetStatisticsAsync(
        int fixtureId,
        CancellationToken cancellationToken = default);


    Task<ApiResponse<EventDto>?> GetEventsAsync(
        int fixtureId,
        CancellationToken cancellationToken = default);


    Task<ApiResponse<StandingsDto>?> GetStandingsAsync(
        int leagueId,
        int season,
        CancellationToken cancellationToken = default);


    Task<ApiResponse<FixtureDto>?> GetHeadToHeadAsync(
        int team1,
        int team2,
        CancellationToken cancellationToken = default);


    Task<ApiResponse<OddsDto>?> GetOddsAsync(
        int fixtureId,
        CancellationToken cancellationToken = default);
}