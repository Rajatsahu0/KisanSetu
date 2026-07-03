using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.HistoricalAnalytics;

/// <summary>
/// Service for aggregating anonymized regional benchmark data
/// </summary>
public interface IRegionalBenchmarkAggregator
{
    /// <summary>
    /// Aggregates soil health data across farmers in a region (minimum 10 farmers)
    /// </summary>
    Task<RegionalBenchmark> AggregateRegionalSoilHealthAsync(
        string region,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates quality grades across farmers in a region (minimum 10 farmers)
    /// </summary>
    Task<RegionalBenchmark> AggregateRegionalQualityGradesAsync(
        string region,
        string produceType,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates yield data across farmers in a region (minimum 10 farmers)
    /// </summary>
    Task<RegionalBenchmark> AggregateRegionalYieldsAsync(
        string region,
        string cropType,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates percentile ranking for a farmer within their region
    /// </summary>
    Task<PercentileRanking> CalculatePercentileRankingAsync(
        string farmerId,
        string region,
        string metricType,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets regional averages for comparison
    /// </summary>
    Task<RegionalAverages> GetRegionalAveragesAsync(
        string region,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that minimum farmer count is met for privacy
    /// </summary>
    Task<bool> ValidateMinimumFarmerCountAsync(
        string region,
        int minimumCount = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets regional benchmark for a specific location and data type
    /// </summary>
    Task<RegionalBenchmark?> GetRegionalBenchmarkAsync(
        string location,
        string dataType,
        CancellationToken cancellationToken = default);
}
