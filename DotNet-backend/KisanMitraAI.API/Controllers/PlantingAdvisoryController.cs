using KisanMitraAI.Core.Authorization;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.PlantingAdvisory;
using KisanMitraAI.Infrastructure.Storage.S3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.Text.Json;

namespace KisanMitraAI.API.Controllers;

/// <summary>
/// Planting advisory controller for predictive planting recommendations (Sowing Oracle module)
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
//[Authorize]
//[RequiresFarmer]
public class PlantingAdvisoryController : ControllerBase
{
    private readonly IWeatherDataCollector _weatherDataCollector;
    private readonly ISoilDataRetriever _soilDataRetriever;
    private readonly IPlantingWindowAnalyzer _plantingWindowAnalyzer;
    private readonly ISeedVarietyRecommender _seedVarietyRecommender;
    private readonly IS3StorageService _s3StorageService;
    private readonly ILogger<PlantingAdvisoryController> _logger;

    public PlantingAdvisoryController(
        IWeatherDataCollector weatherDataCollector,
        ISoilDataRetriever soilDataRetriever,
        IPlantingWindowAnalyzer plantingWindowAnalyzer,
        ISeedVarietyRecommender seedVarietyRecommender,
        IS3StorageService s3StorageService,
        ILogger<PlantingAdvisoryController> logger)
    {
        _weatherDataCollector = weatherDataCollector;
        _soilDataRetriever = soilDataRetriever;
        _plantingWindowAnalyzer = plantingWindowAnalyzer;
        _seedVarietyRecommender = seedVarietyRecommender;
        _s3StorageService = s3StorageService;
        _logger = logger;
    }

    /// <summary>
    /// Get planting recommendations for a specific crop and location
    /// </summary>
    /// <param name="request">Planting recommendation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Planting recommendation with windows, varieties, and confidence scores</returns>
    [HttpPost("recommend")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(PlantingRecommendationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPlantingRecommendation(
        [FromBody] PlantingRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId();
        var language = !string.IsNullOrEmpty(request.Language) ? request.Language : User.GetPreferredLanguage();
        
        _logger.LogInformation(
            "Planting recommendation request received from farmer {FarmerId} for crop {CropType} at location {Location}",
            farmerId,
            request.CropType,
            request.Location);

        // Validate crop type
        if (string.IsNullOrWhiteSpace(request.CropType))
        {
            _logger.LogWarning("Missing crop type parameter from farmer {FarmerId}", farmerId);
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "CROP_TYPE_REQUIRED",
                Message = "Crop type parameter is required",
                UserFriendlyMessage = "कृपया फसल का प्रकार चुनें (Please select the crop type)",
                SuggestedActions = new[] { "Specify the type of crop you want to plant (e.g., wheat, rice, cotton)" }
            });
        }

        // Validate location
        if (string.IsNullOrWhiteSpace(request.Location))
        {
            _logger.LogWarning("Missing location parameter from farmer {FarmerId}", farmerId);
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "LOCATION_REQUIRED",
                Message = "Location parameter is required",
                UserFriendlyMessage = "कृपया स्थान चुनें (Please select the location)",
                SuggestedActions = new[] { "Specify your farm location (city/district)" }
            });
        }

        // Validate forecast days
        var daysAhead = request.ForecastDays ?? 90;
        if (daysAhead < 1 || daysAhead > 90)
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_FORECAST_DAYS",
                Message = "Forecast days must be between 1 and 90",
                UserFriendlyMessage = "पूर्वानुमान दिन 1 से 90 के बीच होने चाहिए (Forecast days must be between 1 and 90)",
                SuggestedActions = new[] { "Adjust the forecast days to a value between 1 and 90" }
            });
        }

        try
        {
            // OPTIMIZED: Parallel fetch — weather API and DynamoDB soil data have zero dependency.
            // Saves 500ms-2s (the shorter of the two calls runs free).
            _logger.LogInformation(
                "Fetching weather + soil data in parallel for farmer {FarmerId}, location {Location}",
                farmerId, request.Location);

            var weatherTask = _weatherDataCollector.GetForecastAsync(
                request.Location, daysAhead, cancellationToken);
            var soilTask = _soilDataRetriever.GetLatestSoilDataAsync(
                farmerId, cancellationToken);

            await Task.WhenAll(weatherTask, soilTask);

            var weatherForecast = await weatherTask;
            var soilData = await soilTask;

            // Soil data is OPTIONAL — if farmer hasn't uploaded Soil Health Card,
            // use regional defaults based on location. Most Indian farmers (~70%) don't have SHC.
            var usedDefaultSoil = false;
            if (soilData == null)
            {
                _logger.LogInformation(
                    "No soil data for farmer {FarmerId}, using regional defaults for {Location}",
                    farmerId, request.Location);
                soilData = CreateDefaultSoilData(farmerId, request.Location);
                usedDefaultSoil = true;
            }

            // Analyze planting windows
            _logger.LogInformation(
                "Analyzing planting windows for crop {CropType}",
                request.CropType);

            var plantingWindows = await _plantingWindowAnalyzer.AnalyzePlantingWindowsAsync(
                weatherForecast,
                soilData,
                request.CropType,
                cancellationToken,
                language);

            var windowsList = plantingWindows.ToList();

            if (!windowsList.Any())
            {
                _logger.LogWarning(
                    "No suitable planting windows found for farmer {FarmerId}, crop {CropType}",
                    farmerId,
                    request.CropType);

                return Ok(new PlantingRecommendationResponse
                {
                    PlantingWindows = Array.Empty<PlantingWindow>(),
                    SeedRecommendations = Array.Empty<SeedRecommendation>(),
                    Message = "वर्तमान मौसम की स्थिति के आधार पर कोई उपयुक्त रोपण खिड़की नहीं मिली (No suitable planting windows found based on current weather conditions)",
                    HasRecommendations = false
                });
            }

            // Get seed variety recommendations for the best planting window
            var bestWindow = windowsList.OrderByDescending(w => w.ConfidenceScore).First();

            _logger.LogInformation(
                "Recommending seed varieties for best planting window ({Start:yyyy-MM-dd} to {End:yyyy-MM-dd})",
                bestWindow.StartDate,
                bestWindow.EndDate);

            var seedRecommendations = await _seedVarietyRecommender.RecommendVarietiesAsync(
                bestWindow,
                soilData,
                request.CropType,
                cancellationToken,
                language);

            _logger.LogInformation(
                "Planting recommendations generated successfully for farmer {FarmerId}. Windows: {WindowCount}, Varieties: {VarietyCount}",
                farmerId,
                windowsList.Count,
                seedRecommendations.Count());

            return Ok(new PlantingRecommendationResponse
            {
                PlantingWindows = windowsList,
                SeedRecommendations = seedRecommendations,
                Message = usedDefaultSoil
                    ? "सिफारिशें अनुमानित मृदा डेटा पर आधारित हैं। बेहतर सटीकता के लिए अपना मृदा स्वास्थ्य कार्ड अपलोड करें (Recommendations based on estimated soil data. Upload your Soil Health Card for better accuracy)"
                    : "रोपण सिफारिशें सफलतापूर्वक उत्पन्न की गईं (Planting recommendations generated successfully)",
                HasRecommendations = true,
                WeatherFetchedAt = weatherForecast.FetchedAt,
                SoilDataDate = soilData.TestDate,
                UsedDefaultSoil = usedDefaultSoil,
                DataSources = new DataSourceInfo
                {
                    RealForecastDays = weatherForecast.RealForecastDays,
                    HistoricalProjectionDays = weatherForecast.HistoricalProjectionDays,
                    SoilMoistureSource = "estimated_from_humidity_rainfall",
                    SoilNutrientSource = usedDefaultSoil ? "regional_default" : "soil_health_card",
                    Disclaimer = "मिट्टी की नमी अनुमानित है। बुआई से पहले हाथ से मिट्टी की जांच करें। " +
                        (usedDefaultSoil ? "मृदा पोषक तत्व क्षेत्रीय औसत पर आधारित हैं — बेहतर सटीकता के लिए मृदा स्वास्थ्य कार्ड अपलोड करें।" : "")
                }
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Planting recommendation processing cancelled for farmer {FarmerId}", farmerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error generating planting recommendations for farmer {FarmerId}",
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "SERVICE_UNAVAILABLE",
                Message = "An error occurred while generating planting recommendations",
                UserFriendlyMessage = "रोपण सिफारिशें उत्पन्न करते समय एक त्रुटि हुई। कृपया पुनः प्रयास करें (An error occurred while generating planting recommendations. Please try again)",
                SuggestedActions = new[] { "Try again in a few moments", "Check your internet connection", "Contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Get planting recommendations using soil data from a saved plan
    /// </summary>
    /// <param name="request">Planting recommendation request with plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Planting recommendation with windows, varieties, and confidence scores</returns>
    [HttpPost("recommend-from-plan")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(PlantingRecommendationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPlantingRecommendationFromPlan(
        [FromBody] PlantingRecommendationFromPlanRequest request,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "a47854d8-40a1-709a-edfa-8c09543849a1";
        var language = !string.IsNullOrEmpty(request.Language) ? request.Language : User.GetPreferredLanguage();
        
        _logger.LogInformation(
            "Planting recommendation request from saved plan {PlanId} received from farmer {FarmerId} for crop {CropType}",
            request.PlanId,
            farmerId,
            request.CropType);

        // Validate plan ID
        if (string.IsNullOrWhiteSpace(request.PlanId))
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "PLAN_ID_REQUIRED",
                Message = "Plan ID is required",
                UserFriendlyMessage = "कृपया एक सहेजी गई योजना चुनें (Please select a saved plan)",
                SuggestedActions = new[] { "Select a saved soil analysis plan from the dropdown" }
            });
        }

        // Validate crop type
        if (string.IsNullOrWhiteSpace(request.CropType))
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "CROP_TYPE_REQUIRED",
                Message = "Crop type parameter is required",
                UserFriendlyMessage = "कृपया फसल का प्रकार चुनें (Please select the crop type)",
                SuggestedActions = new[] { "Specify the type of crop you want to plant" }
            });
        }

        // Validate location
        if (string.IsNullOrWhiteSpace(request.Location))
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "LOCATION_REQUIRED",
                Message = "Location parameter is required",
                UserFriendlyMessage = "कृपया स्थान चुनें (Please select the location)",
                SuggestedActions = new[] { "Specify your farm location" }
            });
        }

        var daysAhead = request.ForecastDays ?? 90;
        if (daysAhead < 1 || daysAhead > 90)
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_FORECAST_DAYS",
                Message = "Forecast days must be between 1 and 90",
                UserFriendlyMessage = "पूर्वानुमान दिन 1 से 90 के बीच होने चाहिए",
                SuggestedActions = new[] { "Adjust the forecast days to a value between 1 and 90" }
            });
        }

        try
        {
            // Retrieve soil data from the saved plan
            _logger.LogInformation(
                "Retrieving soil data from saved plan {PlanId} for farmer {FarmerId}",
                request.PlanId,
                farmerId);

            var soilData = await _soilDataRetriever.GetSoilDataFromPlanAsync(
                farmerId,
                request.PlanId,
                cancellationToken);

            if (soilData == null)
            {
                _logger.LogWarning(
                    "Saved plan {PlanId} not found for farmer {FarmerId}",
                    request.PlanId,
                    farmerId);

                return BadRequest(new ErrorResponse
                {
                    ErrorCode = "PLAN_NOT_FOUND",
                    Message = "The selected plan was not found",
                    UserFriendlyMessage = "चयनित योजना नहीं मिली। कृपया दूसरी योजना चुनें (Selected plan not found. Please select another plan)",
                    SuggestedActions = new[] { "Select a different saved plan", "Create a new soil analysis" }
                });
            }

            // Retrieve weather forecast
            _logger.LogInformation(
                "Fetching weather forecast for location {Location}, {Days} days ahead",
                request.Location,
                daysAhead);

            var weatherForecast = await _weatherDataCollector.GetForecastAsync(
                request.Location,
                daysAhead,
                cancellationToken);

            // Analyze planting windows
            _logger.LogInformation(
                "Analyzing planting windows for crop {CropType} using plan {PlanId}",
                request.CropType,
                request.PlanId);

            var plantingWindows = await _plantingWindowAnalyzer.AnalyzePlantingWindowsAsync(
                weatherForecast,
                soilData,
                request.CropType,
                cancellationToken,
                language);

            var windowsList = plantingWindows.ToList();

            if (!windowsList.Any())
            {
                return Ok(new PlantingRecommendationResponse
                {
                    PlantingWindows = Array.Empty<PlantingWindow>(),
                    SeedRecommendations = Array.Empty<SeedRecommendation>(),
                    Message = "वर्तमान मौसम की स्थिति के आधार पर कोई उपयुक्त रोपण खिड़की नहीं मिली",
                    HasRecommendations = false
                });
            }

            // Get seed variety recommendations
            var bestWindow = windowsList.OrderByDescending(w => w.ConfidenceScore).First();

            var seedRecommendations = await _seedVarietyRecommender.RecommendVarietiesAsync(
                bestWindow,
                soilData,
                request.CropType,
                cancellationToken,
                language);

            _logger.LogInformation(
                "Planting recommendations generated successfully from plan {PlanId} for farmer {FarmerId}",
                request.PlanId,
                farmerId);

            return Ok(new PlantingRecommendationResponse
            {
                PlantingWindows = windowsList,
                SeedRecommendations = seedRecommendations,
                Message = "रोपण सिफारिशें सफलतापूर्वक उत्पन्न की गईं (Planting recommendations generated successfully)",
                HasRecommendations = true,
                WeatherFetchedAt = weatherForecast.FetchedAt,
                SoilDataDate = soilData.TestDate,
                UsedPlanId = request.PlanId
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Planting recommendation processing cancelled for farmer {FarmerId}", farmerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error generating planting recommendations from plan {PlanId} for farmer {FarmerId}",
                request.PlanId,
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "SERVICE_UNAVAILABLE",
                Message = "An error occurred while generating planting recommendations",
                UserFriendlyMessage = "रोपण सिफारिशें उत्पन्न करते समय एक त्रुटि हुई। कृपया पुनः प्रयास करें",
                SuggestedActions = new[] { "Try again in a few moments", "Contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Save a planting recommendation to S3
    /// </summary>
    /// <param name="request">The recommendation to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved recommendation confirmation</returns>
    [HttpPost("recommendations/save")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(SavedRecommendationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SaveRecommendation(
        [FromBody] SaveRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "test-farmer-123";

        _logger.LogInformation(
            "Save recommendation request received from farmer {FarmerId}",
            farmerId);

        try
        {
            var recommendationJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(recommendationJson));
            var s3Key = await _s3StorageService.UploadAsync(
                stream,
                $"planting-recommendation-{request.RecommendationId}.json",
                farmerId,
                "application/json",
                cancellationToken);

            _logger.LogInformation(
                "Recommendation {RecommendationId} saved successfully for farmer {FarmerId} at {S3Key}",
                request.RecommendationId,
                farmerId,
                s3Key);

            return Ok(new SavedRecommendationResponse
            {
                RecommendationId = request.RecommendationId,
                S3Key = s3Key,
                SavedAt = DateTimeOffset.UtcNow,
                Message = "सिफारिश सफलतापूर्वक सहेजी गई (Recommendation saved successfully)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error saving recommendation {RecommendationId} for farmer {FarmerId}",
                request.RecommendationId,
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "RECOMMENDATION_SAVE_FAILED",
                Message = "An error occurred while saving your recommendation",
                UserFriendlyMessage = "आपकी सिफारिश सहेजते समय एक त्रुटि हुई (An error occurred while saving your recommendation)",
                SuggestedActions = new[] { "Try again in a few moments", "Contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Get all saved recommendations for the authenticated farmer
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of saved recommendations</returns>
    [HttpGet("recommendations")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(IEnumerable<SaveRecommendationRequest>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSavedRecommendations(CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "test-farmer-123";

        _logger.LogInformation(
            "Get saved recommendations request from farmer {FarmerId}",
            farmerId);

        try
        {
            // List all recommendation files for this farmer
            var prefix = $"misc/{farmerId}/";
            var s3Keys = await _s3StorageService.ListObjectsAsync(prefix, cancellationToken);

            var recommendations = new List<SaveRecommendationRequest>();

            foreach (var s3Key in s3Keys.Where(k => k.Contains("planting-recommendation-") && k.EndsWith(".json")))
            {
                try
                {
                    using var stream = await _s3StorageService.DownloadAsync(s3Key, farmerId, cancellationToken);
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync(cancellationToken);
                    var recommendation = JsonSerializer.Deserialize<SaveRecommendationRequest>(json);
                    
                    if (recommendation != null)
                    {
                        recommendations.Add(recommendation);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to load recommendation from {S3Key} for farmer {FarmerId}",
                        s3Key,
                        farmerId);
                }
            }

            _logger.LogInformation(
                "Retrieved {Count} saved recommendations for farmer {FarmerId}",
                recommendations.Count,
                farmerId);

            return Ok(recommendations.OrderByDescending(r => r.SavedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving saved recommendations for farmer {FarmerId}",
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "RECOMMENDATIONS_RETRIEVAL_FAILED",
                Message = "An error occurred while retrieving your saved recommendations",
                UserFriendlyMessage = "आपकी सहेजी गई सिफारिशें प्राप्त करते समय एक त्रुटि हुई (An error occurred while retrieving your saved recommendations)",
                SuggestedActions = new[] { "Try again in a few moments", "Contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Delete a saved recommendation
    /// </summary>
    /// <param name="recommendationId">Recommendation ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("recommendations/{recommendationId}")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteRecommendation(
        string recommendationId,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "test-farmer-123";

        _logger.LogInformation(
            "Delete recommendation {RecommendationId} request from farmer {FarmerId}",
            recommendationId,
            farmerId);

        try
        {
            // List all recommendation files for this farmer to find the matching one
            var prefix = $"misc/{farmerId}/";
            var s3Keys = await _s3StorageService.ListObjectsAsync(prefix, cancellationToken);

            var recommendationKey = s3Keys.FirstOrDefault(k => k.Contains($"planting-recommendation-{recommendationId}.json"));

            if (recommendationKey == null)
            {
                return NotFound(new ErrorResponse
                {
                    ErrorCode = "RECOMMENDATION_NOT_FOUND",
                    Message = $"Recommendation {recommendationId} not found",
                    UserFriendlyMessage = "सिफारिश नहीं मिली (Recommendation not found)",
                    SuggestedActions = new[] { "Check the recommendation ID and try again" }
                });
            }

            await _s3StorageService.DeleteAsync(recommendationKey, cancellationToken);

            _logger.LogInformation(
                "Recommendation {RecommendationId} deleted successfully for farmer {FarmerId}",
                recommendationId,
                farmerId);

            return Ok(new
            {
                Message = "सिफारिश सफलतापूर्वक हटाई गई (Recommendation deleted successfully)",
                RecommendationId = recommendationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting recommendation {RecommendationId} for farmer {FarmerId}",
                recommendationId,
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "RECOMMENDATION_DELETION_FAILED",
                Message = "An error occurred while deleting the recommendation",
                UserFriendlyMessage = "सिफारिश हटाते समय एक त्रुटि हुई (An error occurred while deleting the recommendation)",
                SuggestedActions = new[] { "Try again in a few moments", "Contact support if the problem persists" }
            });
        }
    }
    /// <summary>
    /// Creates default soil data based on typical Indian agricultural soil profiles.
    /// Used when farmer hasn't uploaded a Soil Health Card.
    /// Values represent median alluvial soil (most common in Indian agriculture).
    /// </summary>
    private static SoilHealthData CreateDefaultSoilData(string? farmerId, string location)
    {
        return new SoilHealthData(
            farmerId: farmerId ?? "unknown",
            location: location,
            nitrogen: 250f,        // Medium (Low<280, Med 280-560, High>560)
            phosphorus: 15f,       // Medium (Low<11, Med 11-22, High>22)
            potassium: 180f,       // Medium (Low<110, Med 110-280, High>280)
            pH: 7.0f,              // Neutral (most crops thrive 6.0-7.5)
            organicCarbon: 0.5f,   // Low-Medium boundary
            sulfur: 12f,           // Adequate
            zinc: 1.2f,            // Adequate (>0.6 ppm)
            boron: 0.5f,           // Adequate (>0.5 ppm)
            iron: 5.0f,            // Adequate (>4.5 ppm)
            manganese: 3.0f,       // Adequate (>2.0 ppm)
            copper: 0.5f,          // Adequate (>0.2 ppm)
            testDate: DateTime.UtcNow,
            labId: "DEFAULT-ESTIMATE"
        );
    }
}

/// <summary>
/// Request for planting recommendations
/// </summary>
public record PlantingRecommendationRequest
{
    public string CropType { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public int? ForecastDays { get; init; }
    public string? Language { get; init; }
}

/// <summary>
/// Response with planting recommendations
/// </summary>
public record PlantingRecommendationResponse
{
    public IEnumerable<PlantingWindow> PlantingWindows { get; init; } = Array.Empty<PlantingWindow>();
    public IEnumerable<SeedRecommendation> SeedRecommendations { get; init; } = Array.Empty<SeedRecommendation>();
    public string Message { get; init; } = string.Empty;
    public bool HasRecommendations { get; init; }
    public DateTimeOffset? WeatherFetchedAt { get; init; }
    public DateTimeOffset? SoilDataDate { get; init; }
    public string? UsedPlanId { get; init; }
    public bool UsedDefaultSoil { get; init; }
    public DataSourceInfo? DataSources { get; init; }
}

public record DataSourceInfo
{
    public int RealForecastDays { get; init; }
    public int HistoricalProjectionDays { get; init; }
    public string SoilMoistureSource { get; init; } = "estimated";
    public string SoilNutrientSource { get; init; } = "soil_health_card";
    public string Disclaimer { get; init; } = string.Empty;
}

/// <summary>
/// Request for planting recommendations using a saved soil plan
/// </summary>
public record PlantingRecommendationFromPlanRequest
{
    public string PlanId { get; init; } = string.Empty;
    public string CropType { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public int? ForecastDays { get; init; }
    public string? Language { get; init; }
}

/// <summary>
/// Request to save a planting recommendation
/// </summary>
public record SaveRecommendationRequest
{
    public string RecommendationId { get; init; } = string.Empty;
    public string CropType { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public IEnumerable<PlantingWindow> PlantingWindows { get; init; } = Array.Empty<PlantingWindow>();
    public IEnumerable<SeedRecommendation> SeedRecommendations { get; init; } = Array.Empty<SeedRecommendation>();
    public DateTimeOffset? WeatherFetchedAt { get; init; }
    public DateTimeOffset? SoilDataDate { get; init; }
    public string SavedAt { get; init; } = string.Empty;
}

/// <summary>
/// Response for saved recommendation
/// </summary>
public record SavedRecommendationResponse
{
    public string RecommendationId { get; init; } = string.Empty;
    public string S3Key { get; init; } = string.Empty;
    public DateTimeOffset SavedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}
