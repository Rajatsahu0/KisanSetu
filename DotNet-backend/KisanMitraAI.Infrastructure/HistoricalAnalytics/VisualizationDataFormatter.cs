using KisanMitraAI.Core.HistoricalAnalytics;
using KisanMitraAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.HistoricalAnalytics;

/// <summary>
/// Implementation of visualization data formatter for charting
/// </summary>
public class VisualizationDataFormatter : IVisualizationDataFormatter
{
    private readonly ILogger<VisualizationDataFormatter> _logger;

    public VisualizationDataFormatter(ILogger<VisualizationDataFormatter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ChartData> FormatTimeSeriesDataAsync<T>(
        TrendData<T> trendData,
        string chartTitle,
        string xAxisLabel,
        string yAxisLabel,
        CancellationToken cancellationToken = default)
    {
        var points = trendData.DataPoints
            .Select(dp => new ChartPoint(
                dp.Timestamp.ToString("yyyy-MM-dd"),
                ConvertToDecimal(dp.Value),
                dp.Timestamp))
            .ToList();

        var series = new List<ChartSeries>
        {
            new ChartSeries(
                "Actual Values",
                points,
                "#2563eb",
                ChartSeriesType.Line)
        };

        var annotations = trendData.Anomalies
            .Select(a => new ChartAnnotation(
                a.Point.Timestamp,
                a.Reason,
                AnnotationType.Anomaly))
            .ToList();

        var chartData = new ChartData(
            chartTitle,
            xAxisLabel,
            yAxisLabel,
            series,
            annotations);

        return Task.FromResult(chartData);
    }

    public Task<ChartData> CalculateMovingAverageAsync<T>(
        TrendData<T> trendData,
        int windowSize,
        string chartTitle,
        CancellationToken cancellationToken = default)
    {
        var actualPoints = trendData.DataPoints
            .Select(dp => new ChartPoint(
                dp.Timestamp.ToString("yyyy-MM-dd"),
                ConvertToDecimal(dp.Value),
                dp.Timestamp))
            .ToList();

        var movingAvgPoints = CalculateMovingAveragePoints(actualPoints, windowSize);

        var series = new List<ChartSeries>
        {
            new ChartSeries(
                "Actual Values",
                actualPoints,
                "#2563eb",
                ChartSeriesType.Line),
            new ChartSeries(
                $"{windowSize}-Period Moving Average",
                movingAvgPoints,
                "#dc2626",
                ChartSeriesType.Line)
        };

        var chartData = new ChartData(
            chartTitle,
            "Date",
            "Value",
            series,
            Array.Empty<ChartAnnotation>());

        return Task.FromResult(chartData);
    }

    public Task<ComparisonChartData> FormatComparisonDataAsync<T>(
        PeriodComparison<T> comparison,
        string chartTitle,
        CancellationToken cancellationToken = default)
    {
        var periodChartData = comparison.Periods.Select(p =>
        {
            var points = ExtractChartPoints(p.Data);
            return new PeriodChartData(
                p.Period.Label,
                p.AverageValue,
                p.TotalValue,
                p.DataPointCount,
                points);
        }).ToList();

        var insights = comparison.Insights
            .Select(i => i.Description)
            .ToList();

        var comparisonData = new ComparisonChartData(
            chartTitle,
            periodChartData,
            insights);

        return Task.FromResult(comparisonData);
    }

    public Task<IEnumerable<SignificantChange>> IdentifySignificantChangesAsync<T>(
        TrendData<T> trendData,
        float thresholdPercent = 20f,
        CancellationToken cancellationToken = default)
    {
        var changes = new List<SignificantChange>();
        var points = trendData.DataPoints.ToList();

        for (int i = 1; i < points.Count; i++)
        {
            var oldValue = ConvertToDecimal(points[i - 1].Value);
            var newValue = ConvertToDecimal(points[i].Value);

            if (oldValue == 0)
                continue;

            var changePercent = ((newValue - oldValue) / oldValue) * 100;

            if (Math.Abs(changePercent) >= (decimal)thresholdPercent)
            {
                var description = changePercent > 0
                    ? $"Increased by {changePercent:F1}%"
                    : $"Decreased by {Math.Abs(changePercent):F1}%";

                changes.Add(new SignificantChange(
                    points[i].Timestamp,
                    oldValue,
                    newValue,
                    changePercent,
                    description));
            }
        }

        return Task.FromResult<IEnumerable<SignificantChange>>(changes);
    }

    public Task<TrendLineData> PrepareTrendLineAsync<T>(
        TrendData<T> trendData,
        CancellationToken cancellationToken = default)
    {
        var actualPoints = trendData.DataPoints
            .Select(dp => new ChartPoint(
                dp.Timestamp.ToString("yyyy-MM-dd"),
                ConvertToDecimal(dp.Value),
                dp.Timestamp))
            .ToList();

        if (actualPoints.Count < 2)
        {
            return Task.FromResult(new TrendLineData(
                actualPoints,
                Array.Empty<ChartPoint>(),
                TrendDirection.Stable,
                0m,
                "y = 0"));
        }

        // Calculate linear regression for trend line
        var (slope, intercept) = CalculateLinearRegression(actualPoints);

        var trendLinePoints = actualPoints.Select((p, index) =>
        {
            var trendValue = intercept + (slope * index);
            return new ChartPoint(
                p.Label,
                trendValue,
                p.Timestamp);
        }).ToList();

        var equation = $"y = {slope:F2}x + {intercept:F2}";

        var trendLineData = new TrendLineData(
            actualPoints,
            trendLinePoints,
            trendData.Direction,
            slope,
            equation);

        return Task.FromResult(trendLineData);
    }

    private IEnumerable<ChartPoint> CalculateMovingAveragePoints(
        List<ChartPoint> points,
        int windowSize)
    {
        var result = new List<ChartPoint>();

        for (int i = windowSize - 1; i < points.Count; i++)
        {
            var window = points.Skip(i - windowSize + 1).Take(windowSize);
            var avg = window.Average(p => p.Value);
            
            result.Add(new ChartPoint(
                points[i].Label,
                avg,
                points[i].Timestamp));
        }

        return result;
    }

    private IEnumerable<ChartPoint> ExtractChartPoints<T>(IEnumerable<T> data)
    {
        var points = new List<ChartPoint>();

        if (typeof(T) == typeof(MandiPrice))
        {
            var prices = data.Cast<MandiPrice>();
            points.AddRange(prices.Select(p => new ChartPoint(
                p.PriceDate.ToString("yyyy-MM-dd"),
                p.ModalPrice,
                p.PriceDate)));
        }
        else if (typeof(T) == typeof(GradingRecord))
        {
            var records = data.Cast<GradingRecord>();
            points.AddRange(records.Select(r => new ChartPoint(
                r.Timestamp.ToString("yyyy-MM-dd"),
                r.CertifiedPrice,
                r.Timestamp)));
        }
        else if (typeof(T) == typeof(SoilHealthData))
        {
            var soilData = data.Cast<SoilHealthData>();
            points.AddRange(soilData.Select(s => new ChartPoint(
                s.TestDate.ToString("yyyy-MM-dd"),
                (decimal)s.OrganicCarbon,
                s.TestDate)));
        }

        return points;
    }

    private (decimal slope, decimal intercept) CalculateLinearRegression(List<ChartPoint> points)
    {
        var n = points.Count;
        var sumX = 0m;
        var sumY = 0m;
        var sumXY = 0m;
        var sumX2 = 0m;

        for (int i = 0; i < n; i++)
        {
            var x = i;
            var y = points[i].Value;

            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        var intercept = (sumY - slope * sumX) / n;

        return (slope, intercept);
    }

    private decimal ConvertToDecimal<T>(T value)
    {
        if (value is decimal d)
            return d;
        if (value is float f)
            return (decimal)f;
        if (value is double db)
            return (decimal)db;
        if (value is int i)
            return i;
        if (value is long l)
            return l;
        if (value is QualityGrade grade)
            return (int)grade + 1; // A=4, B=3, C=2, Reject=1

        return 0m;
    }
}
