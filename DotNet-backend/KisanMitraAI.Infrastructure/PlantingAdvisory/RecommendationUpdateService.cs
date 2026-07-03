using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.PlantingAdvisory;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.PlantingAdvisory;

/// <summary>
/// Service for updating planting recommendations when weather changes significantly
/// </summary>
public class RecommendationUpdateService : IRecommendationUpdateService
{
    private readonly IWeatherDataCollector _weatherCollector;
    private readonly ISoilDataRetriever _soilRetriever;
    private readonly IPlantingWindowAnalyzer _windowAnalyzer;
    private readonly ILogger<RecommendationUpdateService> _logger;
    private readonly Dictionary<string, WeatherForecast> _previousForecasts = new();
    private const float SignificantRainfallChangeThreshold = 0.20f; // 20% change

    public RecommendationUpdateService(
        IWeatherDataCollector weatherCollector,
        ISoilDataRetriever soilRetriever,
        IPlantingWindowAnalyzer windowAnalyzer,
        ILogger<RecommendationUpdateService> logger)
    {
        _weatherCollector = weatherCollector ?? throw new ArgumentNullException(nameof(weatherCollector));
        _soilRetriever = soilRetriever ?? throw new ArgumentNullException(nameof(soilRetriever));
        _windowAnalyzer = windowAnalyzer ?? throw new ArgumentNullException(nameof(windowAnalyzer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CheckAndUpdateRecommendationsAsync(
        string farmerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(farmerId))
            throw new ArgumentException("Farmer ID cannot be null or empty", nameof(farmerId));

        _logger.LogInformation("Checking for weather changes for farmer {FarmerId}", farmerId);

        try
        {
            // Get farmer's soil data to determine location
            var soilData = await _soilRetriever.GetLatestSoilDataAsync(farmerId, cancellationToken);
            if (soilData == null)
            {
                _logger.LogWarning("No soil data found for farmer {FarmerId}, skipping update", farmerId);
                return false;
            }

            // Get current weather forecast
            var currentForecast = await _weatherCollector.GetForecastAsync(
                soilData.Location, 90, cancellationToken);

            // Check if we have a previous forecast to compare
            var cacheKey = $"{farmerId}_{soilData.Location}";
            if (!_previousForecasts.TryGetValue(cacheKey, out var previousForecast))
            {
                // First time checking, store current forecast
                _previousForecasts[cacheKey] = currentForecast;
                _logger.LogInformation("Stored initial forecast for farmer {FarmerId}", farmerId);
                return false;
            }

            // Check for significant changes
            var hasSignificantChange = DetectSignificantWeatherChange(
                previousForecast, currentForecast);

            if (hasSignificantChange)
            {
                _logger.LogInformation("Significant weather change detected for farmer {FarmerId}, " +
                    "triggering recommendation update", farmerId);

                // Update stored forecast
                _previousForecasts[cacheKey] = currentForecast;

                // Trigger notification (would integrate with notification service)
                _logger.LogInformation("Notification triggered for farmer {FarmerId} about updated recommendations", 
                    farmerId);

                return true;
            }

            _logger.LogInformation("No significant weather changes for farmer {FarmerId}", farmerId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check weather changes for farmer {FarmerId}", farmerId);
            throw;
        }
    }

    public async Task<int> ProcessDailyUpdatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting daily recommendation update job");

        var updatedCount = 0;

        try
        {
            // In production, this would query a database for all active recommendations
            // For now, we'll process the cached forecasts
            var farmerIds = _previousForecasts.Keys.Select(k => k.Split('_')[0]).Distinct().ToList();

            foreach (var farmerId in farmerIds)
            {
                try
                {
                    var wasUpdated = await CheckAndUpdateRecommendationsAsync(farmerId, cancellationToken);
                    if (wasUpdated)
                        updatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update recommendations for farmer {FarmerId}", farmerId);
                    // Continue processing other farmers
                }
            }

            _logger.LogInformation("Daily update job completed. Updated {Count} recommendations", updatedCount);
            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily update job failed");
            throw;
        }
    }

    private bool DetectSignificantWeatherChange(
        WeatherForecast previousForecast,
        WeatherForecast currentForecast)
    {
        // Compare forecasts for the next 30 days (most relevant for planting decisions)
        var comparisonDays = 30;

        var previousDays = previousForecast.DailyForecasts
            .Take(comparisonDays)
            .ToDictionary(d => d.Date, d => d);

        var currentDays = currentForecast.DailyForecasts
            .Take(comparisonDays)
            .ToDictionary(d => d.Date, d => d);

        var significantChanges = 0;

        foreach (var date in previousDays.Keys.Intersect(currentDays.Keys))
        {
            var prevDay = previousDays[date];
            var currDay = currentDays[date];

            // Check for significant rainfall change (>20%)
            var rainfallChange = Math.Abs(currDay.Rainfall - prevDay.Rainfall);
            var rainfallChangePercent = prevDay.Rainfall > 0 
                ? rainfallChange / prevDay.Rainfall 
                : (currDay.Rainfall > 5 ? 1.0f : 0f); // Consider >5mm as significant if previous was 0

            if (rainfallChangePercent > SignificantRainfallChangeThreshold)
            {
                _logger.LogInformation("Significant rainfall change detected on {Date}: " +
                    "{PrevRainfall}mm -> {CurrRainfall}mm ({ChangePercent:P0})",
                    date, prevDay.Rainfall, currDay.Rainfall, rainfallChangePercent);
                significantChanges++;
            }

            // Check for extreme temperature changes (>5°C in max temp)
            var tempChange = Math.Abs(currDay.MaxTemperature - prevDay.MaxTemperature);
            if (tempChange > 5.0f)
            {
                _logger.LogInformation("Significant temperature change detected on {Date}: " +
                    "{PrevTemp}°C -> {CurrTemp}°C",
                    date, prevDay.MaxTemperature, currDay.MaxTemperature);
                significantChanges++;
            }
        }

        // Consider it significant if 3 or more days show significant changes
        return significantChanges >= 3;
    }
}
