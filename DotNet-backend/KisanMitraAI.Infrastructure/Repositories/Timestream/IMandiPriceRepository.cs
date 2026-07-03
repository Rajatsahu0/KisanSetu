using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Infrastructure.Repositories.Timestream;

/// <summary>
/// Repository for Mandi price data in Timestream
/// </summary>
public interface IMandiPriceRepository
{
    Task StorePriceAsync(
        MandiPrice price,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<MandiPrice>> GetCurrentPricesAsync(
        string commodity, 
        string location,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical prices for a commodity at a location within a date range
    /// </summary>
    Task<IEnumerable<MandiPrice>> GetHistoricalPricesAsync(
        string commodity, 
        string location, 
        DateTimeOffset startDate, 
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price trends for a commodity at a location
    /// </summary>
    Task<IEnumerable<MandiPrice>> GetPriceTrendsAsync(
        string commodity, 
        string location,
        int daysBack = 30,
        CancellationToken cancellationToken = default);
}
