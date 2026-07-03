using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.PlantingAdvisory;

/// <summary>
/// Service for analyzing optimal planting windows using AI
/// </summary>
public interface IPlantingWindowAnalyzer
{
    /// <summary>
    /// Analyzes weather and soil data to identify optimal planting windows
    /// </summary>
    /// <param name="forecast">Weather forecast data</param>
    /// <param name="soilData">Soil health data</param>
    /// <param name="cropType">Type of crop to plant</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of recommended planting windows</returns>
    Task<IEnumerable<PlantingWindow>> AnalyzePlantingWindowsAsync(
        WeatherForecast forecast,
        SoilHealthData soilData,
        string cropType,
        CancellationToken cancellationToken = default,
        string? language = null);
}
