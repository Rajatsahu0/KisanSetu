namespace KisanMitraAI.Core.Models;

/// <summary>
/// Represents aggregated regional benchmark data
/// </summary>
public record RegionalBenchmark(
    string Region,
    TimePeriod Period,
    int FarmerCount,
    decimal AverageValue,
    decimal MedianValue,
    decimal MinValue,
    decimal MaxValue,
    decimal StandardDeviation,
    IEnumerable<Percentile> Percentiles,
    bool IsPrivacyCompliant);

/// <summary>
/// Represents a percentile value
/// </summary>
public record Percentile(
    int PercentileRank,
    decimal Value);

/// <summary>
/// Represents a farmer's percentile ranking within their region
/// </summary>
public record PercentileRanking(
    string FarmerId,
    string Region,
    string MetricType,
    decimal FarmerValue,
    decimal RegionalAverage,
    int PercentileRank,
    string Interpretation,
    IEnumerable<string> Recommendations);

/// <summary>
/// Represents regional averages across multiple metrics
/// </summary>
public record RegionalAverages(
    string Region,
    TimePeriod Period,
    int FarmerCount,
    decimal AverageSoilHealth,
    decimal AverageQualityGrade,
    decimal AverageYield,
    IEnumerable<MetricAverage> DetailedMetrics);

/// <summary>
/// Represents an average for a specific metric
/// </summary>
public record MetricAverage(
    string MetricName,
    decimal Average,
    decimal Median,
    string Unit);
