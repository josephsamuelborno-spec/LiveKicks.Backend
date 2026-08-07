using LiveKicks.Backend.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LiveKicks.Backend.Services;

public class FootballApiService : IFootballApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FootballApiService> _logger;
    private readonly int _cacheDurationMinutes;

    public FootballApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<FootballApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
        _cacheDurationMinutes = _configuration.GetValue<int>("FootballApi:CacheDurationMinutes", 5);
    }

    private async Task<ApiResponse<T>?> GetFromApiAsync<T>(string endpoint, string cacheKey)
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(cacheKey, out ApiResponse<T>? cachedResult))
            {
                _logger.LogInformation("? Cache hit for {CacheKey}", cacheKey);
                return cachedResult;
            }

            // Get and validate API key
            var apiKey = _configuration["FootballApi:ApiKey"];
            var apiKeyExists = !string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_API_KEY_HERE";

            if (!apiKeyExists)
            {
                _logger.LogError("? API key not configured or invalid. Check FootballApi__ApiKey environment variable.");
                return null;
            }

            // Log masked API key (first 4 characters only)
            var maskedKey = apiKey!.Length >= 4 ? $"{apiKey.Substring(0, 4)}****" : "****";
            _logger.LogInformation("?? API Key exists: Yes, Starting with: {MaskedKey}", maskedKey);

            // Verify BaseAddress
            if (_httpClient.BaseAddress == null)
            {
                _logger.LogError("? HttpClient BaseAddress is null. Check Program.cs configuration.");
                return null;
            }

            // Build full URL for logging (BaseAddress + relative endpoint)
            var fullUrl = new Uri(_httpClient.BaseAddress, endpoint).ToString();

            // Log request details BEFORE making the call
            _logger.LogInformation("?? Calling API-FOOTBALL:");
            _logger.LogInformation("   ? Full URL: {FullUrl}", fullUrl);
            _logger.LogInformation("   ? Endpoint: {Endpoint}", endpoint);
            _logger.LogInformation("   ? BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
            _logger.LogInformation("   ? Method: GET");
            _logger.LogInformation("   ? Header: x-apisports-key = {MaskedKey}", maskedKey);

            // Create request with relative endpoint
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("x-apisports-key", apiKey);

            // Make the API call
            _logger.LogInformation("? Sending request to API-FOOTBALL...");
            var response = await _httpClient.SendAsync(request);

            // Log response status immediately
            _logger.LogInformation("?? Response received:");
            _logger.LogInformation("   ? Status Code: {StatusCode} ({StatusCodeNumber})", 
                response.StatusCode, (int)response.StatusCode);
            _logger.LogInformation("   ? Success: {IsSuccess}", response.IsSuccessStatusCode);

            // Handle non-success responses with detailed logging
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                var errorLength = errorBody?.Length ?? 0;

                _logger.LogError("? API-FOOTBALL returned error:");
                _logger.LogError("   ? Status Code: {StatusCode} ({StatusCodeNumber})", 
                    response.StatusCode, (int)response.StatusCode);
                _logger.LogError("   ? URL: {FullUrl}", fullUrl);
                _logger.LogError("   ? Response Body Length: {Length} characters", errorLength);
                _logger.LogError("   ? Response Body: {ErrorBody}", 
                    string.IsNullOrEmpty(errorBody) ? "(empty)" : errorBody);
                _logger.LogError("   ? Endpoint: {Endpoint}", endpoint);

                // Additional helpful information
                if ((int)response.StatusCode == 401)
                {
                    _logger.LogError("?? 401 Unauthorized - Check API key validity");
                }
                else if ((int)response.StatusCode == 429)
                {
                    _logger.LogError("?? 429 Rate Limit - API quota exceeded");
                }
                else if ((int)response.StatusCode == 404)
                {
                    _logger.LogError("?? 404 Not Found - Check endpoint URL");
                }

                return null;
            }

            // Success: read and log response
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("? API-FOOTBALL response received successfully:");
            _logger.LogInformation("   ? Response Length: {Length} characters", content.Length);
            _logger.LogInformation("   ? Deserializing JSON...");

            var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result != null)
            {
                _logger.LogInformation("? JSON deserialization successful");
                _logger.LogInformation("   ? Results count: {Count}", result.Results);

                // Cache the result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheDurationMinutes)
                };
                _cache.Set(cacheKey, result, cacheOptions);
                _logger.LogInformation("?? Cached result for {CacheKey} (expires in {Minutes} minutes)", 
                    cacheKey, _cacheDurationMinutes);
            }
            else
            {
                _logger.LogWarning("?? JSON deserialization returned null");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "? HTTP error calling API-FOOTBALL:");
            _logger.LogError("   ? Endpoint: {Endpoint}", endpoint);
            _logger.LogError("   ? Message: {Message}", ex.Message);
            _logger.LogError("   ? Inner Exception: {InnerException}", ex.InnerException?.Message ?? "None");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "? JSON deserialization error:");
            _logger.LogError("   ? Endpoint: {Endpoint}", endpoint);
            _logger.LogError("   ? Message: {Message}", ex.Message);
            _logger.LogError("   ? Path: {Path}", ex.Path ?? "N/A");
            _logger.LogError("   ? Line Number: {LineNumber}", ex.LineNumber);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Unexpected error calling API-FOOTBALL:");
            _logger.LogError("   ? Endpoint: {Endpoint}", endpoint);
            _logger.LogError("   ? Exception Type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("   ? Message: {Message}", ex.Message);
            _logger.LogError("   ? Stack Trace: {StackTrace}", ex.StackTrace);
            return null;
        }
    }

    public async Task<ApiResponse<FixtureDto>?> GetFixturesTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endpoint = $"/fixtures?date={today}";
        var cacheKey = $"fixtures_today_{today}";
        return await GetFromApiAsync<FixtureDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<FixtureDto>?> GetLiveFixturesAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = "/fixtures?live=all";
        var cacheKey = "fixtures_live";
        // Live fixtures have shorter cache (1 minute max)
        var cached = _cache.Get<ApiResponse<FixtureDto>>(cacheKey);
        if (cached != null)
            return cached;

        var result = await GetFromApiAsync<FixtureDto>(endpoint, cacheKey);
        if (result != null)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(1));
        }
        return result;
    }

    // Backward compatibility overload for older AI services
    public async Task<ApiResponse<FixtureDto>?> GetLiveFixturesAsync(string status)
    {
        var endpoint = string.IsNullOrEmpty(status) || status == "all" 
            ? "/fixtures?live=all" 
            : $"/fixtures?live={status}";
        var cacheKey = $"fixtures_live_{status}";
        // Live fixtures have shorter cache (1 minute max)
        var cached = _cache.Get<ApiResponse<FixtureDto>>(cacheKey);
        if (cached != null)
            return cached;

        var result = await GetFromApiAsync<FixtureDto>(endpoint, cacheKey);
        if (result != null)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(1));
        }
        return result;
    }

    public async Task<ApiResponse<FixtureDto>?> GetFixtureByIdAsync(int fixtureId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/fixtures?id={fixtureId}";
        var cacheKey = $"fixture_{fixtureId}";
        return await GetFromApiAsync<FixtureDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<StatisticsDto>?> GetStatisticsAsync(int fixtureId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/fixtures/statistics?fixture={fixtureId}";
        var cacheKey = $"statistics_{fixtureId}";
        return await GetFromApiAsync<StatisticsDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<EventDto>?> GetEventsAsync(int fixtureId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/fixtures/events?fixture={fixtureId}";
        var cacheKey = $"events_{fixtureId}";
        return await GetFromApiAsync<EventDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<StandingsDto>?> GetStandingsAsync(int leagueId, int season, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/standings?league={leagueId}&season={season}";
        var cacheKey = $"standings_{leagueId}_{season}";
        return await GetFromApiAsync<StandingsDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<FixtureDto>?> GetHeadToHeadAsync(int team1, int team2, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/fixtures/headtohead?h2h={team1}-{team2}";
        var cacheKey = $"h2h_{team1}_{team2}";
        return await GetFromApiAsync<FixtureDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<OddsDto>?> GetOddsAsync(int fixtureId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/odds?fixture={fixtureId}";
        var cacheKey = $"odds_{fixtureId}";
        return await GetFromApiAsync<OddsDto>(endpoint, cacheKey);
    }

public async Task<ApiResponse<FixtureDto>?> GetFixturesTomorrowAsync(
    CancellationToken cancellationToken = default)
{
    var tomorrow = DateTime.UtcNow
        .AddDays(1)
        .ToString("yyyy-MM-dd");

    var endpoint = $"/fixtures?date={tomorrow}";

    var cacheKey = $"fixtures_tomorrow_{tomorrow}";

    _logger.LogInformation(
        "Fetching tomorrow fixtures: {Date}",
        tomorrow);

    return await GetFromApiAsync<FixtureDto>(
        endpoint,
        cacheKey);
}



public async Task<ApiResponse<FixtureDto>?> GetTeamHistoryAsync(
    int teamId,
    CancellationToken cancellationToken = default)
{
    var fromDate = DateTime.UtcNow
        .AddYears(-1)
        .ToString("yyyy-MM-dd");

    var toDate = DateTime.UtcNow
        .ToString("yyyy-MM-dd");


    var endpoint =
        $"/fixtures?team={teamId}&from={fromDate}&to={toDate}";


    var cacheKey =
        $"team_history_{teamId}";


    _logger.LogInformation(
        "Fetching team history: {TeamId}",
        teamId);


    return await GetFromApiAsync<FixtureDto>(
        endpoint,
        cacheKey);
}

}
