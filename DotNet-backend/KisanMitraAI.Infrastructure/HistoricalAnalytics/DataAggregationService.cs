using KisanMitraAI.Core.HistoricalAnalytics;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Infrastructure.Repositories.Timestream;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.HistoricalAnalytics;

/// <summary>
/// Implementation of data aggregation service for historical analytics
/// </summary>
public class DataAggregationService : IDataAggregationService
{
    private readonly IMandiPriceRepository _priceRepository;
    private readonly ISoilDataRepository _soilRepository;
    private readonly IGradingHistoryRepository _gradingRepository;
    private readonly ILogger<DataAggregationService> _logger;

    public DataAggregationService(
        IMandiPriceRepository priceRepository,
        ISoilDataRepository soilRepository,
        IGradingHistoryRepository gradingRepository,
        ILogger<DataAggregationService> logger)
    {
        _priceRepository = priceRepository ?? throw new ArgumentNullException(nameof(priceRepository));
        _soilRepository = soilRepository ?? throw new ArgumentNullException(nameof(soilRepository));
        _gradingRepository = gradingRepository ?? throw new ArgumentNullException(nameof(gradingRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<MandiPrice>> GetHistoricalPricesAsync(
        string commodity,
        string location,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving historical prices for {Commodity} in {Location} from {Start} to {End}",
            commodity, location, period.StartDate, period.EndDate);

        return await _priceRepository.GetHistoricalPricesAsync(
            commodity,
            location,
            period.StartDate,
            period.EndDate,
            cancellationToken);
    }

    public async Task<IEnumerable<SoilHealthData>> GetHistoricalSoilDataAsync(
        string farmerId,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving historical soil data for farmer {FarmerId} from {Start} to {End}",
            farmerId, period.StartDate, period.EndDate);

        return await _soilRepository.GetSoilHistoryAsync(
            farmerId,
            period.StartDate,
            period.EndDate,
            cancellationToken);
    }

    public async Task<IEnumerable<GradingRecord>> GetHistoricalGradingDataAsync(
        string farmerId,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving historical grading data for farmer {FarmerId} from {Start} to {End}",
            farmerId, period.StartDate, period.EndDate);

        return await _gradingRepository.GetGradingHistoryAsync(
            farmerId,
            period.StartDate,
            period.EndDate,
            cancellationToken);
    }

    public async Task<TrendData<decimal>> CalculatePriceTrendAsync(
        string commodity,
        string location,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        var prices = await GetHistoricalPricesAsync(commodity, location, period, cancellationToken);
        return CalculatePriceTrendFromData(prices);
    }

    /// <summary>
    /// OPTIMIZED: Calculate trend from pre-fetched prices. Avoids redundant DynamoDB read.
    /// </summary>
    public TrendData<decimal> CalculatePriceTrendFromData(IEnumerable<MandiPrice> prices)
    {
        var priceList = prices.ToList();

        if (!priceList.Any())
        {
            return new TrendData<decimal>(
                Array.Empty<DataPoint<decimal>>(),
                TrendDirection.Stable,
                0m,
                0m,
                0m,
                Array.Empty<Anomaly<decimal>>());
        }

        var dataPoints = priceList
            .Select(p => new DataPoint<decimal>(p.ModalPrice, p.PriceDate))
            .OrderBy(p => p.Timestamp)
            .ToList();

        var direction = CalculateTrendDirection(dataPoints.Select(p => p.Value).ToList());
        var minValue = dataPoints.Min(p => p.Value);
        var maxValue = dataPoints.Max(p => p.Value);
        var avgValue = dataPoints.Average(p => p.Value);
        var anomalies = DetectAnomalies(dataPoints, avgValue);

        return new TrendData<decimal>(
            dataPoints,
            direction,
            minValue,
            maxValue,
            avgValue,
            anomalies);
    }

    public async Task<TrendData<float>> CalculateSoilNutrientTrendAsync(
        string farmerId,
        string nutrientName,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        var soilData = await GetHistoricalSoilDataAsync(farmerId, period, cancellationToken);
        return CalculateSoilNutrientTrendFromData(soilData, nutrientName);
    }

    /// <summary>
    /// OPTIMIZED: Calculate nutrient trend from pre-fetched soil data. Avoids redundant DynamoDB read.
    /// </summary>
    public TrendData<float> CalculateSoilNutrientTrendFromData(IEnumerable<SoilHealthData> soilData, string nutrientName)
    {
        var soilList = soilData.ToList();

        if (!soilList.Any())
        {
            return new TrendData<float>(
                Array.Empty<DataPoint<float>>(),
                TrendDirection.Stable,
                0f,
                0f,
                0f,
                Array.Empty<Anomaly<float>>());
        }

        var dataPoints = soilList
            .Select(s => new DataPoint<float>(GetNutrientValue(s, nutrientName), s.TestDate))
            .OrderBy(p => p.Timestamp)
            .ToList();

        var direction = CalculateTrendDirection(dataPoints.Select(p => p.Value).ToList());
        var minValue = dataPoints.Min(p => p.Value);
        var maxValue = dataPoints.Max(p => p.Value);
        var avgValue = dataPoints.Average(p => p.Value);
        var anomalies = DetectAnomalies(dataPoints, avgValue);

        return new TrendData<float>(
            dataPoints,
            direction,
            minValue,
            maxValue,
            avgValue,
            anomalies);
    }

    public async Task<TrendData<QualityGrade>> CalculateGradeTrendAsync(
        string farmerId,
        string produceType,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        var grades = await GetHistoricalGradingDataAsync(farmerId, period, cancellationToken);
        return CalculateGradeTrendFromData(grades, produceType);
    }

    /// <summary>
    /// OPTIMIZED: Calculate grade trend from pre-fetched grading data. Avoids redundant DynamoDB read.
    /// </summary>
    public TrendData<QualityGrade> CalculateGradeTrendFromData(IEnumerable<GradingRecord> grades, string produceType)
    {
        var gradeList = grades.Where(g => g.ProduceType == produceType).ToList();

        if (!gradeList.Any())
        {
            return new TrendData<QualityGrade>(
                Array.Empty<DataPoint<QualityGrade>>(),
                TrendDirection.Stable,
                QualityGrade.C,
                QualityGrade.C,
                QualityGrade.C,
                Array.Empty<Anomaly<QualityGrade>>());
        }

        var dataPoints = gradeList
            .Select(g => new DataPoint<QualityGrade>(g.Grade, g.Timestamp))
            .OrderBy(p => p.Timestamp)
            .ToList();

        // Calculate numeric values for grades (A=4, B=3, C=2, Reject=1)
        var numericValues = dataPoints.Select(p => (int)p.Value + 1).ToList();
        var direction = CalculateTrendDirection(numericValues);
        
        var gradeValues = dataPoints.Select(p => p.Value).ToList();
        var minGrade = gradeValues.Min();
        var maxGrade = gradeValues.Max();
        var avgGradeValue = (int)Math.Round(numericValues.Average());
        var avgGrade = (QualityGrade)(avgGradeValue - 1);

        return new TrendData<QualityGrade>(
            dataPoints,
            direction,
            minGrade,
            maxGrade,
            avgGrade,
            Array.Empty<Anomaly<QualityGrade>>());
    }

    public async Task<PeriodComparison<T>> ComparePeriodsAsync<T>(
        string identifier,
        IEnumerable<TimePeriod> periods,
        Func<string, TimePeriod, CancellationToken, Task<IEnumerable<T>>> dataRetriever,
        CancellationToken cancellationToken = default)
    {
        var periodDataList = new List<PeriodData<T>>();

        foreach (var period in periods)
        {
            var data = await dataRetriever(identifier, period, cancellationToken);
            var dataList = data.ToList();

            var avgValue = CalculateAverageValue(dataList);
            var totalValue = CalculateTotalValue(dataList);

            periodDataList.Add(new PeriodData<T>(
                period,
                dataList,
                avgValue,
                totalValue,
                dataList.Count));
        }

        var insights = GenerateComparisonInsights(periodDataList);

        return new PeriodComparison<T>(periodDataList, insights);
    }

    private float GetNutrientValue(SoilHealthData soil, string nutrientName)
    {
        return nutrientName.ToLower() switch
        {
            "nitrogen" => soil.Nitrogen,
            "phosphorus" => soil.Phosphorus,
            "potassium" => soil.Potassium,
            "ph" => soil.pH,
            "organiccarbon" => soil.OrganicCarbon,
            "sulfur" => soil.Sulfur,
            "zinc" => soil.Zinc,
            "boron" => soil.Boron,
            "iron" => soil.Iron,
            "manganese" => soil.Manganese,
            "copper" => soil.Copper,
            _ => 0f
        };
    }

    private TrendDirection CalculateTrendDirection<T>(List<T> values) where T : IComparable<T>
    {
        if (values.Count < 2)
            return TrendDirection.Stable;

        var increases = 0;
        var decreases = 0;

        for (int i = 1; i < values.Count; i++)
        {
            var comparison = values[i].CompareTo(values[i - 1]);
            if (comparison > 0) increases++;
            else if (comparison < 0) decreases++;
        }

        var changeRatio = Math.Max(increases, decreases) / (double)values.Count;

        if (changeRatio < 0.3)
            return TrendDirection.Stable;
        if (changeRatio > 0.7)
            return increases > decreases ? TrendDirection.Increasing : TrendDirection.Decreasing;
        
        return TrendDirection.Volatile;
    }

    private List<Anomaly<T>> DetectAnomalies<T>(List<DataPoint<T>> dataPoints, T average) where T : IComparable<T>
    {
        var anomalies = new List<Anomaly<T>>();

        if (typeof(T) == typeof(decimal))
        {
            var avgDecimal = Convert.ToDecimal(average);
            var stdDev = CalculateStandardDeviation(dataPoints.Select(p => Convert.ToDecimal(p.Value)).ToList());
            var threshold = stdDev * 2;

            foreach (var point in dataPoints)
            {
                var value = Convert.ToDecimal(point.Value);
                var deviation = Math.Abs(value - avgDecimal);

                if (deviation > threshold)
                {
                    anomalies.Add(new Anomaly<T>(
                        point,
                        $"Value deviates {deviation:F2} from average {avgDecimal:F2}",
                        (float)(deviation / avgDecimal)));
                }
            }
        }
        else if (typeof(T) == typeof(float))
        {
            var avgFloat = Convert.ToSingle(average);
            var stdDev = CalculateStandardDeviation(dataPoints.Select(p => Convert.ToSingle(p.Value)).ToList());
            var threshold = stdDev * 2;

            foreach (var point in dataPoints)
            {
                var value = Convert.ToSingle(point.Value);
                var deviation = Math.Abs(value - avgFloat);

                if (deviation > threshold)
                {
                    anomalies.Add(new Anomaly<T>(
                        point,
                        $"Value deviates {deviation:F2} from average {avgFloat:F2}",
                        deviation / avgFloat));
                }
            }
        }

        return anomalies;
    }

    private decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (values.Count < 2)
            return 0m;

        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        return (decimal)Math.Sqrt((double)(sumOfSquares / values.Count));
    }

    private float CalculateStandardDeviation(List<float> values)
    {
        if (values.Count < 2)
            return 0f;

        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        return (float)Math.Sqrt(sumOfSquares / values.Count);
    }

    private decimal CalculateAverageValue<T>(List<T> data)
    {
        if (!data.Any())
            return 0m;

        if (typeof(T) == typeof(MandiPrice))
        {
            var prices = data.Cast<MandiPrice>();
            return prices.Average(p => p.ModalPrice);
        }
        else if (typeof(T) == typeof(GradingRecord))
        {
            var records = data.Cast<GradingRecord>();
            return records.Average(r => r.CertifiedPrice);
        }
        else if (typeof(T) == typeof(SoilHealthData))
        {
            // For soil data, return average organic carbon as a representative metric
            var soilData = data.Cast<SoilHealthData>();
            return (decimal)soilData.Average(s => s.OrganicCarbon);
        }

        return 0m;
    }

    private decimal CalculateTotalValue<T>(List<T> data)
    {
        if (!data.Any())
            return 0m;

        if (typeof(T) == typeof(MandiPrice))
        {
            var prices = data.Cast<MandiPrice>();
            return prices.Sum(p => p.ModalPrice);
        }
        else if (typeof(T) == typeof(GradingRecord))
        {
            var records = data.Cast<GradingRecord>();
            return records.Sum(r => r.CertifiedPrice);
        }

        return 0m;
    }

    private IEnumerable<ComparisonInsight> GenerateComparisonInsights<T>(List<PeriodData<T>> periods)
    {
        var insights = new List<ComparisonInsight>();

        if (periods.Count < 2)
            return insights;

        // Compare consecutive periods
        for (int i = 1; i < periods.Count; i++)
        {
            var current = periods[i];
            var previous = periods[i - 1];

            var change = current.AverageValue - previous.AverageValue;
            var changePercent = previous.AverageValue != 0 
                ? (change / previous.AverageValue) * 100 
                : 0;

            if (Math.Abs(changePercent) < 5)
            {
                insights.Add(new ComparisonInsight(
                    $"{current.Period.Label} shows stable performance compared to {previous.Period.Label}",
                    InsightType.Stable,
                    0.9f));
            }
            else if (changePercent > 0)
            {
                insights.Add(new ComparisonInsight(
                    $"{current.Period.Label} shows {changePercent:F1}% improvement over {previous.Period.Label}",
                    InsightType.Improvement,
                    0.85f));
            }
            else
            {
                insights.Add(new ComparisonInsight(
                    $"{current.Period.Label} shows {Math.Abs(changePercent):F1}% decline from {previous.Period.Label}",
                    InsightType.Decline,
                    0.85f));
            }
        }

        return insights;
    }
}
