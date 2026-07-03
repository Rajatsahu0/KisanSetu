using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Infrastructure.Repositories.Timestream;

/// <summary>
/// Repository for soil health data in Timestream (10-year retention)
/// </summary>
public interface ISoilDataRepository
{
    /// <summary>
    /// Stores soil health data
    /// </summary>
    Task StoreSoilDataAsync(
        SoilHealthData soilData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets soil health history for a farmer
    /// </summary>
    Task<IEnumerable<SoilHealthData>> GetSoilHistoryAsync(
        string farmerId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        CancellationToken cancellationToken = default);
}
