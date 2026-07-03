namespace KisanMitraAI.Core.PlantingAdvisory;

/// <summary>
/// Service for updating planting recommendations when weather data changes
/// </summary>
public interface IRecommendationUpdateService
{
    /// <summary>
    /// Checks for significant weather changes and updates recommendations if needed
    /// </summary>
    /// <param name="farmerId">Farmer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if recommendations were updated</returns>
    Task<bool> CheckAndUpdateRecommendationsAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes daily update job for all active recommendations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of recommendations updated</returns>
    Task<int> ProcessDailyUpdatesAsync(
        CancellationToken cancellationToken = default);
}
