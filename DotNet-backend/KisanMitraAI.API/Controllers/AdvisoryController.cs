using KisanMitraAI.Core.Advisory;
using KisanMitraAI.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KisanMitraAI.API.Controllers;

/// <summary>
/// Advisory controller for agricultural questions and knowledge base queries
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[RequiresFarmer]
public class AdvisoryController : ControllerBase
{
    private readonly IAdvisoryService _advisoryService;
    private readonly ILogger<AdvisoryController> _logger;

    public AdvisoryController(
        IAdvisoryService advisoryService,
        ILogger<AdvisoryController> logger)
    {
        _advisoryService = advisoryService;
        _logger = logger;
    }

    /// <summary>
    /// Ask an agricultural question and get expert advice
    /// </summary>
    /// <param name="request">Advisory question request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Advisory response with answer and citations</returns>
    [HttpPost("ask")]
    [EnableRateLimiting("farmer-rate-limit")]
    [ProducesResponseType(typeof(AdvisoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AskQuestion(
        [FromBody] AdvisoryQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var farmerId = User.GetFarmerId();
        
        _logger.LogInformation(
            "Advisory question received from farmer {FarmerId}",
            farmerId);

        // Validate question text
        if (string.IsNullOrWhiteSpace(request.QuestionText))
        {
            _logger.LogWarning("Empty question text received from farmer {FarmerId}", farmerId);
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "QUESTION_TEXT_REQUIRED",
                Message = "Question text is required",
                UserFriendlyMessage = "कृपया एक प्रश्न दर्ज करें (Please enter a question)",
                SuggestedActions = new[] { "Type or speak your agricultural question" }
            });
        }

        // Validate question length
        if (request.QuestionText.Length > 1000)
        {
            return BadRequest(new ErrorResponse
            {
                ErrorCode = "QUESTION_TOO_LONG",
                Message = "Question text exceeds maximum length of 1000 characters",
                UserFriendlyMessage = "प्रश्न बहुत लंबा है। कृपया 1000 अक्षरों से कम में प्रश्न पूछें (Question is too long. Please ask in less than 1000 characters)",
                SuggestedActions = new[] { "Shorten your question", "Break it into multiple questions" }
            });
        }

        // Validate conversation history length (max 10 exchanges)
        var conversationHistory = request.ConversationHistory ?? Array.Empty<string>();
        if (conversationHistory.Count() > 10)
        {
            _logger.LogWarning(
                "Conversation history too long ({Count} exchanges) from farmer {FarmerId}",
                conversationHistory.Count(),
                farmerId);

            // Trim to last 10 exchanges
            conversationHistory = conversationHistory.TakeLast(10);
        }

        try
        {
            // Create advisory question
            var question = new AdvisoryQuestion(
                QuestionText: request.QuestionText,
                FarmerId: farmerId,
                Context: request.Context ?? string.Empty,
                ConversationHistory: conversationHistory);

            // Get advisory response
            var response = await _advisoryService.AskQuestionAsync(
                question,
                cancellationToken);

            _logger.LogInformation(
                "Advisory response generated for farmer {FarmerId}. Citations: {CitationCount}, Escalation: {RequiresEscalation}",
                farmerId,
                response.Sources.Count(),
                response.RequiresExpertEscalation);

            return Ok(new AdvisoryResponseDto
            {
                AnswerText = response.AnswerText,
                Sources = response.Sources,
                RequiresExpertEscalation = response.RequiresExpertEscalation,
                RespondedAt = response.RespondedAt,
                Message = response.RequiresExpertEscalation
                    ? "यह प्रश्न विशेषज्ञ के पास भेजा गया है। आपको 24 घंटे के भीतर उत्तर मिलेगा (This question has been escalated to an expert. You will receive an answer within 24 hours)"
                    : "उत्तर सफलतापूर्वक उत्पन्न किया गया (Answer generated successfully)"
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Advisory question processing cancelled for farmer {FarmerId}", farmerId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing advisory question for farmer {FarmerId}",
                farmerId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "SERVICE_UNAVAILABLE",
                Message = "An error occurred while processing your question",
                UserFriendlyMessage = "आपके प्रश्न को संसाधित करते समय एक त्रुटि हुई। कृपया पुनः प्रयास करें (An error occurred while processing your question. Please try again)",
                SuggestedActions = new[] { "Try again in a few moments", "Rephrase your question", "Contact support if the problem persists" }
            });
        }
    }
}

/// <summary>
/// Request for advisory question
/// </summary>
public record AdvisoryQuestionRequest
{
    public string QuestionText { get; init; } = string.Empty;
    public string? Context { get; init; }
    public IEnumerable<string>? ConversationHistory { get; init; }
}

/// <summary>
/// Response with advisory answer
/// </summary>
public record AdvisoryResponseDto
{
    public string AnswerText { get; init; } = string.Empty;
    public IEnumerable<Citation> Sources { get; init; } = Array.Empty<Citation>();
    public bool RequiresExpertEscalation { get; init; }
    public DateTimeOffset RespondedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}
