using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.PlantingAdvisory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KisanMitraAI.Infrastructure.PlantingAdvisory;

/// <summary>
/// Implementation of weather data collector with caching support
/// </summary>
public class WeatherDataCollector : IWeatherDataCollector
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherDataCollector> _logger;
    private readonly string _apiKey;
    private readonly string _apiBaseUrl;

    public WeatherDataCollector(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<WeatherDataCollector> logger,
        string apiKey,
        string apiBaseUrl = "https://api.openweathermap.org/data/2.5")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _apiBaseUrl = apiBaseUrl;
    }

    public async Task<WeatherForecast> GetForecastAsync(
        string location,
        int daysAhead,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location cannot be null or empty", nameof(location));

        if (daysAhead < 1 || daysAhead > 90)
            throw new ArgumentOutOfRangeException(nameof(daysAhead), "Days ahead must be between 1 and 90");

        var cacheKey = $"weather_{location}_{daysAhead}";

        // Check cache first (6 hour TTL as per requirements)
        if (_cache.TryGetValue<WeatherForecast>(cacheKey, out var cachedForecast))
        {
            _logger.LogInformation("Weather forecast retrieved from cache for {Location}", location);
            return cachedForecast!;
        }

        _logger.LogInformation("Fetching weather forecast for {Location} for {Days} days", location, daysAhead);

        try
        {
            // For MVP, we'll use OpenWeatherMap API
            // In production, this could be AWS Weather Service or other provider
            var url = $"{_apiBaseUrl}/forecast?q={Uri.EscapeDataString(location)}&appid={_apiKey}&units=metric&cnt={Math.Min(daysAhead, 16)}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            
            var weatherData = JsonSerializer.Deserialize<OpenWeatherMapResponse>(content, options);

            if (weatherData == null || weatherData.List == null || weatherData.List.Count == 0)
                throw new InvalidOperationException("Failed to parse weather data");

            var dailyForecasts = weatherData.List.Select(item => new DailyForecast(
                DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(item.Dt).DateTime),
                item.Main.TempMin,
                item.Main.TempMax,
                item.Rain?.ThreeHour ?? 0,
                item.Main.Humidity,
                CalculateSoilMoisture(item.Main.Humidity, item.Rain?.ThreeHour ?? 0)
            )).ToList();

            var forecast = new WeatherForecast(
                location,
                dailyForecasts,
                DateTimeOffset.UtcNow
            );

            // Cache for 6 hours
            _cache.Set(cacheKey, forecast, TimeSpan.FromHours(6));

            _logger.LogInformation("Weather forecast fetched and cached for {Location}", location);

            return forecast;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch weather data for {Location}", location);
            throw new InvalidOperationException($"Failed to retrieve weather data for {location}", ex);
        }
    }

    private static float CalculateSoilMoisture(float humidity, float rainfall)
    {
        // Simple estimation: soil moisture correlates with humidity and recent rainfall
        // This is a simplified model; production would use actual soil moisture sensors
        return (humidity * 0.6f + Math.Min(rainfall * 10, 40)) / 100f;
    }

    // DTOs for OpenWeatherMap API
    private class OpenWeatherMapResponse
    {
        [JsonPropertyName("list")]
        public List<WeatherItem>? List { get; set; }
    }

    private class WeatherItem
    {
        [JsonPropertyName("dt")]
        public long Dt { get; set; }
        
        [JsonPropertyName("main")]
        public MainData Main { get; set; } = new();
        
        [JsonPropertyName("rain")]
        public RainData? Rain { get; set; }
    }

    private class MainData
    {
        [JsonPropertyName("temp_min")]
        public float TempMin { get; set; }
        
        [JsonPropertyName("temp_max")]
        public float TempMax { get; set; }
        
        [JsonPropertyName("humidity")]
        public float Humidity { get; set; }
    }

    private class RainData
    {
        [JsonPropertyName("3h")]
        public float ThreeHour { get; set; }
    }
}
