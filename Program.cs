using LiveKicks.Backend.Services;
using LiveKicks.Backend.Services.AI;
using LiveKicks.Backend.Services.AI.Engine;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add memory cache for API response caching
builder.Services.AddMemoryCache();


// Configure HttpClient for FootballApiService with typed client pattern
builder.Services.AddHttpClient<FootballApiService>(client =>
{
    var baseUrl = builder.Configuration["FootballApi:BaseUrl"]
                  ?? "https://v3.football.api-sports.io";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});


// Register IFootballApiService interface
builder.Services.AddScoped<IFootballApiService>(sp =>
    sp.GetRequiredService<FootballApiService>());


// ================================
// Phase 2B.5 AI Services
// ================================

builder.Services.AddScoped<AIContextBuilder>();


// ================================
// Phase 2C Elite AI Engine Services
// ================================

builder.Services.AddScoped<PredictionFeatureBuilder>();
builder.Services.AddScoped<EliteConfidenceCalculator>();
builder.Services.AddScoped<RiskAssessmentService>();
builder.Services.AddScoped<EliteTeamRatingCalculator>();
builder.Services.AddScoped<PredictionRankingService>();
builder.Services.AddScoped<ElitePredictionEngine>();


// ================================
// AI Orchestration Layer
// ================================

builder.Services.AddScoped<AIContextService>();
builder.Services.AddScoped<AIPredictionOrchestrator>();


// ================================
// CORS for MAUI App
// ================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("MauiApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();


// ================================
// Swagger
// Enabled for Render production testing
// ================================

app.UseSwagger();

app.UseSwaggerUI();


// Render handles HTTPS termination
// Do not force HTTPS redirect here


app.UseCors("MauiApp");

app.UseAuthorization();

app.MapControllers();

app.Run();