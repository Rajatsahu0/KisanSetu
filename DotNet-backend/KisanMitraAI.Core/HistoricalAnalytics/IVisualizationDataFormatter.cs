using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.HistoricalAnalytics;

/// <summary>
/// Service for formatting historical data for visualization and charting
/// </summary>
public interface IVisualizationDataFormatter
{
    /// <summary>
    /// Formats time-series data for charting (JSON format for frontend)
    /// </summary>
    Task<ChartData> FormatTimeSeriesDataAsync<T>(
        TrendData<T> trendData,
        string chartTitle,
        string xAxisLabel,
        string yAxisLabel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates moving averages for smoothing trend lines
    /// </summary>
    Task<ChartData> CalculateMovingAverageAsync<T>(
        TrendData<T> trendData,
        int windowSize,
        string chartTitle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats comparison data for side-by-side display
    /// </summary>
    Task<ComparisonChartData> FormatComparisonDataAsync<T>(
        PeriodComparison<T> comparison,
        string chartTitle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies significant changes and anomalies in the data
    /// </summary>
    Task<IEnumerable<SignificantChange>> IdentifySignificantChangesAsync<T>(
        TrendData<T> trendData,
        float thresholdPercent = 20f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prepares data for trend line visualization
    /// </summary>
    Task<TrendLineData> PrepareTrendLineAsync<T>(
        TrendData<T> trendData,
        CancellationToken cancellationToken = default);
}
