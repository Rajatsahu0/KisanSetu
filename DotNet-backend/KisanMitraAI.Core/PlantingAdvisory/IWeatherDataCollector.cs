using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.PlantingAdvisory;

/// <summary>
/// Service for collecting weather forecast data for planting recommendations
/// </summary>
public interface IWeatherDataCollector
{
    /// <summary>
    /// Retrieves hyper-local weather forecast for the specified location
    /// </summary>
    /// <param name="location">Farm location (city/district)</param>
    /// <param name="daysAhead">Number of days to forecast (up to 90)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Weather forecast with daily predictions</returns>
    Task<WeatherForecast> GetForecastAsync(
        string location,
        int daysAhead,
        CancellationToken cancellationToken = default);
}
