# LiveKicks Backend API

ASP.NET Core Web API backend for LiveKicks MAUI app, connecting to API-FOOTBALL.

## ?? Purpose

Securely proxy API-FOOTBALL requests from the LiveKicks MAUI app without exposing the API key.

## ??? Architecture

```
LiveKicks .NET MAUI App
          ?
LiveKicks Backend (ASP.NET Core Web API)
          ?
API-FOOTBALL
```

## ?? Security

- **API Key**: Stored ONLY in Render environment variables
- **Never** commit the API key to GitHub
- **Never** include the API key in the MAUI app

## ?? Tech Stack

- .NET 8
- ASP.NET Core Web API
- Memory Cache (in-memory caching)
- HttpClient with Polly (timeout handling)
- Swagger/OpenAPI

## ?? Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/football/fixtures/today` | GET | Get all fixtures for today |
| `/api/football/live` | GET | Get all live fixtures |
| `/api/football/fixture/{id}` | GET | Get fixture details by ID |
| `/api/football/statistics/{fixtureId}` | GET | Get match statistics |
| `/api/football/events/{fixtureId}` | GET | Get match events |
| `/api/football/standings/{leagueId}` | GET | Get league standings |
| `/api/football/headtohead/{team1}/{team2}` | GET | Get head-to-head matches |
| `/api/football/odds/{fixtureId}` | GET | Get odds for fixture |
| `/api/football/health` | GET | Health check |

## ?? Configuration

### Local Development

Update `appsettings.Development.json`:

```json
{
  "FootballApi": {
    "BaseUrl": "https://v3.football.api-sports.io",
    "ApiKey": "YOUR_API_KEY_HERE",
    "CacheDurationMinutes": 1
  }
}
```

### Render Environment Variables

Set these in the Render dashboard:

```
FootballApi__BaseUrl=https://v3.football.api-sports.io
FootballApi__ApiKey=YOUR_ACTUAL_API_KEY
FootballApi__CacheDurationMinutes=5
```

## ?? Local Development

1. **Set API Key**

```bash
cd LiveKicks.Backend
dotnet user-secrets set "FootballApi:ApiKey" "YOUR_API_KEY"
```

2. **Run the API**

```bash
dotnet run
```

3. **Test Endpoints**

Open browser: `https://localhost:5001/swagger`

Or use curl:

```bash
curl https://localhost:5001/api/football/fixtures/today
curl https://localhost:5001/api/football/live
curl https://localhost:5001/api/football/health
```

## ?? Render Deployment

### Option 1: Blueprint (render.yaml)

1. Push code to GitHub
2. Create new Web Service on Render
3. Connect your GitHub repository
4. Render will auto-detect `render.yaml`
5. Set the `FootballApi__ApiKey` environment variable
6. Deploy!

### Option 2: Manual

1. Create new Web Service on Render
2. Select "Docker" as environment
3. Set:
   - **Dockerfile Path**: `LiveKicks.Backend/Dockerfile`
   - **Docker Context**: `LiveKicks.Backend`
4. Add environment variables:
   - `FootballApi__BaseUrl`: `https://v3.football.api-sports.io`
   - `FootballApi__ApiKey`: `YOUR_API_KEY`
   - `FootballApi__CacheDurationMinutes`: `5`
5. Set health check path: `/api/football/health`
6. Deploy!

## ?? Testing the Deployed Backend

Once deployed to Render:

```bash
curl https://your-app-name.onrender.com/api/football/health
curl https://your-app-name.onrender.com/api/football/fixtures/today
```

## ?? Caching Strategy

- **Today's Fixtures**: 5 minutes
- **Live Fixtures**: 1 minute
- **Statistics/Events**: 5 minutes
- **Standings**: 5 minutes
- **Head-to-Head**: 5 minutes

This reduces API calls and stays within the free tier limits.

## ?? MAUI Integration

After backend deployment, update the MAUI app to call:

```csharp
var baseUrl = "https://your-app-name.onrender.com";
var response = await httpClient.GetAsync($"{baseUrl}/api/football/fixtures/today");
```

## ?? API Rate Limits (Free Tier)

- **100 requests per day**
- **10 requests per minute**
- Caching helps minimize API calls

## ?? Troubleshooting

### "Failed to fetch fixtures"
- Check API key is set in Render environment variables
- Verify API key is valid on api-football.com
- Check logs in Render dashboard

### Timeout errors
- API-FOOTBALL may be slow or down
- Check status: https://status.api-football.com

### CORS errors from MAUI
- Ensure CORS policy allows all origins in `Program.cs`
- MAUI apps need unrestricted CORS

## ?? License

Part of LiveKicks project.
