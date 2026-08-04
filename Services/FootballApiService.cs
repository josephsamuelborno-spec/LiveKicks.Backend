using LiveKicks.Backend.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LiveKicks.Backend.Services;

public class FootballApiService
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
                _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
                return cachedResult;
            }

            // Get API key from configuration
            var apiKey = _configuration["FootballApi:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
            {
                _logger.LogError("API key not configured");
                return null;
            }

            // Create request with relative endpoint (BaseAddress is set in Program.cs)
            // HttpClient will automatically combine BaseAddress + endpoint
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("x-apisports-key", apiKey);

            // Log the full URL for debugging
            var baseUrl = _httpClient.BaseAddress?.ToString() ?? "No BaseAddress";
            var fullUrl = $"{baseUrl.TrimEnd('/')}{endpoint}";
            _logger.LogInformation("Calling API-FOOTBALL: {FullUrl} with API key: {ApiKeyMasked}", 
                fullUrl, 
                apiKey.Length > 8 ? $"{apiKey.Substring(0, 4)}...{apiKey.Substring(apiKey.Length - 4)}" : "***");

            var response = await _httpClient.SendAsync(request);

            // Improved error handling with detailed logging
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "API-FOOTBALL returned {StatusCode}: {Error}. Request URL: {FullUrl}",
                    response.StatusCode,
                    error,
                    fullUrl
                );
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("API-FOOTBALL response received successfully. Length: {Length} characters", content.Length);

            var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result != null)
            {
                // Cache the result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheDurationMinutes)
                };
                _cache.Set(cacheKey, result, cacheOptions);
                _logger.LogInformation("Cached result for {CacheKey}", cacheKey);
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling API-FOOTBALL: {Endpoint}", endpoint);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for endpoint: {Endpoint}", endpoint);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling API-FOOTBALL: {Endpoint}", endpoint);
            return null;
        }
    }

    public async Task<ApiResponse<FixtureDto>?> GetFixturesTodayAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endpoint = $"/fixtures?date={today}";
        var cacheKey = $"fixtures_today_{today}";
        return await GetFromApiAsync<FixtureDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<FixtureDto>?> GetLiveFixturesAsync()
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

    public async Task<ApiResponse<FixtureDto>?> GetFixtureByIdAsync(int fixtureId)
    {
        var endpoint = $"/fixtures?id={fixtureId}";
        var cacheKey = $"fixture_{fixtureId}";
        return await GetFromApiAsync<FixtureDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<StatisticsDto>?> GetStatisticsAsync(int fixtureId)
    {
        var endpoint = $"/fixtures/statistics?fixture={fixtureId}";
        var cacheKey = $"statistics_{fixtureId}";
        return await GetFromApiAsync<StatisticsDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<EventDto>?> GetEventsAsync(int fixtureId)
    {
        var endpoint = $"/fixtures/events?fixture={fixtureId}";
        var cacheKey = $"events_{fixtureId}";
        return await GetFromApiAsync<EventDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<StandingsDto>?> GetStandingsAsync(int leagueId, int season)
    {
        var endpoint = $"/standings?league={leagueId}&season={season}";
        var cacheKey = $"standings_{leagueId}_{season}";
        return await GetFromApiAsync<StandingsDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<FixtureDto>?> GetHeadToHeadAsync(int team1, int team2)
    {
        var endpoint = $"/fixtures/headtohead?h2h={team1}-{team2}";
        var cacheKey = $"h2h_{team1}_{team2}";
        return await GetFromApiAsync<FixtureDto>(endpoint, cacheKey);
    }

    public async Task<ApiResponse<OddsDto>?> GetOddsAsync(int fixtureId)
    {
        var endpoint = $"/odds?fixture={fixtureId}";
        var cacheKey = $"odds_{fixtureId}";
        return await GetFromApiAsync<OddsDto>(endpoint, cacheKey);
    }
}
