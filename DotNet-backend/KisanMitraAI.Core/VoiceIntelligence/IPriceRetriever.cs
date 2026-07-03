using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.VoiceIntelligence;

/// <summary>
/// Service for retrieving Mandi prices
/// </summary>
public interface IPriceRetriever
{
    /// <summary>
    /// Gets current Mandi prices for a commodity and location
    /// </summary>
    /// <param name="commodity">Commodity name</param>
    /// <param name="location">Location/Mandi name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of current Mandi prices</returns>
    Task<IEnumerable<MandiPrice>> GetCurrentPricesAsync(
        string commodity,
        string location,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets historical Mandi prices for a commodity and location
    /// </summary>
    /// <param name="commodity">Commodity name</param>
    /// <param name="location">Location/Mandi name</param>
    /// <param name="startDate">Start date for historical data</param>
    /// <param name="endDate">End date for historical data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of historical Mandi prices</returns>
    Task<IEnumerable<MandiPrice>> GetHistoricalPricesAsync(
        string commodity,
        string location,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken);
}
