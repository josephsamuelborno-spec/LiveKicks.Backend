using LiveKicks.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add memory cache for API response caching
builder.Services.AddMemoryCache();

// Configure HttpClient for FootballApiService with typed client pattern
// This automatically registers FootballApiService as scoped with the configured HttpClient
builder.Services.AddHttpClient<FootballApiService>(client =>
{
    var baseUrl = builder.Configuration["FootballApi:BaseUrl"] ?? "https://v3.football.api-sports.io";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
// Note: AddHttpClient<T> automatically registers T as scoped, so no need for AddScoped<FootballApiService>()

// Add CORS policy for MAUI app
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("MauiApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
