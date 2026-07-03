using KisanMitraAI.Core.HistoricalAnalytics;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Infrastructure.Repositories.Timestream;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.HistoricalAnalytics;

/// <summary>
/// Implementation of regional benchmark aggregator with privacy protection
/// </summary>
public class RegionalBenchmarkAggregator : IRegionalBenchmarkAggregator
{
    private readonly ISoilDataRepository _soilRepository;
    private readonly IGradingHistoryRepository _gradingRepository;
    private readonly ILogger<RegionalBenchmarkAggregator> _logger;
    private const int MinimumFarmerCount = 10;

    public RegionalBenchmarkAggregator(
        ISoilDataRepository soilRepository,
        IGradingHistoryRepository gradingRepository,
        ILogger<RegionalBenchmarkAggregator> logger)
    {
        _soilRepository = soilRepository ?? throw new ArgumentNullException(nameof(soilRepository));
        _gradingRepository = gradingRepository ?? throw new ArgumentNullException(nameof(gradingRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RegionalBenchmark> AggregateRegionalSoilHealthAsync(
        string region,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aggregating soil health data for region {Region}", region);

        // In a real implementation, we would query all farmers in the region
        // For now, we'll create a placeholder implementation
        var farmerIds = await GetFarmerIdsInRegionAsync(region, cancellationToken);

        if (farmerIds.Count() < MinimumFarmerCount)
        {
            _logger.LogWarning(
                "Insufficient farmers in region {Region} for privacy-compliant aggregation. Found {Count}, need {Min}",
                region, farmerIds.Count(), MinimumFarmerCount);

            return CreateEmptyBenchmark(region, period, farmerIds.Count(), isPrivacyCompliant: false);
        }

        var allSoilData = new List<SoilHealthData>();
        foreach (var farmerId in farmerIds)
        {
            var soilData = await _soilRepository.GetSoilHistoryAsync(
                farmerId,
                period.StartDate,
                period.EndDate,
                cancellationToken);
            allSoilData.AddRange(soilData);
        }

        if (!allSoilData.Any())
        {
            return CreateEmptyBenchmark(region, period, farmerIds.Count(), isPrivacyCompliant: true);
        }

        // Calculate aggregate metrics using organic carbon as representative metric
        var values = allSoilData.Select(s => (decimal)s.OrganicCarbon).OrderBy(v => v).ToList();

        return CalculateBenchmark(region, period, farmerIds.Count(), values);
    }

    public async Task<RegionalBenchmark> AggregateRegionalQualityGradesAsync(
        string region,
        string produceType,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Aggregating quality grades for {ProduceType} in region {Region}",
            produceType, region);

        var farmerIds = await GetFarmerIdsInRegionAsync(region, cancellationToken);

        if (farmerIds.Count() < MinimumFarmerCount)
        {
            _logger.LogWarning(
                "Insufficient farmers in region {Region} for privacy-compliant aggregation",
                region);

            return CreateEmptyBenchmark(region, period, farmerIds.Count(), isPrivacyCompliant: false);
        }

        var allGrades = new List<GradingRecord>();
        foreach (var farmerId in farmerIds)
        {
            var grades = await _gradingRepository.GetGradingHistoryAsync(
                farmerId,
                period.StartDate,
                period.EndDate,
                cancellationToken);
            allGrades.AddRange(grades.Where(g => g.ProduceType == produceType));
        }

        if (!allGrades.Any())
        {
            return CreateEmptyBenchmark(region, period, farmerIds.Count(), isPrivacyCompliant: true);
        }

        // Convert grades to numeric values (A=4, B=3, C=2, Reject=1)
        var values = allGrades
            .Select(g => (decimal)((int)g.Grade + 1))
            .OrderBy(v => v)
            .ToList();

        return CalculateBenchmark(region, period, farmerIds.Count(), values);
    }

    public async Task<RegionalBenchmark> AggregateRegionalYieldsAsync(
        string region,
        string cropType,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Aggregating yields for {CropType} in region {Region}",
            cropType, region);

        var farmerIds = await GetFarmerIdsInRegionAsync(region, cancellationToken);

        if (farmerIds.Count() < MinimumFarmerCount)
        {
            return CreateEmptyBenchmark(region, period, farmerIds.Count(), isPrivacyCompliant: false);
        }

        // Placeholder: In real implementation, query yield data
        // For now, return empty benchmark
        return CreateEmptyBenchmark(region, period, farmerIds.Count(), isPrivacyCompliant: true);
    }

    public async Task<PercentileRanking> CalculatePercentileRankingAsync(
        string farmerId,
        string region,
        string metricType,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating percentile ranking for farmer {FarmerId} in region {Region}",
            farmerId, region);

        RegionalBenchmark benchmark;
        decimal farmerValue;

        if (metricType.Equals("soil_health", StringComparison.OrdinalIgnoreCase))
        {
            benchmark = await AggregateRegionalSoilHealthAsync(region, period, cancellationToken);
            
            var soilData = await _soilRepository.GetSoilHistoryAsync(
                farmerId,
                period.StartDate,
                period.EndDate,
                cancellationToken);
            
            farmerValue = soilData.Any() ? (decimal)soilData.Average(s => s.OrganicCarbon) : 0m;
        }
        else if (metricType.Equals("quality_grade", StringComparison.OrdinalIgnoreCase))
        {
            benchmark = await AggregateRegionalQualityGradesAsync(region, "all", period, cancellationToken);
            
            var grades = await _gradingRepository.GetGradingHistoryAsync(
                farmerId,
                period.StartDate,
                period.EndDate,
                cancellationToken);
            
            farmerValue = grades.Any() ? (decimal)grades.Average(g => (int)g.Grade + 1) : 0m;
        }
        else
        {
            throw new ArgumentException($"Unknown metric type: {metricType}", nameof(metricType));
        }

        if (!benchmark.IsPrivacyCompliant)
        {
            _logger.LogWarning("Cannot calculate percentile ranking - insufficient farmers in region");
            return new PercentileRanking(
                farmerId,
                region,
                metricType,
                farmerValue,
                0m,
                0,
                "Insufficient data for regional comparison",
                Array.Empty<string>());
        }

        var percentileRank = CalculatePercentile(farmerValue, benchmark);
        var interpretation = InterpretPercentile(percentileRank);
        var recommendations = GenerateRecommendations(percentileRank, metricType);

        return new PercentileRanking(
            farmerId,
            region,
            metricType,
            farmerValue,
            benchmark.AverageValue,
            percentileRank,
            interpretation,
            recommendations);
    }

    public async Task<RegionalAverages> GetRegionalAveragesAsync(
        string region,
        TimePeriod period,
        CancellationToken cancellationToken = default)
    {
        var farmerIds = await GetFarmerIdsInRegionAsync(region, cancellationToken);

        if (farmerIds.Count() < MinimumFarmerCount)
        {
            return new RegionalAverages(
                region,
                period,
                farmerIds.Count(),
                0m,
                0m,
                0m,
                Array.Empty<MetricAverage>());
        }

        var soilBenchmark = await AggregateRegionalSoilHealthAsync(region, period, cancellationToken);
        var gradeBenchmark = await AggregateRegionalQualityGradesAsync(region, "all", period, cancellationToken);

        var detailedMetrics = new List<MetricAverage>
        {
            new MetricAverage("Soil Organic Carbon", soilBenchmark.AverageValue, soilBenchmark.MedianValue, "%"),
            new MetricAverage("Quality Grade", gradeBenchmark.AverageValue, gradeBenchmark.MedianValue, "grade")
        };

        return new RegionalAverages(
            region,
            period,
            farmerIds.Count(),
            soilBenchmark.AverageValue,
            gradeBenchmark.AverageValue,
            0m, // Yield placeholder
            detailedMetrics);
    }

    public async Task<bool> ValidateMinimumFarmerCountAsync(
        string region,
        int minimumCount = 10,
        CancellationToken cancellationToken = default)
    {
        var farmerIds = await GetFarmerIdsInRegionAsync(region, cancellationToken);
        var isValid = farmerIds.Count() >= minimumCount;

        _logger.LogInformation(
            "Region {Region} has {Count} farmers (minimum: {Min}, valid: {Valid})",
            region, farmerIds.Count(), minimumCount, isValid);

        return isValid;
    }

    public async Task<RegionalBenchmark?> GetRegionalBenchmarkAsync(
        string location,
        string dataType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting regional benchmark for location {Location}, dataType {DataType}",
            location, dataType);

        // Use a default time period (last year) for benchmark
        var period = TimePeriod.LastYears(1);

        try
        {
            return dataType.ToLowerInvariant() switch
            {
                "soil" => await AggregateRegionalSoilHealthAsync(location, period, cancellationToken),
                "grades" => await AggregateRegionalQualityGradesAsync(location, "all", period, cancellationToken),
                "yields" => await AggregateRegionalYieldsAsync(location, "all", period, cancellationToken),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting regional benchmark for {Location}, {DataType}", location, dataType);
            return null;
        }
    }

    private async Task<IEnumerable<string>> GetFarmerIdsInRegionAsync(
        string region,
        CancellationToken cancellationToken)
    {
        // Placeholder: In real implementation, query farmer database by region
        // For now, return a mock list
        await Task.CompletedTask;
        
        // Simulate having enough farmers for testing
        return Enumerable.Range(1, 15).Select(i => $"farmer_{region}_{i}");
    }

    private RegionalBenchmark CreateEmptyBenchmark(
        string region,
        TimePeriod period,
        int farmerCount,
        bool isPrivacyCompliant)
    {
        return new RegionalBenchmark(
            region,
            period,
            farmerCount,
            0m,
            0m,
            0m,
            0m,
            0m,
            Array.Empty<Percentile>(),
            isPrivacyCompliant);
    }

    private RegionalBenchmark CalculateBenchmark(
        string region,
        TimePeriod period,
        int farmerCount,
        List<decimal> values)
    {
        var average = values.Average();
        var median = CalculateMedian(values);
        var min = values.Min();
        var max = values.Max();
        var stdDev = CalculateStandardDeviation(values, average);

        var percentiles = new List<Percentile>
        {
            new Percentile(25, CalculatePercentileValue(values, 25)),
            new Percentile(50, median),
            new Percentile(75, CalculatePercentileValue(values, 75)),
            new Percentile(90, CalculatePercentileValue(values, 90)),
            new Percentile(95, CalculatePercentileValue(values, 95))
        };

        return new RegionalBenchmark(
            region,
            period,
            farmerCount,
            average,
            median,
            min,
            max,
            stdDev,
            percentiles,
            IsPrivacyCompliant: true);
    }

    private decimal CalculateMedian(List<decimal> sortedValues)
    {
        var count = sortedValues.Count;
        if (count == 0) return 0m;

        if (count % 2 == 0)
        {
            return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2m;
        }
        else
        {
            return sortedValues[count / 2];
        }
    }

    private decimal CalculatePercentileValue(List<decimal> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0) return 0m;

        var index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
        
        return sortedValues[index];
    }

    private decimal CalculateStandardDeviation(List<decimal> values, decimal average)
    {
        if (values.Count < 2) return 0m;

        var sumOfSquares = values.Sum(v => (v - average) * (v - average));
        return (decimal)Math.Sqrt((double)(sumOfSquares / values.Count));
    }

    private int CalculatePercentile(decimal value, RegionalBenchmark benchmark)
    {
        // Simple percentile calculation
        if (value >= benchmark.MaxValue) return 100;
        if (value <= benchmark.MinValue) return 0;

        // Linear interpolation
        var range = benchmark.MaxValue - benchmark.MinValue;
        if (range == 0) return 50;

        var position = (value - benchmark.MinValue) / range;
        return (int)(position * 100);
    }

    private string InterpretPercentile(int percentile)
    {
        return percentile switch
        {
            >= 90 => "Excellent - Top 10% in your region",
            >= 75 => "Above Average - Top 25% in your region",
            >= 50 => "Average - Middle 50% in your region",
            >= 25 => "Below Average - Bottom 50% in your region",
            _ => "Needs Improvement - Bottom 25% in your region"
        };
    }

    private IEnumerable<string> GenerateRecommendations(int percentile, string metricType)
    {
        var recommendations = new List<string>();

        if (percentile < 50)
        {
            recommendations.Add($"Consider consulting with top-performing farmers in your region");
            recommendations.Add($"Review best practices for improving {metricType}");
        }
        else if (percentile >= 75)
        {
            recommendations.Add($"Maintain current practices - you're performing well");
            recommendations.Add($"Consider sharing your knowledge with other farmers");
        }

        return recommendations;
    }
}
