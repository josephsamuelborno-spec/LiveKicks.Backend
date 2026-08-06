using LiveKicks.Backend.Models.AI;
using LiveKicks.Backend.Models.DTOs;
using LiveKicks.Backend.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LiveKicks.Backend.Services.AI;

/// <summary>
/// Builds complete AI context by aggregating football data
/// Phase 2B.5 - Intelligence Data Pipeline
/// </summary>
public class AIContextBuilder
{
    private readonly IFootballApiService _footballApi;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AIContextBuilder> _logger;

    // Cache keys
    private const string CACHE_PREFIX_CONTEXT = "AI_CONTEXT_";
    private const string CACHE_PREFIX_TEAM = "TEAM_PROFILE_";
    private const string CACHE_PREFIX_LEAGUE = "LEAGUE_PROFILE_";

    // Cache durations
    private static readonly TimeSpan CONTEXT_CACHE_DURATION = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TEAM_CACHE_DURATION = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LEAGUE_CACHE_DURATION = TimeSpan.FromHours(6);
    private static readonly TimeSpan ODDS_CACHE_DURATION = TimeSpan.FromMinutes(5);

    public AIContextBuilder(
        IFootballApiService footballApi,
        IMemoryCache cache,
        ILogger<AIContextBuilder> logger)
    {
        _footballApi = footballApi;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Build complete AI context for a fixture
    /// </summary>
    public async Task<AIContextResponse> BuildContextAsync(int fixtureId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("?? Building AI context for fixture {FixtureId}", fixtureId);

        // Check cache first
        var cacheKey = $"{CACHE_PREFIX_CONTEXT}{fixtureId}";
        if (_cache.TryGetValue<AIContextResponse>(cacheKey, out var cachedContext))
        {
            _logger.LogInformation("? Returning cached AI context for fixture {FixtureId}", fixtureId);
            cachedContext!.FromCache = true;
            return cachedContext;
        }

        var context = new AIContextResponse();
        var dataQuality = new DataQualityInfo { OverallScore = 100.0 };

        try
        {
            // 1. Get fixture details
            _logger.LogDebug("  ? Fetching fixture details...");
            var fixtureResponse = await _footballApi.GetFixtureByIdAsync(fixtureId);

            if (fixtureResponse?.Response == null || fixtureResponse.Response.Count == 0)
            {
                _logger.LogWarning("? Fixture {FixtureId} not found", fixtureId);
                dataQuality.OverallScore = 0;
                dataQuality.Reliability = "LOW";
                dataQuality.MissingData.Add("Fixture not found");
                context.DataQuality = dataQuality;
                return context;
            }

            var fixture = fixtureResponse.Response[0];
            context.Fixture = MapFixtureInfo(fixture);
            _logger.LogDebug("  ? Fixture: {Home} vs {Away}", context.Fixture.HomeTeamName, context.Fixture.AwayTeamName);

            // 2. Build home team profile
            _logger.LogDebug("  ? Building home team profile...");
            context.HomeTeam = await BuildTeamProfileAsync(
                context.Fixture.HomeTeamId,
                context.Fixture.HomeTeamName,
                context.Fixture.LeagueId,
                isHome: true,
                dataQuality,
                cancellationToken);

            // 3. Build away team profile
            _logger.LogDebug("  ? Building away team profile...");
            context.AwayTeam = await BuildTeamProfileAsync(
                context.Fixture.AwayTeamId,
                context.Fixture.AwayTeamName,
                context.Fixture.LeagueId,
                isHome: false,
                dataQuality,
                cancellationToken);

            // 4. Get head-to-head
            _logger.LogDebug("  ? Fetching head-to-head...");
            context.HeadToHead = await BuildHeadToHeadAsync(
                context.Fixture.HomeTeamId,
                context.Fixture.AwayTeamId,
                dataQuality,
                cancellationToken);

            // 5. Get league profile
            _logger.LogDebug("  ? Building league profile...");
            context.LeagueProfile = await BuildLeagueProfileAsync(
                context.Fixture.LeagueId,
                context.Fixture.LeagueName,
                dataQuality,
                cancellationToken);

            // 6. Get market odds
            _logger.LogDebug("  ? Fetching market odds...");
            context.MarketOdds = await BuildMarketOddsAsync(
                fixtureId,
                dataQuality,
                cancellationToken);

            // 7. Calculate final data quality
            CalculateFinalDataQuality(dataQuality);
            context.DataQuality = dataQuality;

            // Cache the result (only if quality is acceptable)
            if (dataQuality.OverallScore >= 50)
            {
                _cache.Set(cacheKey, context, CONTEXT_CACHE_DURATION);
                _logger.LogDebug("  ?? Cached AI context for fixture {FixtureId}", fixtureId);
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "? AI context built for fixture {FixtureId} in {Duration}ms - Quality: {Quality}% ({Reliability})",
                fixtureId,
                duration,
                dataQuality.OverallScore,
                dataQuality.Reliability);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error building AI context for fixture {FixtureId}", fixtureId);
            dataQuality.OverallScore = 0;
            dataQuality.Reliability = "LOW";
            dataQuality.MissingData.Add($"System error: {ex.Message}");
            context.DataQuality = dataQuality;
            return context;
        }
    }

    /// <summary>
    /// Build team AI profile
    /// </summary>
    private async Task<TeamAIProfile> BuildTeamProfileAsync(
        int teamId,
        string teamName,
        int leagueId,
        bool isHome,
        DataQualityInfo dataQuality,
        CancellationToken cancellationToken)
    {
        var profile = new TeamAIProfile
        {
            TeamId = teamId,
            TeamName = teamName
        };

        try
        {
            // Get league standings
            var standingsResponse = await _footballApi.GetStandingsAsync(leagueId, 2024);

            if (standingsResponse?.Response != null && standingsResponse.Response.Count > 0)
            {
                var teamStanding = FindTeamInStandings(standingsResponse, teamId);

                if (teamStanding != null)
                {
                    profile.LeaguePosition = teamStanding.Rank;
                    profile.Points = teamStanding.Points;
                    profile.MatchesPlayed = teamStanding.All.Played;
                    profile.GoalDifference = teamStanding.GoalsDiff;

                    // Calculate basic stats from standings
                    var all = teamStanding.All;
                    profile.Last10Matches = new FormStats
                    {
                        Wins = all.Win,
                        Draws = all.Draw,
                        Losses = all.Lose,
                        GoalsScored = all.Goals?.For ?? 0,
                        GoalsConceded = all.Goals?.Against ?? 0,
                        MatchCount = all.Played,
                        PointsPerGame = all.Played > 0 ? (double)teamStanding.Points / all.Played : 0,
                        GoalsPerGame = all.Played > 0 ? (double)(all.Goals?.For ?? 0) / all.Played : 0,
                        GoalsConcededPerGame = all.Played > 0 ? (double)(all.Goals?.Against ?? 0) / all.Played : 0
                    };

                    profile.AvgGoalsScored = profile.Last10Matches.GoalsPerGame;
                    profile.AvgGoalsConceded = profile.Last10Matches.GoalsConcededPerGame;

                    dataQuality.HasStandings = true;
                    _logger.LogDebug("    ? {Team}: Position {Pos}, Points {Points}", teamName, profile.LeaguePosition, profile.Points);
                }
                else
                {
                    _logger.LogWarning("    ? Team {TeamId} not found in standings", teamId);
                    dataQuality.MissingData.Add($"{teamName} not in standings");
                    dataQuality.OverallScore -= 10;
                }
            }
            else
            {
                _logger.LogWarning("    ? Standings unavailable for league {LeagueId}", leagueId);
                dataQuality.MissingData.Add($"{teamName} standings unavailable");
                dataQuality.OverallScore -= 15;
            }

            // Note: Recent match history requires additional backend endpoints
            // For now, we use standings data and mark as incomplete
            if (!dataQuality.HasRecentMatches)
            {
                dataQuality.MissingData.Add($"{teamName} recent match history (endpoint needed)");
                dataQuality.OverallScore -= 20;
            }

            // Calculate form trend from current stats
            profile.FormTrend = CalculateFormTrend(profile);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "    ? Error building profile for team {TeamId}", teamId);
            dataQuality.OverallScore -= 25;
            dataQuality.MissingData.Add($"{teamName} profile error");
        }

        return profile;
    }

    /// <summary>
    /// Build head-to-head statistics
    /// </summary>
    private async Task<HeadToHeadInfo> BuildHeadToHeadAsync(
        int team1Id,
        int team2Id,
        DataQualityInfo dataQuality,
        CancellationToken cancellationToken)
    {
        var h2h = new HeadToHeadInfo();

        try
        {
            var h2hResponse = await _footballApi.GetHeadToHeadAsync(team1Id, team2Id);

            if (h2hResponse?.Response != null && h2hResponse.Results > 0)
            {
                h2h.TotalMeetings = Math.Min(h2hResponse.Results, 10);

                int totalGoals = 0;
                int bttsCount = 0;
                int over25Count = 0;
                var results = new List<string>();

                foreach (var fixture in h2hResponse.Response.Take(10))
                {
                    bool team1Home = fixture.Teams.Home.Id == team1Id;
                    int? t1Goals = team1Home ? fixture.Goals.Home : fixture.Goals.Away;
                    int? t2Goals = team1Home ? fixture.Goals.Away : fixture.Goals.Home;

                    if (!t1Goals.HasValue || !t2Goals.HasValue) continue;

                    // Results
                    if (t1Goals > t2Goals)
                    {
                        h2h.HomeWins++;
                        results.Add("H");
                    }
                    else if (t1Goals == t2Goals)
                    {
                        h2h.Draws++;
                        results.Add("D");
                    }
                    else
                    {
                        h2h.AwayWins++;
                        results.Add("A");
                    }

                    // Goals
                    totalGoals += t1Goals.Value + t2Goals.Value;
                    if (t1Goals > 0 && t2Goals > 0) bttsCount++;
                    if (t1Goals + t2Goals > 2) over25Count++;
                }

                // Averages
                if (h2h.TotalMeetings > 0)
                {
                    h2h.AvgTotalGoals = (double)totalGoals / h2h.TotalMeetings;
                    h2h.BTTSPercentage = (double)bttsCount / h2h.TotalMeetings * 100;
                    h2h.Over25Percentage = (double)over25Count / h2h.TotalMeetings * 100;
                }

                h2h.Last5Results = results.Take(5).ToList();
                h2h.RecentTrend = h2h.HomeWins > h2h.AwayWins * 2 ? "Home dominance" :
                                  h2h.AwayWins > h2h.HomeWins * 2 ? "Away dominance" : "Balanced";

                dataQuality.HasHeadToHead = true;
                _logger.LogDebug("    ? H2H: {Meetings} meetings, {Trend}", h2h.TotalMeetings, h2h.RecentTrend);
            }
            else
            {
                _logger.LogWarning("    ? H2H data unavailable");
                dataQuality.MissingData.Add("Head-to-head unavailable");
                dataQuality.OverallScore -= 10;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "    ? Error building H2H");
            dataQuality.OverallScore -= 10;
        }

        return h2h;
    }

    /// <summary>
    /// Build league profile
    /// </summary>
    private async Task<LeagueAIProfile> BuildLeagueProfileAsync(
        int leagueId,
        string leagueName,
        DataQualityInfo dataQuality,
        CancellationToken cancellationToken)
    {
        // Check cache
        var cacheKey = $"{CACHE_PREFIX_LEAGUE}{leagueId}";
        if (_cache.TryGetValue<LeagueAIProfile>(cacheKey, out var cachedProfile))
        {
            dataQuality.HasLeagueProfile = true;
            return cachedProfile!;
        }

        var profile = new LeagueAIProfile
        {
            LeagueId = leagueId,
            LeagueName = leagueName,
            ProfileReliability = 50.0 // Basic profile without full match history
        };

        try
        {
            // TODO: Build comprehensive league profile from match history
            // For now, return basic profile
            dataQuality.HasLeagueProfile = true;
            dataQuality.MissingData.Add("Full league analysis (match history needed)");
            dataQuality.OverallScore -= 5;

            // Cache the basic profile
            _cache.Set(cacheKey, profile, LEAGUE_CACHE_DURATION);
            _logger.LogDebug("    ? League profile created (basic)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "    ? Error building league profile");
            dataQuality.OverallScore -= 10;
        }

        return profile;
    }

    /// <summary>
    /// Build market odds
    /// </summary>
    private async Task<MarketOddsInfo> BuildMarketOddsAsync(
        int fixtureId,
        DataQualityInfo dataQuality,
        CancellationToken cancellationToken)
    {
        var odds = new MarketOddsInfo();

        try
        {
            var oddsResponse = await _footballApi.GetOddsAsync(fixtureId);

            if (oddsResponse?.Response != null && oddsResponse.Results > 0)
            {
                var bookmaker = oddsResponse.Response.FirstOrDefault()?.Bookmakers?.FirstOrDefault();

                if (bookmaker != null)
                {
                    // Match Winner odds
                    var matchWinner = bookmaker.Bets.FirstOrDefault(b => b.Name == "Match Winner");
                    if (matchWinner != null)
                    {
                        odds.HomeWinOdds = ParseOdd(matchWinner.Values.FirstOrDefault(v => v.Value == "Home")?.Odd);
                        odds.DrawOdds = ParseOdd(matchWinner.Values.FirstOrDefault(v => v.Value == "Draw")?.Odd);
                        odds.AwayWinOdds = ParseOdd(matchWinner.Values.FirstOrDefault(v => v.Value == "Away")?.Odd);
                    }

                    // Over/Under 2.5
                    var ou = bookmaker.Bets.FirstOrDefault(b => b.Name.Contains("Over/Under") && b.Name.Contains("2.5"));
                    if (ou != null)
                    {
                        odds.Over25Odds = ParseOdd(ou.Values.FirstOrDefault(v => v.Value.Contains("Over"))?.Odd);
                        odds.Under25Odds = ParseOdd(ou.Values.FirstOrDefault(v => v.Value.Contains("Under"))?.Odd);
                    }

                    // BTTS
                    var btts = bookmaker.Bets.FirstOrDefault(b => b.Name.Contains("Both Teams Score") || b.Name.Contains("BTTS"));
                    if (btts != null)
                    {
                        odds.BTTSYesOdds = ParseOdd(btts.Values.FirstOrDefault(v => v.Value == "Yes")?.Odd);
                    }

                    odds.OddsAvailable = true;
                    dataQuality.HasOdds = true;
                    _logger.LogDebug("    ? Odds: {Home}/{Draw}/{Away}", odds.HomeWinOdds, odds.DrawOdds, odds.AwayWinOdds);
                }
            }
            else
            {
                _logger.LogWarning("    ? Odds unavailable");
                dataQuality.MissingData.Add("Market odds unavailable");
                dataQuality.OverallScore -= 5;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "    ? Error fetching odds");
            dataQuality.OverallScore -= 5;
        }

        return odds;
    }

    #region Helper Methods

    private FixtureInfo MapFixtureInfo(FixtureDto fixture)
    {
        return new FixtureInfo
        {
            FixtureId = fixture.Fixture.Id,
            Date = fixture.Fixture.Date,
            HomeTeamName = fixture.Teams.Home.Name,
            AwayTeamName = fixture.Teams.Away.Name,
            HomeTeamId = fixture.Teams.Home.Id,
            AwayTeamId = fixture.Teams.Away.Id,
            LeagueName = fixture.League.Name,
            LeagueId = fixture.League.Id,
            Status = fixture.Fixture.Status.Short,
            Venue = fixture.Fixture.Venue?.Name ?? "Unknown"
        };
    }

    private dynamic? FindTeamInStandings(ApiResponse<StandingsDto>? standings, int teamId)
    {
        if (standings?.Response == null || standings.Response.Count == 0)
            return null;

        foreach (var standing in standings.Response)
        {
            foreach (var group in standing.League.Standings)
            {
                foreach (var team in group)
                {
                    if (team.Team.Id == teamId)
                        return team;
                }
            }
        }
        return null;
    }

    private string CalculateFormTrend(TeamAIProfile profile)
    {
        var ppg = profile.Last10Matches.PointsPerGame;

        if (ppg >= 2.5) return "EXCELLENT";
        if (ppg >= 2.0) return "IMPROVING";
        if (ppg >= 1.5) return "STABLE";
        if (ppg >= 1.0) return "DECLINING";
        return "POOR";
    }

    private double ParseOdd(string? oddStr)
    {
        return double.TryParse(oddStr, out var odd) ? odd : 0;
    }

    private void CalculateFinalDataQuality(DataQualityInfo dataQuality)
    {
        // Determine reliability tier
        if (dataQuality.OverallScore >= 80)
            dataQuality.Reliability = "HIGH";
        else if (dataQuality.OverallScore >= 60)
            dataQuality.Reliability = "MEDIUM";
        else
            dataQuality.Reliability = "LOW";
    }

    #endregion
}
