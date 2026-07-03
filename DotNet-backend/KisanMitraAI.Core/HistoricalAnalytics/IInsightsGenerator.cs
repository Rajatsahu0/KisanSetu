using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.HistoricalAnalytics;

/// <summary>
/// Service for generating insights from historical data patterns using AI
/// </summary>
public interface IInsightsGenerator
{
    /// <summary>
    /// Detects patterns in historical data (requires 2+ years of data)
    /// </summary>
    Task<IEnumerable<DataPattern>> DetectPatternsAsync<T>(
        TrendData<T> trendData,
        string dataType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates natural language insights from trend data
    /// </summary>
    Task<IEnumerable<Insight>> GenerateInsightsAsync<T>(
        TrendData<T> trendData,
        string farmerId,
        string dataType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies trends (improving, declining, stable) in the data
    /// </summary>
    Task<TrendAnalysis> AnalyzeTrendAsync<T>(
        TrendData<T> trendData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests actions based on historical patterns
    /// </summary>
    Task<IEnumerable<ActionSuggestion>> SuggestActionsAsync<T>(
        TrendData<T> trendData,
        string farmerId,
        string dataType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates insights from period comparisons
    /// </summary>
    Task<IEnumerable<Insight>> GenerateComparisonInsightsAsync<T>(
        PeriodComparison<T> comparison,
        string farmerId,
        string dataType,
        CancellationToken cancellationToken = default);
}
