using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.HistoricalAnalytics;

/// <summary>
/// Service for aggregating and retrieving historical data across all data types
/// </summary>
public interface IDataAggregationService
{
    /// <summary>
    /// Retrieves historical Mandi prices for a specific time period
    /// </summary>
    Task<IEnumerable<MandiPrice>> GetHistoricalPricesAsync(
        string commodity,
        string location,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves historical soil health data for a farmer
    /// </summary>
    Task<IEnumerable<SoilHealthData>> GetHistoricalSoilDataAsync(
        string farmerId,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves historical quality grading records for a farmer
    /// </summary>
    Task<IEnumerable<GradingRecord>> GetHistoricalGradingDataAsync(
        string farmerId,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates trend data for prices over a time period
    /// </summary>
    Task<TrendData<decimal>> CalculatePriceTrendAsync(
        string commodity,
        string location,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates trend data for soil nutrients over a time period
    /// </summary>
    Task<TrendData<float>> CalculateSoilNutrientTrendAsync(
        string farmerId,
        string nutrientName,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates trend data for quality grades over a time period
    /// </summary>
    Task<TrendData<QualityGrade>> CalculateGradeTrendAsync(
        string farmerId,
        string produceType,
        TimePeriod period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares data across multiple time periods
    /// </summary>
    Task<PeriodComparison<T>> ComparePeriodsAsync<T>(
        string identifier,
        IEnumerable<TimePeriod> periods,
        Func<string, TimePeriod, CancellationToken, Task<IEnumerable<T>>> dataRetriever,
        CancellationToken cancellationToken = default);
}
