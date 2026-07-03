namespace KisanMitraAI.Core.Models;

/// <summary>
/// Represents formatted data for charting
/// </summary>
public record ChartData(
    string Title,
    string XAxisLabel,
    string YAxisLabel,
    IEnumerable<ChartSeries> Series,
    IEnumerable<ChartAnnotation> Annotations);

/// <summary>
/// Represents a data series in a chart
/// </summary>
public record ChartSeries(
    string Name,
    IEnumerable<ChartPoint> Points,
    string Color,
    ChartSeriesType Type);

/// <summary>
/// Represents a single point in a chart
/// </summary>
public record ChartPoint(
    string Label,
    decimal Value,
    DateTimeOffset Timestamp);

/// <summary>
/// Represents an annotation on a chart (e.g., anomaly marker)
/// </summary>
public record ChartAnnotation(
    DateTimeOffset Timestamp,
    string Text,
    AnnotationType Type);

/// <summary>
/// Types of chart series
/// </summary>
public enum ChartSeriesType
{
    Line,
    Bar,
    Area,
    Scatter
}

/// <summary>
/// Types of chart annotations
/// </summary>
public enum AnnotationType
{
    Anomaly,
    Peak,
    Trough,
    Milestone
}

/// <summary>
/// Represents comparison chart data for multiple periods
/// </summary>
public record ComparisonChartData(
    string Title,
    IEnumerable<PeriodChartData> Periods,
    IEnumerable<string> Insights);

/// <summary>
/// Represents chart data for a specific period
/// </summary>
public record PeriodChartData(
    string PeriodLabel,
    decimal AverageValue,
    decimal TotalValue,
    int DataPointCount,
    IEnumerable<ChartPoint> Points);

/// <summary>
/// Represents a significant change in the data
/// </summary>
public record SignificantChange(
    DateTimeOffset Timestamp,
    decimal OldValue,
    decimal NewValue,
    decimal ChangePercent,
    string Description);

/// <summary>
/// Represents trend line data for visualization
/// </summary>
public record TrendLineData(
    IEnumerable<ChartPoint> ActualPoints,
    IEnumerable<ChartPoint> TrendLinePoints,
    TrendDirection Direction,
    decimal Slope,
    string Equation);
