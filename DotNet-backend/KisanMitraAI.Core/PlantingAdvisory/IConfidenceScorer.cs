using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.PlantingAdvisory;

/// <summary>
/// Service for calculating confidence scores for planting recommendations
/// </summary>
public interface IConfidenceScorer
{
    /// <summary>
    /// Calculates confidence score for a planting window based on forecast reliability
    /// </summary>
    /// <param name="window">Planting window</param>
    /// <param name="forecast">Weather forecast</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confidence score between 0 and 100</returns>
    Task<float> CalculateConfidenceAsync(
        PlantingWindow window,
        WeatherForecast forecast,
        CancellationToken cancellationToken = default);
}
