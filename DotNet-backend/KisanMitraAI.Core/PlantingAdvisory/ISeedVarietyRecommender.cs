using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.PlantingAdvisory;

/// <summary>
/// Service for recommending seed varieties based on conditions
/// </summary>
public interface ISeedVarietyRecommender
{
    /// <summary>
    /// Recommends seed varieties suited to the planting window and soil conditions
    /// </summary>
    /// <param name="window">Planting window</param>
    /// <param name="soilData">Soil health data</param>
    /// <param name="cropType">Type of crop</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of seed variety recommendations</returns>
    Task<IEnumerable<SeedRecommendation>> RecommendVarietiesAsync(
        PlantingWindow window,
        SoilHealthData soilData,
        string cropType,
        CancellationToken cancellationToken = default,
        string? language = null);
}
