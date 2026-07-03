using KisanMitraAI.Core.Authorization;
using KisanMitraAI.Core.HistoricalAnalytics;
using KisanMitraAI.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KisanMitraAI.API.Controllers;

/// <summary>
/// Historical data controller for tracking and analyzing historical data
/// Requirements: 7.2, 7.4, 7.5
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[RequiresFarmer]
public class HistoricalDataController : ControllerBase
{
    private readonly IDataAggregationService _dataAggregationService;
    private readonly IVisualizationDataFormatter _visualizationFormatter;
    private readonly IInsightsGenerator _insightsGenerator;
    private readonly IRegionalBenchmarkAggregator _benchmarkAggregator;
    private readonly ILogger<HistoricalDataController> _logger;

    public HistoricalDataController(
        IDataAggregationService dataAggregationService,
        IVisualizationDataFormatter visualizationFormatter,
        IInsightsGenerator insightsGenerator,
        IRegionalBenchmarkAggregator benchmarkAggregator,
        ILogger<HistoricalDataController> logger)
    {
        _dataAggregationService = dataAggregationService;
        _visualizationFormatter = visualizationFormatter;
        _insightsGenerator = insightsGenerator;
        _benchmarkAggregator = benchmarkAggregator;
        _logger = logger;
    }

    /// <summary>
    /// Get historical Mandi prices for a commodity and location
    /// </summary>
    /// <param name="commodity">Commodity name</param>
    /// <param name="location">Location name</param>
    /// <param name="periodType">Time period type (last7days, lastSeason, lastYear, custom)</param>
    /// <param name="startDate">Start date for custom period</param>
    /// <param name="endDate">End date for custom period</param>
    /// <param name="compareWith">Optional comparison period type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical price data with trends and visualization</returns>
    [HttpGet("prices")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(HistoricalPriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetHistoricalPrices(
        [FromQuery] string commodity,
        [FromQuery] string location,
        [FromQuery] string periodType = "last7days",
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        [FromQuery] string? compareWith = null,
        CancellationToken cancellationToken = default)
    {
        var farmerId = User.GetFarmerId();
        
        _logger.LogInformation(
            "Historical prices request from farmer {FarmerId} for {Commodity} at {Location}, period {PeriodType}",
            farmerId,
            commodity,
            location,
            periodType);

        // Validate inputs
        if (string.IsNullOrWhiteSpace(commodity))
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_COMMODITY",
                Message = "Commodity name is required",
                UserFriendlyMessage = "Please provide a valid commodity name",
                SuggestedActions = new[] { "Specify a commodity like 'wheat', 'rice', or 'tomato'" },
                Timestamp = DateTimeOffset.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            });
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_LOCATION",
                Message = "Location is required",
                UserFriendlyMessage = "Please provide a valid location",
                SuggestedActions = new[] { "Specify a location like 'Delhi', 'Mumbai', or 'Indore'" },
                Timestamp = DateTimeOffset.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            });
        }


        try
        {
            // Parse time period
            var period = ParseTimePeriod(periodType, startDate, endDate);

            // Get historical prices
            var prices = await _dataAggregationService.GetHistoricalPricesAsync(
                commodity, location, period, cancellationToken);

            if (!prices.Any())
            {
                return NotFound(new ErrorResponse
                {
                    ErrorCode = "NO_DATA",
                    Message = "No historical data found",
                    UserFriendlyMessage = "No price data available for the specified period",
                    SuggestedActions = new[] { "Try a different time period", "Check if the commodity and location are correct" },
                    Timestamp = DateTimeOffset.UtcNow,
                    RequestId = HttpContext.TraceIdentifier
                });
            }


            // OPTIMIZED: Calculate trend from already-fetched prices (no redundant DynamoDB read)
            var trend = ((KisanMitraAI.Infrastructure.HistoricalAnalytics.DataAggregationService)_dataAggregationService)
                .CalculatePriceTrendFromData(prices);

            // Format for visualization
            var chartData = await _visualizationFormatter.FormatTimeSeriesDataAsync(
                trend,
                $"{commodity} Prices in {location}",
                "Date",
                "Price (₹)",
                cancellationToken);

            // Calculate moving average
            var movingAverage = await _visualizationFormatter.CalculateMovingAverageAsync(
                trend,
                7, // 7-day moving average
                $"{commodity} Price Trend (7-day MA)",
                cancellationToken);

            // Identify significant changes
            var significantChanges = await _visualizationFormatter.IdentifySignificantChangesAsync(
                trend,
                20f, // 20% threshold
                cancellationToken);

            // Handle comparison if requested
            ComparisonChartData? comparisonData = null;
            if (!string.IsNullOrWhiteSpace(compareWith))
            {
                var comparisonPeriod = ParseTimePeriod(compareWith, null, null);
                var comparison = await _dataAggregationService.ComparePeriodsAsync(
                    $"{commodity}_{location}",
                    new[] { period, comparisonPeriod },
                    async (id, p, ct) => await _dataAggregationService.GetHistoricalPricesAsync(
                        commodity, location, p, ct),
                    cancellationToken);

                comparisonData = await _visualizationFormatter.FormatComparisonDataAsync(
                    comparison,
                    $"{commodity} Price Comparison",
                    cancellationToken);
            }

            var response = new HistoricalPriceResponse(
                commodity,
                location,
                period,
                prices,
                trend,
                chartData,
                movingAverage,
                significantChanges,
                comparisonData);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical prices for {Commodity} at {Location}", commodity, location);
            return StatusCode(500, new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "Failed to retrieve historical prices",
                UserFriendlyMessage = "An error occurred while processing your request",
                SuggestedActions = new[] { "Please try again later", "Contact support if the problem persists" },
                Timestamp = DateTimeOffset.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            });

        }
    }

    /// <summary>
    /// Get historical soil health data for the authenticated farmer
    /// </summary>
    /// <param name="periodType">Time period type (last7days, lastYear, last10years, custom)</param>
    /// <param name="startDate">Start date for custom period</param>
    /// <param name="endDate">End date for custom period</param>
    /// <param name="nutrient">Optional specific nutrient to analyze (Nitrogen, Phosphorus, Potassium, pH, OrganicCarbon)</param>
    /// <param name="compareWith">Optional comparison period type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical soil data with trends and visualization</returns>
    [HttpGet("soil")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(HistoricalSoilResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetHistoricalSoilData(
        [FromQuery] string periodType = "lastYear",
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        [FromQuery] string? nutrient = null,
        [FromQuery] string? compareWith = null,
        CancellationToken cancellationToken = default)
    {
        var farmerId = User.GetFarmerId();
        
        _logger.LogInformation(
            "Historical soil data request from farmer {FarmerId}, period {PeriodType}, nutrient {Nutrient}",
            farmerId,
            periodType,
            nutrient ?? "all");

        try
        {
            // Parse time period
            var period = ParseTimePeriod(periodType, startDate, endDate);

            // Get historical soil data
            var soilData = await _dataAggregationService.GetHistoricalSoilDataAsync(
                farmerId,
                period,
                cancellationToken);

            if (!soilData.Any())
            {
                return NotFound(new ErrorResponse
                {
                    ErrorCode = "NO_DATA",
                    Message = "No historical soil data found",
                    UserFriendlyMessage = "No soil health data available for the specified period",
                    SuggestedActions = new[] { "Upload a Soil Health Card to start tracking", "Try a different time period" },
                    Timestamp = DateTimeOffset.UtcNow,
                    RequestId = HttpContext.TraceIdentifier
                });
            }


            // If specific nutrient requested, calculate trend for that nutrient
            TrendData<float>? nutrientTrend = null;
            ChartData? nutrientChart = null;
            if (!string.IsNullOrWhiteSpace(nutrient))
            {
                nutrientTrend = await _dataAggregationService.CalculateSoilNutrientTrendAsync(
                    farmerId,
                    nutrient,
                    period,
                    cancellationToken);

                nutrientChart = await _visualizationFormatter.FormatTimeSeriesDataAsync(
                    nutrientTrend,
                    $"{nutrient} Levels Over Time",
                    "Date",
                    GetNutrientUnit(nutrient),
                    cancellationToken);
            }

            // Handle comparison if requested
            ComparisonChartData? comparisonData = null;
            if (!string.IsNullOrWhiteSpace(compareWith) && !string.IsNullOrWhiteSpace(nutrient))
            {
                var comparisonPeriod = ParseTimePeriod(compareWith, null, null);
                var comparison = await _dataAggregationService.ComparePeriodsAsync(
                    $"{farmerId}_{nutrient}",
                    new[] { period, comparisonPeriod },
                    async (id, p, ct) =>
                    {
                        var data = await _dataAggregationService.GetHistoricalSoilDataAsync(farmerId, p, ct);
                        return data.Select(d => GetNutrientValue(d, nutrient));
                    },
                    cancellationToken);

                comparisonData = await _visualizationFormatter.FormatComparisonDataAsync(
                    comparison,
                    $"{nutrient} Comparison",
                    cancellationToken);
            }

            var response = new HistoricalSoilResponse(
                farmerId,
                period,
                soilData,
                nutrient,
                nutrientTrend,
                nutrientChart,
                comparisonData);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical soil data for farmer {FarmerId}", farmerId);
            return StatusCode(500, new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "Failed to retrieve historical soil data",
                UserFriendlyMessage = "An error occurred while processing your request",
                SuggestedActions = new[] { "Please try again later", "Contact support if the problem persists" },
                Timestamp = DateTimeOffset.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            });

        }
    }

    /// <summary>
    /// Get historical quality grading records for the authenticated farmer
    /// </summary>
    /// <param name="periodType">Time period type (last7days, lastSeason, lastYear, custom)</param>
    /// <param name="startDate">Start date for custom period</param>
    /// <param name="endDate">End date for custom period</param>
    /// <param name="produceType">Optional specific produce type to filter</param>
    /// <param name="compareWith">Optional comparison period type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical grading data with trends and visualization</returns>
    [HttpGet("grades")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(HistoricalGradingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetHistoricalGrades(
        [FromQuery] string periodType = "lastSeason",
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        [FromQuery] string? produceType = null,
        [FromQuery] string? compareWith = null,
        CancellationToken cancellationToken = default)
    {
        var farmerId = User.GetFarmerId();
        
        _logger.LogInformation(
            "Historical grading data request from farmer {FarmerId}, period {PeriodType}, produce {ProduceType}",
            farmerId,
            periodType,
            produceType ?? "all");

        try
        {
            // Parse time period
            var period = ParseTimePeriod(periodType, startDate, endDate);

            // Get historical grading data
            var gradingData = await _dataAggregationService.GetHistoricalGradingDataAsync(
                farmerId,
                period,
                cancellationToken);

            if (!gradingData.Any())
            {
                return NotFound(new ErrorResponse
                {
                    ErrorCode = "NO_DATA",
                    Message = "No historical grading data found",
                    UserFriendlyMessage = "No quality grading records available for the specified period",
                    SuggestedActions = new[] { "Grade some produce to start tracking", "Try a different time period" },
                    Timestamp = DateTimeOffset.UtcNow,
                    RequestId = HttpContext.TraceIdentifier
                });
            }


            // Filter by produce type if specified
            if (!string.IsNullOrWhiteSpace(produceType))
            {
                gradingData = gradingData.Where(g => g.ProduceType.Equals(produceType, StringComparison.OrdinalIgnoreCase));
            }

            // Calculate grade trend
            TrendData<QualityGrade>? gradeTrend = null;
            ChartData? gradeChart = null;
            if (!string.IsNullOrWhiteSpace(produceType))
            {
                gradeTrend = await _dataAggregationService.CalculateGradeTrendAsync(
                    farmerId,
                    produceType,
                    period,
                    cancellationToken);

                gradeChart = await _visualizationFormatter.FormatTimeSeriesDataAsync(
                    gradeTrend,
                    $"{produceType} Quality Grades Over Time",
                    "Date",
                    "Grade",
                    cancellationToken);
            }

            // Calculate grade distribution
            var gradeDistribution = gradingData
                .GroupBy(g => g.Grade)
                .Select(g => new GradeDistribution(g.Key, g.Count(), (decimal)g.Count() / gradingData.Count() * 100))
                .OrderByDescending(g => g.Count);

            // Calculate average certified price
            var avgCertifiedPrice = gradingData.Average(g => g.CertifiedPrice);

            // Handle comparison if requested
            ComparisonChartData? comparisonData = null;
            if (!string.IsNullOrWhiteSpace(compareWith) && !string.IsNullOrWhiteSpace(produceType))
            {
                var comparisonPeriod = ParseTimePeriod(compareWith, null, null);
                var comparison = await _dataAggregationService.ComparePeriodsAsync(
                    $"{farmerId}_{produceType}",
                    new[] { period, comparisonPeriod },
                    async (id, p, ct) => await _dataAggregationService.GetHistoricalGradingDataAsync(farmerId, p, ct),
                    cancellationToken);

                comparisonData = await _visualizationFormatter.FormatComparisonDataAsync(
                    comparison,
                    $"{produceType} Grade Comparison",
                    cancellationToken);
            }

            var response = new HistoricalGradingResponse(
                farmerId,
                period,
                gradingData,
                produceType,
                gradeTrend,
                gradeChart,
                gradeDistribution,
                avgCertifiedPrice,
                comparisonData);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical grading data for farmer {FarmerId}", farmerId);
            return StatusCode(500, new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "Failed to retrieve historical grading data",
                UserFriendlyMessage = "An error occurred while processing your request",
                SuggestedActions = new[] { "Please try again later", "Contact support if the problem persists" },
                Timestamp = DateTimeOffset.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            });

        }
    }

    /// <summary>
    /// Get AI-generated insights from historical data patterns
    /// </summary>
    /// <param name="dataType">Type of data to analyze (prices, soil, grades, all)</param>
    /// <param name="periodType">Time period type (requires at least 2 years for pattern detection)</param>
    /// <param name="startDate">Start date for custom period</param>
    /// <param name="endDate">End date for custom period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI-generated insights and action suggestions</returns>
    [HttpGet("insights")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(InsightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetInsights(
        [FromQuery] string dataType = "all",
        [FromQuery] string periodType = "last2years",
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var farmerId = User.GetFarmerId();
        
        _logger.LogInformation(
            "Insights request from farmer {FarmerId}, dataType {DataType}, period {PeriodType}",
            farmerId,
            dataType,
            periodType);

        try
        {
            // Parse time period
            var period = ParseTimePeriod(periodType, startDate, endDate);

            // Validate minimum 2 years for pattern detection
            var periodDuration = period.EndDate - period.StartDate;
            if (periodDuration.TotalDays < 730) // 2 years
            {
                return BadRequest(new ErrorResponse
                {
                    ErrorCode = "INSUFFICIENT_DATA_PERIOD",
                    Message = "Insufficient time period for insights",
                    UserFriendlyMessage = "Pattern detection requires at least 2 years of data",
                    SuggestedActions = new[] { "Select a longer time period (at least 2 years)", "Use 'last2years' or 'last10years' period type" },
                    Timestamp = DateTimeOffset.UtcNow,
                    RequestId = HttpContext.TraceIdentifier
                });
            }


            var allInsights = new List<Insight>();
            var allPatterns = new List<DataPattern>();
            var allSuggestions = new List<ActionSuggestion>();
            var trendAnalyses = new List<TrendAnalysis>();

            // Analyze soil data if requested
            if (dataType == "all" || dataType == "soil")
            {
                var soilData = await _dataAggregationService.GetHistoricalSoilDataAsync(farmerId, period, cancellationToken);
                if (soilData.Any())
                {
                    // Analyze organic carbon trend
                    var ocTrend = await _dataAggregationService.CalculateSoilNutrientTrendAsync(
                        farmerId, "OrganicCarbon", period, cancellationToken);
                    
                    var ocPatterns = await _insightsGenerator.DetectPatternsAsync(ocTrend, "OrganicCarbon", cancellationToken);
                    var ocInsights = await _insightsGenerator.GenerateInsightsAsync(ocTrend, farmerId, "OrganicCarbon", cancellationToken);
                    var ocAnalysis = await _insightsGenerator.AnalyzeTrendAsync(ocTrend, cancellationToken);
                    var ocSuggestions = await _insightsGenerator.SuggestActionsAsync(ocTrend, farmerId, "OrganicCarbon", cancellationToken);

                    allPatterns.AddRange(ocPatterns);
                    allInsights.AddRange(ocInsights);
                    trendAnalyses.Add(ocAnalysis);
                    allSuggestions.AddRange(ocSuggestions);
                }
            }

            // Analyze grading data if requested
            if (dataType == "all" || dataType == "grades")
            {
                var gradingData = await _dataAggregationService.GetHistoricalGradingDataAsync(farmerId, period, cancellationToken);
                if (gradingData.Any())
                {
                    // Group by produce type and analyze each
                    var produceTypes = gradingData.Select(g => g.ProduceType).Distinct();
                    foreach (var produceType in produceTypes)
                    {
                        var gradeTrend = await _dataAggregationService.CalculateGradeTrendAsync(
                            farmerId, produceType, period, cancellationToken);
                        
                        var gradePatterns = await _insightsGenerator.DetectPatternsAsync(gradeTrend, $"Grade_{produceType}", cancellationToken);
                        var gradeInsights = await _insightsGenerator.GenerateInsightsAsync(gradeTrend, farmerId, $"Grade_{produceType}", cancellationToken);
                        var gradeAnalysis = await _insightsGenerator.AnalyzeTrendAsync(gradeTrend, cancellationToken);
                        var gradeSuggestions = await _insightsGenerator.SuggestActionsAsync(gradeTrend, farmerId, $"Grade_{produceType}", cancellationToken);

                        allPatterns.AddRange(gradePatterns);
                        allInsights.AddRange(gradeInsights);
                        trendAnalyses.Add(gradeAnalysis);
                        allSuggestions.AddRange(gradeSuggestions);
                    }
                }
            }

            if (!allInsights.Any())
            {
                return NotFound(new ErrorResponse
                {
                    ErrorCode = "NO_DATA",
                    Message = "Insufficient data for insights",
                    UserFriendlyMessage = "Not enough historical data to generate meaningful insights",
                    SuggestedActions = new[] { "Continue using the platform to build historical data", "Try again after collecting more data over time" },
                    Timestamp = DateTimeOffset.UtcNow,
                    RequestId = HttpContext.TraceIdentifier
                });

            }

            // Get regional benchmarks for context
            RegionalBenchmark? regionalBenchmark = null;
            try
            {
                var soilData = await _dataAggregationService.GetHistoricalSoilDataAsync(farmerId, period, cancellationToken);
                if (soilData.Any())
                {
                    var latestSoil = soilData.OrderByDescending(s => s.TestDate).First();
                    regionalBenchmark = await _benchmarkAggregator.GetRegionalBenchmarkAsync(
                        latestSoil.Location,
                        "soil",
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve regional benchmark for farmer {FarmerId}", farmerId);
                // Continue without benchmark
            }

            var response = new InsightsResponse(
                farmerId,
                period,
                dataType,
                allInsights.OrderByDescending(i => i.Confidence),
                allPatterns.OrderByDescending(p => p.Confidence),
                trendAnalyses,
                allSuggestions.OrderByDescending(s => s.Priority),
                regionalBenchmark);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating insights for farmer {FarmerId}", farmerId);
            return StatusCode(500, new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "Failed to generate insights",
                UserFriendlyMessage = "An error occurred while analyzing your data",
                SuggestedActions = new[] { "Please try again later", "Contact support if the problem persists" },
                Timestamp = DateTimeOffset.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            });

        }
    }

    #region Helper Methods

    private TimePeriod ParseTimePeriod(string periodType, DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        return periodType.ToLowerInvariant() switch
        {
            "last7days" => TimePeriod.LastDays(7),
            "last30days" => TimePeriod.LastDays(30),
            "lastseason" => TimePeriod.LastMonths(4), // Approximate season as 4 months
            "lastmonth" => TimePeriod.LastMonths(1),
            "last3months" => TimePeriod.LastMonths(3),
            "last6months" => TimePeriod.LastMonths(6),
            "lastyear" => TimePeriod.LastYears(1),
            "last2years" => TimePeriod.LastYears(2),
            "last5years" => TimePeriod.LastYears(5),
            "last10years" => TimePeriod.LastYears(10),
            "custom" when startDate.HasValue && endDate.HasValue => 
                TimePeriod.Custom(startDate.Value, endDate.Value, "Custom Period"),
            _ => TimePeriod.LastDays(7) // Default to last 7 days
        };
    }

    private string GetNutrientUnit(string nutrient)
    {
        return nutrient.ToLowerInvariant() switch
        {
            "ph" => "pH",
            "nitrogen" => "kg/ha",
            "phosphorus" => "kg/ha",
            "potassium" => "kg/ha",
            "organiccarbon" => "%",
            "sulfur" => "ppm",
            "zinc" => "ppm",
            "boron" => "ppm",
            "iron" => "ppm",
            "manganese" => "ppm",
            "copper" => "ppm",
            _ => "units"
        };
    }

    private float GetNutrientValue(SoilHealthData data, string nutrient)
    {
        return nutrient.ToLowerInvariant() switch
        {
            "nitrogen" => data.Nitrogen,
            "phosphorus" => data.Phosphorus,
            "potassium" => data.Potassium,
            "ph" => data.pH,
            "organiccarbon" => data.OrganicCarbon,
            "sulfur" => data.Sulfur,
            "zinc" => data.Zinc,
            "boron" => data.Boron,
            "iron" => data.Iron,
            "manganese" => data.Manganese,
            "copper" => data.Copper,
            _ => 0f
        };
    }

    #endregion
}

#region Response Models

/// <summary>
/// Response model for historical price data
/// </summary>
public record HistoricalPriceResponse(
    string Commodity,
    string Location,
    TimePeriod Period,
    IEnumerable<MandiPrice> Prices,
    TrendData<decimal> Trend,
    ChartData ChartData,
    ChartData MovingAverage,
    IEnumerable<SignificantChange> SignificantChanges,
    ComparisonChartData? Comparison);

/// <summary>
/// Response model for historical soil data
/// </summary>
public record HistoricalSoilResponse(
    string FarmerId,
    TimePeriod Period,
    IEnumerable<SoilHealthData> SoilData,
    string? AnalyzedNutrient,
    TrendData<float>? NutrientTrend,
    ChartData? NutrientChart,
    ComparisonChartData? Comparison);

/// <summary>
/// Response model for historical grading data
/// </summary>
public record HistoricalGradingResponse(
    string FarmerId,
    TimePeriod Period,
    IEnumerable<GradingRecord> GradingRecords,
    string? ProduceType,
    TrendData<QualityGrade>? GradeTrend,
    ChartData? GradeChart,
    IEnumerable<GradeDistribution> GradeDistribution,
    decimal AverageCertifiedPrice,
    ComparisonChartData? Comparison);

/// <summary>
/// Response model for insights
/// </summary>
public record InsightsResponse(
    string FarmerId,
    TimePeriod Period,
    string DataType,
    IEnumerable<Insight> Insights,
    IEnumerable<DataPattern> Patterns,
    IEnumerable<TrendAnalysis> TrendAnalyses,
    IEnumerable<ActionSuggestion> Suggestions,
    RegionalBenchmark? RegionalBenchmark);

/// <summary>
/// Grade distribution model
/// </summary>
public record GradeDistribution(
    QualityGrade Grade,
    int Count,
    decimal Percentage);

/// <summary>
/// Error response model
/// </summary>
//public record ErrorResponse(
//    string ErrorCode,
//    string Message,
//    string UserFriendlyMessage,
//    IEnumerable<string> SuggestedActions,
//    DateTimeOffset Timestamp,
//    string RequestId
//    );

#endregion
