using LiveKicks.Backend.Services;
using LiveKicks.Backend.Services.AI;
using LiveKicks.Backend.Services.AI.Engine;

var builder = WebApplication.CreateBuilder(args);

// =====================================
// Add services
// =====================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();


// =====================================
// Football-Data.org Active Provider
// =====================================

builder.Services.AddHttpClient("FootballData", client =>
{
    var baseUrl =
        builder.Configuration["FootballData:BaseUrl"]
        ?? "https://api.football-data.org/v4/";

    // IMPORTANT:
    // Keep trailing slash.
    // This prevents .NET URI merging from removing /v4
    client.BaseAddress = new Uri(baseUrl);

    client.Timeout = TimeSpan.FromSeconds(30);


    var token =
        builder.Configuration["FootballData:ApiToken"];


    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Remove("X-Auth-Token");

        client.DefaultRequestHeaders.Add(
            "X-Auth-Token",
            token);
    }
});


// =====================================
// Football Provider Switch
// API-FOOTBALL disabled
// Football-Data.org active
// =====================================

builder.Services.AddScoped<
    IFootballApiService,
    FootballDataService>();



// =====================================
// Phase 2B.5 AI Services
// =====================================

// Keep your existing AI registrations here
// Example:
// builder.Services.AddScoped<YourService>();



// =====================================
// Phase 2C Elite AI Engine Services
// =====================================

// Keep your existing AI Engine registrations here
// Example:
// builder.Services.AddScoped<YourService>();



// =====================================
// AI Orchestration Layer
// =====================================

// Keep your existing AI orchestration registrations here
// Example:
// builder.Services.AddScoped<YourService>();



// =====================================
// CORS for MAUI App
// =====================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "MauiApp",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});



var app = builder.Build();



// =====================================
// Swagger
// =====================================

app.UseSwagger();

app.UseSwaggerUI();



// Render handles HTTPS termination

app.UseCors("MauiApp");

app.UseAuthorization();

app.MapControllers();

app.Run();