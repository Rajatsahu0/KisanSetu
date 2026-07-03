using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.PlantingAdvisory;

/// <summary>
/// Service for retrieving farmer's soil health data
/// </summary>
public interface ISoilDataRetriever
{
    /// <summary>
    /// Retrieves the most recent soil health data for a farmer
    /// </summary>
    /// <param name="farmerId">Farmer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest soil health data</returns>
    Task<SoilHealthData?> GetLatestSoilDataAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves soil health data from a saved regenerative plan
    /// </summary>
    /// <param name="farmerId">Farmer identifier</param>
    /// <param name="planId">Saved plan identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Soil health data from the saved plan</returns>
    Task<SoilHealthData?> GetSoilDataFromPlanAsync(
        string farmerId,
        string planId,
        CancellationToken cancellationToken = default);
}
