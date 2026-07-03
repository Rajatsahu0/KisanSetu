using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Core.VoiceIntelligence.Models;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// Gemini-based response generator fallback for non-price queries.
/// </summary>
public class ResponseGenerator : IResponseGenerator
{
    private readonly GeminiService _geminiService;
    private readonly GeminiModelConfig _modelConfig;
    private readonly ILogger<ResponseGenerator> _logger;

    public ResponseGenerator(
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        ILogger<ResponseGenerator> logger)
    {
        _geminiService = geminiService;
        _modelConfig = modelConfig;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(
        ParsedQuery query, IEnumerable<MandiPrice> prices, string dialect, CancellationToken cancellationToken)
    {
        var prompt = $@"You are an agricultural advisor for Indian farmers.
Respond in the dialect: {dialect}
Farmer's query intent: {query.Intent}
Commodity: {query.Commodity}
Location: {query.Location}
Original question context available.

Provide a helpful, practical answer in 3-5 sentences.";

        try
        {
            return await _geminiService.GenerateContentAsync(
                _modelConfig.ResponseGeneratorModel, prompt, 0.7f, 500, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini response generation failed");
            return dialect.Contains("en", StringComparison.OrdinalIgnoreCase)
                ? "Sorry, I couldn't generate a response. Please try again."
                : "क्षमा करें, उत्तर देने में समस्या हो रही है। कृपया पुनः प्रयास करें।";
        }
    }
}
