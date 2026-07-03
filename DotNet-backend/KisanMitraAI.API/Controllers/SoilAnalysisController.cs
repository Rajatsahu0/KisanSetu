using KisanMitraAI.Core.Authorization;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.SoilAnalysis;
using KisanMitraAI.Infrastructure.Repositories.Timestream;
using KisanMitraAI.Infrastructure.Storage.S3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.Text.Json;

namespace KisanMitraAI.API.Controllers;

/// <summary>
/// Soil analysis controller for Soil Health Card digitization (Dhara-Analyzer module)
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
//[RequiresFarmer]
public class SoilAnalysisController : ControllerBase
{
    private readonly IDocumentUploadHandler _documentUploadHandler;
    private readonly ITextExtractor _textExtractor;
    private readonly ISoilDataParser _soilDataParser;
    private readonly IRegenerativePlanGenerator _planGenerator;
    private readonly ISoilDataRepository _soilDataRepository;
    private readonly IS3StorageService _s3StorageService;
    private readonly ILogger<SoilAnalysisController> _logger;

    public SoilAnalysisController(
        IDocumentUploadHandler documentUploadHandler,
        ITextExtractor textExtractor,
        ISoilDataParser soilDataParser,
        IRegenerativePlanGenerator planGenerator,
        ISoilDataRepository soilDataRepository,
        IS3StorageService s3StorageService,
        ILogger<SoilAnalysisController> logger)
    {
        _documentUploadHandler = documentUploadHandler;
        _textExtractor = textExtractor;
        _soilDataParser = soilDataParser;
        _planGenerator = planGenerator;
        _soilDataRepository = soilDataRepository;
        _s3StorageService = s3StorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload and digitize a Soil Health Card
    /// </summary>
    /// <param name="cardImage">Image or PDF of the Soil Health Card</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Digitized soil health data</returns>
    [HttpPost("upload-card")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(SoilHealthCardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max
    public async Task<IActionResult> UploadSoilHealthCard(
        [FromForm] IFormFile cardImage,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId();
        
        _logger.LogInformation(
            "Soil Health Card upload request received from farmer {FarmerId}",
            farmerId);

        // Validate document file
        if (cardImage == null || cardImage.Length == 0)
        {
            _logger.LogWarning("Empty document file received from farmer {FarmerId}", farmerId);
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "DOCUMENT_FILE_REQUIRED",
                Message = "Document file is required",
                UserFriendlyMessage = "कृपया मृदा स्वास्थ्य कार्ड की छवि अपलोड करें (Please upload an image of your Soil Health Card)",
                SuggestedActions = new[] { "Upload a valid image or PDF file of your Soil Health Card" }
            });
        }

        // Validate document format
        var allowedFormats = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".txt" };
        var fileExtension = Path.GetExtension(cardImage.FileName).ToLowerInvariant();
        
        if (!allowedFormats.Contains(fileExtension))
        {
            _logger.LogWarning(
                "Invalid document format {Format} received from farmer {FarmerId}",
                fileExtension,
                farmerId);
            
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "DOCUMENT_FORMAT_INVALID",
                Message = $"Invalid document format: {fileExtension}",
                UserFriendlyMessage = "कृपया JPEG, PNG, PDF, या TXT प्रारूप में दस्तावेज़ अपलोड करें (Please upload document in JPEG, PNG, PDF, or TXT format)",
                SuggestedActions = new[] { "Convert your document to JPEG, PNG, PDF, or TXT format and try again" }
            });
        }

        // Validate file size (max 10 MB)
        if (cardImage.Length > 10 * 1024 * 1024)
        {
            _logger.LogWarning(
                "Document file too large ({Size} bytes) from farmer {FarmerId}",
                cardImage.Length,
                farmerId);
            
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "DOCUMENT_FILE_TOO_LARGE",
                Message = $"Document file size ({cardImage.Length} bytes) exceeds maximum allowed size (10 MB)",
                UserFriendlyMessage = "दस्तावेज़ फ़ाइल बहुत बड़ी है। कृपया 10 MB से छोटी फ़ाइल अपलोड करें (Document file is too large. Please upload a file smaller than 10 MB)",
                SuggestedActions = new[] { "Reduce document file size to under 10 MB", "Take a photo with lower resolution" }
            });
        }

        try
        {
            // Upload document
            using var documentStream = cardImage.OpenReadStream();
            var uploadResult = await _documentUploadHandler.UploadDocumentAsync(
                documentStream,
                farmerId,
                "SoilHealthCard",
                cancellationToken);

            _logger.LogInformation(
                "Document uploaded successfully for farmer {FarmerId}: {S3Key}",
                farmerId,
                uploadResult.DocumentS3Key);

            // Extract text from document
            var extractionResult = await _textExtractor.ExtractTextAsync(
                uploadResult.DocumentS3Key,
                cancellationToken);

            // Parse soil data
            var soilData = await _soilDataParser.ParseSoilDataAsync(
                extractionResult,
                cancellationToken);

            // Validate soil data
            var validationResult = await _soilDataParser.ValidateSoilDataAsync(
                soilData,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Soil data validation failed for farmer {FarmerId}. Errors: {ErrorCount}",
                    farmerId,
                    validationResult.Errors.Count);

                return Ok(new SoilHealthCardResponse
                {
                    SoilData = SoilHealthDataDto.FromDomain(soilData),
                    IsValid = false,
                    ValidationErrors = validationResult.Errors,
                    Message = "कुछ फ़ील्ड गुम या अमान्य हैं। कृपया मैन्युअल रूप से सत्यापित करें (Some fields are missing or invalid. Please verify manually)",
                    RequiresManualVerification = true
                });
            }

            // Store soil data
            await _soilDataRepository.StoreSoilDataAsync(soilData, cancellationToken);

            _logger.LogInformation(
                "Soil Health Card digitized successfully for farmer {FarmerId}",
                farmerId);

            return Ok(new SoilHealthCardResponse
            {
                SoilData = SoilHealthDataDto.FromDomain(soilData),
                IsValid = true,
                ValidationErrors = new List<ValidationError>(),
                Message = "मृदा स्वास्थ्य कार्ड सफलतापूर्वक डिजिटाइज़ किया गया (Soil Health Card digitized successfully)",
                RequiresManualVerification = false
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Soil card processing cancelled for farmer {FarmerId}", farmerId);
            throw;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not supported") || ex.Message.Contains("unsupported"))
        {
            _logger.LogError(
                ex,
                "Unsupported document format for farmer {FarmerId}",
                farmerId);

            return StatusCode(StatusCodes.Status400BadRequest, new ErrorResponse
            {
                ErrorCode = "DOCUMENT_FORMAT_UNSUPPORTED",
                Message = "The uploaded document format is not supported or the file may be corrupted",
                UserFriendlyMessage = "अपलोड किया गया दस्तावेज़ प्रारूप समर्थित नहीं है या फ़ाइल दूषित हो सकती है (The uploaded document format is not supported or the file may be corrupted)",
                SuggestedActions = new[] 
                { 
                    "Take a clear photo of your Soil Health Card using your camera",
                    "Ensure the file is a valid JPEG, PNG, PDF, or TXT",
                    "Try uploading a different format (JPEG, PNG, or TXT recommended)"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing Soil Health Card for farmer {FarmerId}",
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "SOIL_CARD_UNREADABLE",
                Message = "An error occurred while processing your Soil Health Card",
                UserFriendlyMessage = "आपके मृदा स्वास्थ्य कार्ड को संसाधित करते समय एक त्रुटि हुई। कृपया स्पष्ट छवि के साथ पुनः प्रयास करें (An error occurred while processing your Soil Health Card. Please try again with a clear image)",
                SuggestedActions = new[] { "Take a clearer photo in good lighting", "Ensure all text is visible", "Try again in a few moments" }
            });
        }
    }

    /// <summary>
    /// Generate a regenerative farming plan based on soil data
    /// </summary>
    /// <param name="request">Plan generation request with farm profile</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>12-month regenerative farming plan</returns>
    [HttpPost("generate-plan")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(RegenerativePlan), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateRegenerativePlan(
        [FromBody] PlanGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "a47854d8-40a1-709a-edfa-8c09543849a1";  // Use test farmer ID when not authenticated
        
        _logger.LogInformation(
            "Regenerative plan generation request received from farmer {FarmerId}",
            farmerId);

        // Validate request
        if (request.SoilData == null)
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "SOIL_DATA_REQUIRED",
                Message = "Soil data is required to generate a plan",
                UserFriendlyMessage = "योजना बनाने के लिए मृदा डेटा आवश्यक है (Soil data is required to generate a plan)",
                SuggestedActions = new[] { "Upload your Soil Health Card first", "Provide soil data in the request" }
            });
        }

        if (request.FarmProfile == null)
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "FARM_PROFILE_REQUIRED",
                Message = "Farm profile is required to generate a plan",
                UserFriendlyMessage = "योजना बनाने के लिए खेत की जानकारी आवश्यक है (Farm profile is required to generate a plan)",
                SuggestedActions = new[] { "Provide your farm profile information" }
            });
        }

        // Verify farmer owns this farm
        if (!string.IsNullOrEmpty(request.FarmProfile.FarmerId) 
            && request.FarmProfile.FarmerId != "unknown"
            && request.FarmProfile.FarmerId != farmerId)
        {
            _logger.LogWarning(
                "Farmer {FarmerId} attempted to generate plan for farm owned by {OwnerId}",
                farmerId,
                request.FarmProfile.FarmerId);
            
            return Forbid();
        }

        try
        {
            // Convert DTO to domain model
            // Build location string from farm profile
            var locationString = $"{request.FarmProfile.Location.Village}, {request.FarmProfile.Location.Block}, {request.FarmProfile.Location.District}, {request.FarmProfile.Location.State}";
            
            // Parse collection date
            DateTimeOffset collectionDate;
            if (!DateTimeOffset.TryParse(request.SoilData.CollectionDate, out collectionDate))
            {
                collectionDate = DateTimeOffset.UtcNow;
            }
            
            var soilData = new SoilHealthData(
                farmerId: request.SoilData.FarmerId,
                location: locationString,
                nitrogen: request.SoilData.Nitrogen,
                phosphorus: request.SoilData.Phosphorus,
                potassium: request.SoilData.Potassium,
                pH: request.SoilData.pH,
                organicCarbon: request.SoilData.OrganicCarbon,
                sulfur: request.SoilData.Sulfur,
                zinc: request.SoilData.Zinc,
                boron: request.SoilData.Boron,
                iron: request.SoilData.Iron,
                manganese: request.SoilData.Manganese,
                copper: request.SoilData.Copper,
                testDate: collectionDate.UtcDateTime,
                labId: request.SoilData.SampleId ?? "UNKNOWN"
            );
            
            // Convert FarmProfileDto to FarmProfile domain model
            var farmProfile = new FarmProfile(
                farmId: Guid.NewGuid().ToString(),
                farmerId: farmerId,
                areaInAcres: request.FarmProfile.FarmSize,
                soilType: request.FarmProfile.SoilType,
                irrigationType: "Unknown", // Not provided in DTO
                currentCrops: request.FarmProfile.PrimaryCrops,
                coordinates: new GeoCoordinates(0, 0) // Not provided in DTO
            );
            
            // Store soil data in DynamoDB for future use (e.g., planting advisory)
            await _soilDataRepository.StoreSoilDataAsync(soilData, cancellationToken);
            
            _logger.LogInformation(
                "Soil data stored for farmer {FarmerId}",
                farmerId);

            // Generate regenerative plan
            var plan = await _planGenerator.GeneratePlanAsync(
                soilData,
                farmProfile,
                cancellationToken,
                request.Language);

            _logger.LogInformation(
                "Regenerative plan generated successfully for farmer {FarmerId}",
                farmerId);

            return Ok(plan);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Plan generation cancelled for farmer {FarmerId}", farmerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error generating regenerative plan for farmer {FarmerId}",
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "SERVICE_UNAVAILABLE",
                Message = "An error occurred while generating your regenerative plan",
                UserFriendlyMessage = "आपकी पुनर्योजी योजना बनाते समय एक त्रुटि हुई (An error occurred while generating your regenerative plan)",
                SuggestedActions = new[] { "Try again in a few moments", "Contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Get soil data history for the authenticated farmer
    /// </summary>
    /// <param name="startDate">Start date for history query</param>
    /// <param name="endDate">End date for history query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of soil health data records</returns>
    [HttpGet("history")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(IEnumerable<SoilHealthData>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetSoilHistory(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "a47854d8-40a1-709a-edfa-8c09543849a1";  // Use test farmer ID when not authenticated
        var start = startDate ?? DateTimeOffset.UtcNow.AddYears(-2);
        var end = endDate ?? DateTimeOffset.UtcNow;

        if (start > end)
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "INVALID_DATE_RANGE",
                Message = "Start date must be before end date",
                UserFriendlyMessage = "प्रारंभ तिथि समाप्ति तिथि से पहले होनी चाहिए (Start date must be before end date)",
                SuggestedActions = new[] { "Adjust the date range and try again" }
            });
        }

        _logger.LogInformation(
            "Soil history request from farmer {FarmerId} for period {Start} to {End}",
            farmerId,
            start,
            end);

        try
        {
            var history = await _soilDataRepository.GetSoilHistoryAsync(
                farmerId,
                start,
                end,
                cancellationToken);

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving soil history for farmer {FarmerId}",
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "SERVICE_UNAVAILABLE",
                Message = "An error occurred while retrieving your soil history",
                UserFriendlyMessage = "आपका मृदा इतिहास प्राप्त करते समय एक त्रुटि हुई (An error occurred while retrieving your soil history)",
                SuggestedActions = new[] { "Try again in a few moments", "Contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Save a regenerative plan to S3
    /// </summary>
    /// <param name="plan">The regenerative plan to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved plan confirmation</returns>
    [HttpPost("plans/save")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(SavedPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SavePlan(
        [FromBody] RegenerativePlan plan,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "a47854d8-40a1-709a-edfa-8c09543849a1";

        _logger.LogInformation(
            "Save plan request received from farmer {FarmerId} for plan {PlanId}",
            farmerId,
            plan.PlanId);

        // Update plan's farmerId to match authenticated user (for security)
        // This ensures the plan is saved under the correct farmer's account
        if (plan.FarmerId != farmerId)
        {
            _logger.LogInformation(
                "Updating plan farmerId from {PlanFarmerId} to authenticated farmer {FarmerId}",
                plan.FarmerId,
                farmerId);
            
            // Create a new plan with the correct farmerId
            plan = plan with { FarmerId = farmerId };
        }

        try
        {
            var planJson = JsonSerializer.Serialize(plan, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(planJson));
            var s3Key = await _s3StorageService.UploadAsync(
                stream,
                $"{plan.PlanId}.json",
                farmerId,
                "application/json",
                cancellationToken);

            _logger.LogInformation(
                "Plan {PlanId} saved successfully for farmer {FarmerId} at {S3Key}",
                plan.PlanId,
                farmerId,
                s3Key);

            return Ok(new SavedPlanResponse
            {
                PlanId = plan.PlanId,
                S3Key = s3Key,
                SavedAt = DateTimeOffset.UtcNow,
                Message = "योजना सफलतापूर्वक सहेजी गई (Plan saved successfully)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error saving plan {PlanId} for farmer {FarmerId}",
                plan.PlanId,
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "PLAN_SAVE_FAILED",
                Message = "An error occurred while saving your plan",
                UserFriendlyMessage = "आपकी योजना सहेजते समय एक त्रुटि हुई (An error occurred while saving your plan)",
                SuggestedActions = new[] { "Try again in a few moments", "Contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Get all saved plans for the authenticated farmer
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of saved plans</returns>
    [HttpGet("plans")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(IEnumerable<RegenerativePlan>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSavedPlans(CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "a47854d8-40a1-709a-edfa-8c09543849a1";

        _logger.LogInformation(
            "Get saved plans request from farmer {FarmerId}",
            farmerId);

        try
        {
            // List all plan files for this farmer
            var prefix = $"misc/{farmerId}/";
            var s3Keys = await _s3StorageService.ListObjectsAsync(prefix, cancellationToken);

            var plans = new List<RegenerativePlan>();

            foreach (var s3Key in s3Keys.Where(k => k.EndsWith(".json") && !k.Contains("planting-recommendation")))
            {
                try
                {
                    using var stream = await _s3StorageService.DownloadAsync(s3Key, farmerId, cancellationToken);
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync();
                    var plan = JsonSerializer.Deserialize<RegenerativePlan>(json);
                    
                    if (plan != null)
                    {
                        plans.Add(plan);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to load plan from {S3Key} for farmer {FarmerId}",
                        s3Key,
                        farmerId);
                }
            }

            _logger.LogInformation(
                "Retrieved {Count} saved plans for farmer {FarmerId}",
                plans.Count,
                farmerId);

            return Ok(plans.OrderByDescending(p => p.CreatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving saved plans for farmer {FarmerId}",
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "PLANS_RETRIEVAL_FAILED",
                Message = "An error occurred while retrieving your saved plans",
                UserFriendlyMessage = "आपकी सहेजी गई योजनाएं प्राप्त करते समय एक त्रुटि हुई (An error occurred while retrieving your saved plans)",
                SuggestedActions = new[] { "Try again in a few moments", "Contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Get a specific saved plan by ID
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The regenerative plan</returns>
    [HttpGet("plans/{planId}")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(RegenerativePlan), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPlanById(
        string planId,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "a47854d8-40a1-709a-edfa-8c09543849a1";

        _logger.LogInformation(
            "Get plan {PlanId} request from farmer {FarmerId}",
            planId,
            farmerId);

        try
        {
            // List all plan files for this farmer to find the matching plan
            var prefix = $"misc/{farmerId}/";
            var s3Keys = await _s3StorageService.ListObjectsAsync(prefix, cancellationToken);

            var planKey = s3Keys.FirstOrDefault(k => k.Contains($"{planId}.json"));

            if (planKey == null)
            {
                return NotFound(new ErrorResponse
                {
                    ErrorCode = "PLAN_NOT_FOUND",
                    Message = $"Plan {planId} not found",
                    UserFriendlyMessage = "योजना नहीं मिली (Plan not found)",
                    SuggestedActions = new[] { "Check the plan ID and try again" }
                });
            }

            using var stream = await _s3StorageService.DownloadAsync(planKey, farmerId, cancellationToken);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            var plan = JsonSerializer.Deserialize<RegenerativePlan>(json);

            return Ok(plan);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving plan {PlanId} for farmer {FarmerId}",
                planId,
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "PLAN_RETRIEVAL_FAILED",
                Message = "An error occurred while retrieving the plan",
                UserFriendlyMessage = "योजना प्राप्त करते समय एक त्रुटि हुई (An error occurred while retrieving the plan)",
                SuggestedActions = new[] { "Try again in a few moments", "Contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Delete a saved plan
    /// </summary>
    /// <param name="planId">Plan ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("plans/{planId}")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePlan(
        string planId,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "a47854d8-40a1-709a-edfa-8c09543849a1";

        _logger.LogInformation(
            "Delete plan {PlanId} request from farmer {FarmerId}",
            planId,
            farmerId);

        try
        {
            // List all plan files for this farmer to find the matching plan
            var prefix = $"misc/{farmerId}/";
            var s3Keys = await _s3StorageService.ListObjectsAsync(prefix, cancellationToken);

            var planKey = s3Keys.FirstOrDefault(k => k.Contains($"{planId}.json"));

            if (planKey == null)
            {
                return NotFound(new ErrorResponse
                {
                    ErrorCode = "PLAN_NOT_FOUND",
                    Message = $"Plan {planId} not found",
                    UserFriendlyMessage = "योजना नहीं मिली (Plan not found)",
                    SuggestedActions = new[] { "Check the plan ID and try again" }
                });
            }

            await _s3StorageService.DeleteAsync(planKey, cancellationToken);

            _logger.LogInformation(
                "Plan {PlanId} deleted successfully for farmer {FarmerId}",
                planId,
                farmerId);

            return Ok(new
            {
                Message = "योजना सफलतापूर्वक हटाई गई (Plan deleted successfully)",
                PlanId = planId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting plan {PlanId} for farmer {FarmerId}",
                planId,
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "PLAN_DELETION_FAILED",
                Message = "An error occurred while deleting the plan",
                UserFriendlyMessage = "योजना हटाते समय एक त्रुटि हुई (An error occurred while deleting the plan)",
                SuggestedActions = new[] { "Try again in a few moments", "Contact support if the problem persists" }
            });
        }
    }
}

/// <summary>
/// Response for Soil Health Card upload
/// </summary>
public record SoilHealthCardResponse
{
    public SoilHealthDataDto? SoilData { get; init; }
    public bool IsValid { get; init; }
    public List<ValidationError> ValidationErrors { get; init; } = new();
    public string Message { get; init; } = string.Empty;
    public bool RequiresManualVerification { get; init; }
}

/// <summary>
/// DTO for Soil Health Data that matches frontend expectations
/// </summary>
public record SoilHealthDataDto
{
    public string FarmerId { get; init; } = string.Empty;
    public string SampleId { get; init; } = string.Empty;
    public string CollectionDate { get; init; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("pH")]
    public float pH { get; init; }
    
    public float OrganicCarbon { get; init; }
    public float Nitrogen { get; init; }
    public float Phosphorus { get; init; }
    public float Potassium { get; init; }
    public float Sulfur { get; init; }
    public float Zinc { get; init; }
    public float Boron { get; init; }
    public float Iron { get; init; }
    public float Manganese { get; init; }
    public float Copper { get; init; }
    public string SoilTexture { get; init; } = string.Empty;
    public List<string> Recommendations { get; init; } = new();

    public static SoilHealthDataDto FromDomain(SoilHealthData domain)
    {
        return new SoilHealthDataDto
        {
            FarmerId = domain.FarmerId,
            SampleId = domain.LabId, // Use LabId as SampleId
            CollectionDate = domain.TestDate.ToString("yyyy-MM-dd"),
            pH = domain.pH,
            OrganicCarbon = domain.OrganicCarbon,
            Nitrogen = domain.Nitrogen,
            Phosphorus = domain.Phosphorus,
            Potassium = domain.Potassium,
            Sulfur = domain.Sulfur,
            Zinc = domain.Zinc,
            Boron = domain.Boron,
            Iron = domain.Iron,
            Manganese = domain.Manganese,
            Copper = domain.Copper,
            SoilTexture = "Loamy", // Default value, can be extracted from location or added to model
            Recommendations = new List<string>() // Can be generated based on nutrient levels
        };
    }
}

/// <summary>
/// Location information for farm profile
/// </summary>
public record LocationDto
{
    public string State { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string Block { get; init; } = string.Empty;
    public string Village { get; init; } = string.Empty;
}

/// <summary>
/// Farm profile DTO that matches frontend structure
/// </summary>
public record FarmProfileDto
{
    public string? FarmerId { get; init; }
    public string FarmName { get; init; } = string.Empty;
    public LocationDto Location { get; init; } = new();
    public float FarmSize { get; init; }
    public List<string> PrimaryCrops { get; init; } = new();
    public string SoilType { get; init; } = string.Empty;
}

/// <summary>
/// Request for regenerative plan generation
/// </summary>
public record PlanGenerationRequest
{
    public SoilHealthDataDto SoilData { get; init; } = null!;
    public FarmProfileDto FarmProfile { get; init; } = null!;
    public string? Language { get; init; }
}

/// <summary>
/// Response for saved plan
/// </summary>
public record SavedPlanResponse
{
    public string PlanId { get; init; } = string.Empty;
    public string S3Key { get; init; } = string.Empty;
    public DateTimeOffset SavedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}
