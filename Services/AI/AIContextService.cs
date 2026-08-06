using LiveKicks.Backend.Models.AI;
using LiveKicks.Backend.Services;

namespace LiveKicks.Backend.Services.AI;

/// <summary>
/// Service for managing AI context operations
/// Provides batch and individual context retrieval
/// </summary>
public class AIContextService
{
    private readonly AIContextBuilder _contextBuilder;
    private readonly IFootballApiService _footballApi;
    private readonly ILogger<AIContextService> _logger;

    public AIContextService(
        AIContextBuilder contextBuilder,
        IFootballApiService footballApi,
        ILogger<AIContextService> logger)
    {
        _contextBuilder = contextBuilder;
        _footballApi = footballApi;
        _logger = logger;
    }

    /// <summary>
    /// Get AI context for a specific fixture
    /// </summary>
    public async Task<AIContextResponse?> GetFixtureContextAsync(
        int fixtureId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _contextBuilder.BuildContextAsync(fixtureId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build context for fixture {FixtureId}", fixtureId);
            return null;
        }
    }

    /// <summary>
    /// Get AI contexts for all today's fixtures
    /// </summary>
    public async Task<List<AIContextResponse>> GetTodayFixtureContextsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("?? Fetching today's fixtures for AI context");

        var contexts = new List<AIContextResponse>();

        try
        {
            // Get today's fixtures
            var todayResponse = await _footballApi.GetFixturesTodayAsync(cancellationToken);

            if (todayResponse?.Response == null || todayResponse.Response.Count == 0)
            {
                _logger.LogWarning("No fixtures found for today");
                return contexts;
            }

            _logger.LogInformation("Found {Count} fixtures for today", todayResponse.Response.Count);

            // Build context for each fixture (with concurrency limit)
            var semaphore = new SemaphoreSlim(5); // Max 5 concurrent context builds
            var tasks = new List<Task>();

            foreach (var fixture in todayResponse.Response)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var context = await _contextBuilder.BuildContextAsync(fixture.Fixture.Id, cancellationToken);
                        if (context != null && context.DataQuality?.OverallScore > 50)
                        {
                            lock (contexts)
                            {
                                contexts.Add(context);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to build context for fixture {Id}", fixture.Fixture.Id);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);

            _logger.LogInformation("? Built {Count} AI contexts from {Total} fixtures",
                contexts.Count, todayResponse.Response.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get today's fixture contexts");
        }

        return contexts;
    }

    /// <summary>
    /// Get AI contexts for live fixtures
    /// </summary>
    public async Task<List<AIContextResponse>> GetLiveFixtureContextsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("? Fetching live fixtures for AI context");

        var contexts = new List<AIContextResponse>();

        try
        {
            var liveResponse = await _footballApi.GetLiveFixturesAsync(cancellationToken);

            if (liveResponse?.Response == null || liveResponse.Response.Count == 0)
            {
                _logger.LogWarning("No live fixtures found");
                return contexts;
            }

            _logger.LogInformation("Found {Count} live fixtures", liveResponse.Response.Count);

            var semaphore = new SemaphoreSlim(5);
            var tasks = new List<Task>();

            foreach (var fixture in liveResponse.Response)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var context = await _contextBuilder.BuildContextAsync(fixture.Fixture.Id, cancellationToken);
                        if (context != null && context.DataQuality?.OverallScore > 50)
                        {
                            lock (contexts)
                            {
                                contexts.Add(context);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to build context for live fixture {Id}", fixture.Fixture.Id);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);

            _logger.LogInformation("? Built {Count} AI contexts from {Total} live fixtures",
                contexts.Count, liveResponse.Response.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get live fixture contexts");
        }

        return contexts;
    }
}
