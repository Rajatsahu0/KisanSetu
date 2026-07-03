namespace KisanMitraAI.Core.Offline;

/// <summary>
/// Service for caching data for offline access
/// </summary>
public interface IOfflineCacheService
{
    /// <summary>
    /// Cache Mandi prices for offline access
    /// </summary>
    Task CachePricesAsync(
        string farmerId,
        IEnumerable<Models.MandiPrice> prices,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cached Mandi prices for offline access
    /// </summary>
    Task<IEnumerable<Models.MandiPrice>> GetCachedPricesAsync(
        string farmerId,
        string? commodity = null,
        string? location = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache size for a farmer in bytes
    /// </summary>
    Task<long> GetCacheSizeAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate cache for a farmer
    /// </summary>
    Task InvalidateCacheAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh cache with latest prices
    /// </summary>
    Task RefreshCacheAsync(
        string farmerId,
        CancellationToken cancellationToken = default);
}
