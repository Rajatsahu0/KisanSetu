using KisanMitraAI.Core.Authorization;
using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Core.VoiceIntelligence.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KisanMitraAI.API.Controllers;

/// <summary>
/// Voice query controller for market intelligence (Krishi-Vani module)
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
//[Authorize]  // Temporarily disabled for testing
//[RequiresFarmer]
public class VoiceQueryController : ControllerBase
{
    private readonly IVoiceQueryHandler _voiceQueryHandler;
    private readonly IVoiceQueryHistoryRepository _historyRepository;
    private readonly ILogger<VoiceQueryController> _logger;

    public VoiceQueryController(
        IVoiceQueryHandler voiceQueryHandler,
        IVoiceQueryHistoryRepository historyRepository,
        ILogger<VoiceQueryController> logger)
    {
        _voiceQueryHandler = voiceQueryHandler;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Process a TEXT query (client-side transcription via Web Speech API).
    /// Skips Transcribe entirely — response in 1-3 seconds.
    /// </summary>
    [HttpPost("text-query")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(VoiceQueryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessTextQuery(
        [FromBody] TextQueryRequest request,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId() ?? "74b814c8-e071-7010-1a65-ad38404fdce0";
        var dialect = request.Dialect ?? "hi-IN";

        _logger.LogInformation("Text query from farmer {FarmerId}: {Text}, Dialect: {Dialect}",
            farmerId, request.Text, dialect);

        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new ErrorResponse { ErrorCode = "TEXT_REQUIRED", Message = "Query text is required" });

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Step 1: Parse query (dictionary <10ms or Bedrock fallback)
            var queryParser = HttpContext.RequestServices.GetRequiredService<KisanMitraAI.Core.VoiceIntelligence.IQueryParser>();
            var parsed = await queryParser.ParseQueryAsync(request.Text, $"Farmer: {farmerId}, Dialect: {dialect}", cancellationToken);

            _logger.LogInformation("PERF TextQuery: Parse={Ms}ms, Intent={Intent}, Commodity={Commodity}",
                sw.ElapsedMilliseconds, parsed.Intent, parsed.Commodity);

            string responseText;
            var prices = new List<KisanMitraAI.Core.Models.MandiPrice>();

            if (parsed.RequiresClarification || string.IsNullOrEmpty(parsed.Commodity))
            {
                // Fetch grounded data for factual intents (weather, soil, etc.)
                KisanMitraAI.Core.VoiceIntelligence.GroundedData? groundedData = null;
                var voiceDataProvider = HttpContext.RequestServices.GetService<KisanMitraAI.Core.VoiceIntelligence.IVoiceDataProvider>();
                if (voiceDataProvider != null)
                {
                    groundedData = await voiceDataProvider.GetDataForIntentAsync(
                        parsed.Intent, request.Text, farmerId, parsed.Location, parsed.Commodity, cancellationToken);
                    _logger.LogInformation("GroundedData: Intent={Intent}, HasRealData={HasData}, Source={Source}",
                        groundedData.Intent, groundedData.HasRealData, groundedData.DataSource);
                }

                // VoiceKnowledgeBaseService (Gemini) with grounded data injection
                var voiceKb = new KisanMitraAI.Infrastructure.AI.VoiceKnowledgeBaseService(
                    HttpContext.RequestServices.GetRequiredService<KisanMitraAI.Infrastructure.AI.GeminiService>(),
                    HttpContext.RequestServices.GetRequiredService<KisanMitraAI.Core.AI.GeminiModelConfig>(),
                    HttpContext.RequestServices.GetRequiredService<ILogger<KisanMitraAI.Infrastructure.AI.VoiceKnowledgeBaseService>>());
                voiceKb.SetGroundedData(groundedData);
                var kbResponse = await voiceKb.QueryKnowledgeBaseAsync(
                    request.Text, $"Farmer: {farmerId}, Dialect: {dialect}", 5, cancellationToken);
                responseText = kbResponse.Answer;
            }
            else
            {
                // Price query → DynamoDB + template (<50ms)
                var priceRetriever = HttpContext.RequestServices.GetRequiredService<KisanMitraAI.Core.VoiceIntelligence.IPriceRetriever>();

                var isMultiIntent = parsed.Intent is "price_comparison" or "multi_commodity_query" or "multi_commodity_comparison";
                if (isMultiIntent)
                {
                    // Build all commodity × location combinations and fetch in parallel
                    var commodities = parsed.Commodities.Count > 0 ? parsed.Commodities : new List<string> { parsed.Commodity };
                    var locations = parsed.Locations.Count > 0 ? parsed.Locations : new List<string> { parsed.Location };

                    var tasks = new List<Task<IEnumerable<KisanMitraAI.Core.Models.MandiPrice>>>();
                    foreach (var c in commodities)
                        foreach (var l in locations)
                            tasks.Add(priceRetriever.GetCurrentPricesAsync(c, l, cancellationToken));

                    var allResults = await Task.WhenAll(tasks);
                    prices = allResults.SelectMany(r => r).ToList();
                }
                else
                {
                    var priceResult = await priceRetriever.GetCurrentPricesAsync(parsed.Commodity, parsed.Location, cancellationToken);
                    prices = priceResult.ToList();
                }

                var responseGenerator = HttpContext.RequestServices.GetRequiredService<KisanMitraAI.Core.VoiceIntelligence.IResponseGenerator>();
                
                // If no prices from data.gov.in, fallback to Gemini for approximate prices
                if (prices.Count == 0)
                {
                    _logger.LogInformation("No live prices available, falling back to Gemini for {Commodity} in {Location}", parsed.Commodity, parsed.Location);
                    var geminiService = HttpContext.RequestServices.GetRequiredService<KisanMitraAI.Infrastructure.AI.GeminiService>();
                    var modelConfig = HttpContext.RequestServices.GetRequiredService<KisanMitraAI.Core.AI.GeminiModelConfig>();
                    var prompt = $"What is the current approximate market price (mandi bhav) of {parsed.Commodity} in {parsed.Location}, India? Give 2-3 nearby mandi prices in ₹ per Quintal. Be concise. Reply in {(dialect.Contains("en") ? "English" : "Hindi")}.";
                    responseText = await geminiService.GenerateContentAsync(modelConfig.VoiceParseModel, prompt, 0.7f, 300, cancellationToken);
                }
                else
                {
                    responseText = await responseGenerator.GenerateResponseAsync(parsed, prices, dialect, cancellationToken);
                }
            }

            // Step 3: Synthesize voice (Polly direct, 200-500ms)
            string audioUrl = "";
            try
            {
                var synthesizer = HttpContext.RequestServices.GetRequiredService<KisanMitraAI.Core.VoiceIntelligence.IVoiceSynthesizer>();
                var languageCode = dialect.Contains("en") ? "en-IN" : "hi-IN";
                var (_, url) = await synthesizer.SynthesizeAsync(responseText, "", languageCode, cancellationToken);
                audioUrl = url;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Polly synthesis failed for text query");
            }

            _logger.LogInformation("PERF TextQuery COMPLETE: Total={Ms}ms", sw.ElapsedMilliseconds);

            // Save to history
            try
            {
                var historyItem = new VoiceQueryHistoryItem
                {
                    QueryId = Guid.NewGuid().ToString(),
                    FarmerId = farmerId,
                    Transcription = request.Text,
                    ResponseText = responseText,
                    Dialect = dialect,
                    Confidence = request.Confidence ?? 0.95f,
                    Timestamp = DateTime.UtcNow,
                    IsFavorite = false,
                    PricesJson = System.Text.Json.JsonSerializer.Serialize(prices)
                };
                await _historyRepository.SaveQueryAsync(historyItem, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save text query history");
            }

            return Ok(new VoiceQueryResponseDto
            {
                Transcription = request.Text,
                Prices = prices.Select(p => new MarketPriceDto
                {
                    Commodity = p.Commodity,
                    Market = p.MandiName,
                    Price = p.ModalPrice,
                    Unit = p.Unit,
                    Date = p.PriceDate.ToString("o"),
                    Source = p.Location
                }).ToList(),
                AudioResponseUrl = audioUrl,
                ResponseText = responseText,
                Confidence = request.Confidence ?? 0.95f,
                Dialect = dialect
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing text query");
            return StatusCode(500, new ErrorResponse
            {
                ErrorCode = "SERVICE_UNAVAILABLE",
                Message = "Failed to process your query",
                UserFriendlyMessage = "आपकी क्वेरी प्रोसेस करने में त्रुटि हुई। कृपया पुनः प्रयास करें।"
            });
        }
    }

    /// <summary>
    /// Process a voice query for market intelligence
    /// </summary>
    /// <param name="audioFile">Audio file containing the voice query (MP3, WAV, OGG)</param>
    /// <param name="dialect">Regional dialect (e.g., Bundelkhandi, Bhojpuri, Marwari)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Voice query response with audio URL and price data</returns>
    [HttpPost("query")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(VoiceQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max
    public async Task<IActionResult> ProcessVoiceQuery(
        [FromForm] IFormFile audioFile,
        [FromForm] string dialect,
        CancellationToken cancellationToken)
    {
        // Temporary: Use test user ID when authorization is disabled
        var farmerId = User.GetFarmerId() ?? "74b814c8-e071-7010-1a65-ad38404fdce0"; // Test user Rajat
        
        _logger.LogInformation(
            "Voice query request received from farmer {FarmerId} with dialect {Dialect}",
            farmerId,
            dialect);

        // Validate audio file
        if (audioFile == null || audioFile.Length == 0)
        {
            _logger.LogWarning("Empty audio file received from farmer {FarmerId}", farmerId);
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "AUDIO_FILE_REQUIRED",
                Message = "Audio file is required",
                UserFriendlyMessage = "कृपया एक ऑडियो फ़ाइल अपलोड करें (Please upload an audio file)",
                SuggestedActions = new[] { "Upload a valid audio file in MP3, WAV, or OGG format" }
            });
        }

        // Validate audio format
        var allowedFormats = new[] { ".mp3", ".wav", ".ogg" };
        var fileExtension = Path.GetExtension(audioFile.FileName).ToLowerInvariant();
        
        if (!allowedFormats.Contains(fileExtension))
        {
            _logger.LogWarning(
                "Invalid audio format {Format} received from farmer {FarmerId}",
                fileExtension,
                farmerId);
            
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "AUDIO_FORMAT_INVALID",
                Message = $"Invalid audio format: {fileExtension}",
                UserFriendlyMessage = "कृपया MP3, WAV, या OGG प्रारूप में ऑडियो अपलोड करें (Please upload audio in MP3, WAV, or OGG format)",
                SuggestedActions = new[] { "Convert your audio to MP3, WAV, or OGG format and try again" }
            });
        }

        // Validate file size (max 10 MB)
        if (audioFile.Length > 10 * 1024 * 1024)
        {
            _logger.LogWarning(
                "Audio file too large ({Size} bytes) from farmer {FarmerId}",
                audioFile.Length,
                farmerId);
            
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "AUDIO_FILE_TOO_LARGE",
                Message = $"Audio file size ({audioFile.Length} bytes) exceeds maximum allowed size (10 MB)",
                UserFriendlyMessage = "ऑडियो फ़ाइल बहुत बड़ी है। कृपया 10 MB से छोटी फ़ाइल अपलोड करें (Audio file is too large. Please upload a file smaller than 10 MB)",
                SuggestedActions = new[] { "Reduce audio file size to under 10 MB", "Record a shorter audio clip" }
            });
        }

        // Validate dialect
        if (string.IsNullOrWhiteSpace(dialect))
        {
            _logger.LogWarning("Missing dialect parameter from farmer {FarmerId}", farmerId);
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "DIALECT_REQUIRED",
                Message = "Dialect parameter is required",
                UserFriendlyMessage = "कृपया अपनी बोली चुनें (Please select your dialect)",
                SuggestedActions = new[] { "Specify a regional dialect (e.g., Bundelkhandi, Bhojpuri, Marwari)" }
            });
        }

        try
        {
            // Process voice query
            using var audioStream = audioFile.OpenReadStream();
            var response = await _voiceQueryHandler.ProcessVoiceQueryAsync(
                audioStream,
                dialect,
                farmerId,
                cancellationToken);

            // OPTIMIZED: Await the save instead of fire-and-forget Task.Run.
            // Lambda freezes after response — Task.Run would silently lose data.
            // DynamoDB write is ~10ms, negligible impact on response time.
            try
            {
                var historyItem = new VoiceQueryHistoryItem
                {
                    QueryId = Guid.NewGuid().ToString(),
                    FarmerId = farmerId,
                    Transcription = response.Transcription,
                    ResponseText = response.ResponseText,
                    Dialect = dialect,
                    Confidence = response.Confidence,
                    AudioS3Key = ExtractS3KeyFromUrl(response.AudioResponseUrl),
                    ResponseAudioS3Key = ExtractS3KeyFromUrl(response.AudioResponseUrl),
                    Timestamp = DateTime.UtcNow,
                    IsFavorite = false,
                    PricesJson = System.Text.Json.JsonSerializer.Serialize(response.Prices)
                };

                await _historyRepository.SaveQueryAsync(historyItem, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save voice query to history for farmer {FarmerId}", farmerId);
            }

            _logger.LogInformation(
                "Voice query processed successfully for farmer {FarmerId}. Prices returned: {PriceCount}",
                farmerId,
                response.Prices.Count());

            // Map to DTO for frontend compatibility
            var responseDto = new VoiceQueryResponseDto
            {
                Transcription = response.Transcription,
                Prices = response.Prices.Select(p => new MarketPriceDto
                {
                    Commodity = p.Commodity,
                    Market = p.MandiName,
                    Price = p.ModalPrice,
                    Unit = p.Unit,
                    Date = p.PriceDate.ToString("o"), // ISO 8601 format
                    Source = p.Location
                }).ToList(),
                AudioResponseUrl = response.AudioResponseUrl,
                ResponseText = response.ResponseText,
                Confidence = response.Confidence,
                Dialect = response.Dialect
            };

            return Ok(responseDto);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Voice query processing cancelled for farmer {FarmerId}", farmerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing voice query for farmer {FarmerId}",
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "SERVICE_UNAVAILABLE",
                Message = "An error occurred while processing your voice query",
                UserFriendlyMessage = "आपकी आवाज़ क्वेरी को संसाधित करते समय एक त्रुटि हुई। कृपया पुनः प्रयास करें (An error occurred while processing your voice query. Please try again)",
                SuggestedActions = new[] { "Try again in a few moments", "Check your internet connection", "Contact support if the problem persists" }
            });
        }
    }

    /// <summary>
    /// Get voice query history for the authenticated farmer
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<VoiceQueryHistoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var farmerId = User.GetFarmerId() ?? "74b814c8-e071-7010-1a65-ad38404fdce0";
        
        _logger.LogInformation("Retrieving voice query history for farmer {FarmerId}", farmerId);

        var history = await _historyRepository.GetHistoryAsync(farmerId, limit, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Get favorite voice queries for the authenticated farmer
    /// </summary>
    [HttpGet("favorites")]
    [ProducesResponseType(typeof(IEnumerable<VoiceQueryHistoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFavorites(CancellationToken cancellationToken = default)
    {
        var farmerId = User.GetFarmerId() ?? "74b814c8-e071-7010-1a65-ad38404fdce0";
        
        _logger.LogInformation("Retrieving favorite queries for farmer {FarmerId}", farmerId);

        var favorites = await _historyRepository.GetFavoritesAsync(farmerId, cancellationToken);
        return Ok(favorites);
    }

    /// <summary>
    /// Toggle favorite status for a voice query
    /// </summary>
    [HttpPut("history/{queryId}/favorite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ToggleFavorite(
        string queryId,
        [FromBody] ToggleFavoriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var farmerId = User.GetFarmerId() ?? "74b814c8-e071-7010-1a65-ad38404fdce0";
        
        _logger.LogInformation(
            "Toggling favorite status for query {QueryId} to {IsFavorite}", 
            queryId, 
            request.IsFavorite);

        await _historyRepository.ToggleFavoriteAsync(farmerId, queryId, request.IsFavorite, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete a voice query from history
    /// </summary>
    [HttpDelete("history/{queryId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteQuery(
        string queryId,
        CancellationToken cancellationToken = default)
    {
        var farmerId = User.GetFarmerId() ?? "74b814c8-e071-7010-1a65-ad38404fdce0";
        
        _logger.LogInformation("Deleting query {QueryId} for farmer {FarmerId}", queryId, farmerId);

        await _historyRepository.DeleteQueryAsync(farmerId, queryId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get a specific voice query by ID with fresh presigned URL
    /// </summary>
    [HttpGet("history/{queryId}")]
    [ProducesResponseType(typeof(VoiceQueryHistoryItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetQueryById(
        string queryId,
        CancellationToken cancellationToken = default)
    {
        var farmerId = User.GetFarmerId() ?? "74b814c8-e071-7010-1a65-ad38404fdce0";
        
        _logger.LogInformation("Retrieving query {QueryId} for farmer {FarmerId}", queryId, farmerId);

        var query = await _historyRepository.GetQueryByIdAsync(farmerId, queryId, cancellationToken);
        
        if (query == null)
        {
            return NotFound(new ErrorResponse
            {
                ErrorCode = "QUERY_NOT_FOUND",
                Message = $"Query {queryId} not found",
                UserFriendlyMessage = "क्वेरी नहीं मिली (Query not found)"
            });
        }

        return Ok(query);
    }

    private string ExtractS3KeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        try
        {
            var uri = new Uri(url);
            // Extract the path without query parameters
            var path = uri.AbsolutePath;
            // Remove leading slash
            return path.TrimStart('/');
        }
        catch
        {
            return url;
        }
    }
}

/// <summary>
/// Request for text-based query (client-side transcription)
/// </summary>
public record TextQueryRequest
{
    public string Text { get; init; } = string.Empty;
    public string? Dialect { get; init; }
    public float? Confidence { get; init; }
}

/// <summary>
/// Standard error response format
/// </summary>
public record ErrorResponse
{
    public string ErrorCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string UserFriendlyMessage { get; init; } = string.Empty;
    public IEnumerable<string> SuggestedActions { get; init; } = Array.Empty<string>();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? RequestId { get; init; }
}

/// <summary>
/// Request to toggle favorite status
/// </summary>
public record ToggleFavoriteRequest
{
    public bool IsFavorite { get; init; }
}

/// <summary>
/// DTO for voice query response compatible with frontend
/// </summary>
public record VoiceQueryResponseDto
{
    public string Transcription { get; init; } = string.Empty;
    public List<MarketPriceDto> Prices { get; init; } = new();
    public string AudioResponseUrl { get; init; } = string.Empty;
    public string ResponseText { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public string Dialect { get; init; } = string.Empty;
}

/// <summary>
/// DTO for market price compatible with frontend
/// </summary>
public record MarketPriceDto
{
    public string Commodity { get; init; } = string.Empty;
    public string Market { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
}
