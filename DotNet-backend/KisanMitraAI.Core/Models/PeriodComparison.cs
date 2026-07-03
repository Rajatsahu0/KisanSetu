namespace KisanMitraAI.Core.Models;

/// <summary>
/// Represents a comparison of data across multiple time periods
/// </summary>
public record PeriodComparison<T>(
    IEnumerable<PeriodData<T>> Periods,
    IEnumerable<ComparisonInsight> Insights)
{
    /// <summary>
    /// Gets the period with the highest average value
    /// </summary>
    public PeriodData<T>? GetBestPeriod()
    {
        return Periods.OrderByDescending(p => p.AverageValue).FirstOrDefault();
    }

    /// <summary>
    /// Gets the period with the lowest average value
    /// </summary>
    public PeriodData<T>? GetWorstPeriod()
    {
        return Periods.OrderBy(p => p.AverageValue).FirstOrDefault();
    }
}

/// <summary>
/// Represents data for a specific time period
/// </summary>
public record PeriodData<T>(
    TimePeriod Period,
    IEnumerable<T> Data,
    decimal AverageValue,
    decimal TotalValue,
    int DataPointCount);

/// <summary>
/// Represents an insight from comparing periods
/// </summary>
public record ComparisonInsight(
    string Description,
    InsightType Type,
    float Confidence);

/// <summary>
/// Types of comparison insights
/// </summary>
public enum InsightType
{
    Improvement,
    Decline,
    Seasonal,
    Anomaly,
    Stable
}
