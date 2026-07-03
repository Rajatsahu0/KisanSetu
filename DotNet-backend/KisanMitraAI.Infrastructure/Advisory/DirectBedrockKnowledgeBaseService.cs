using KisanMitraAI.Core.Advisory;
using KisanMitraAI.Core.AI;
using KisanMitraAI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Text;

namespace KisanMitraAI.Infrastructure.Advisory;

public class DirectBedrockKnowledgeBaseService : IKnowledgeBaseService
{
    private readonly GeminiService _geminiService;
    private readonly GeminiModelConfig _modelConfig;
    private readonly ILogger<DirectBedrockKnowledgeBaseService> _logger;

    private const string AgriculturalContext = @"
You are an expert agricultural advisor with deep knowledge of:
- Indian farming practices and regional variations
- Crop types: wheat, rice, cotton, sugarcane, pulses, oilseeds, vegetables, fruits
- Soil health: pH levels, NPK nutrients, organic carbon, micronutrients
- Regenerative farming: cover cropping, crop rotation, composting, reduced tillage
- Irrigation methods: drip, sprinkler, flood, rainfed
- Pest and disease management using integrated approaches
- Weather patterns and climate considerations for Indian agriculture
- Government schemes: PM-KISAN, Soil Health Card, crop insurance
- Market prices and value chain optimization

Provide practical, actionable advice tailored to small and marginal farmers in India.
Use simple language and focus on cost-effective, sustainable solutions.";

    public DirectBedrockKnowledgeBaseService(
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        ILogger<DirectBedrockKnowledgeBaseService> logger)
    {
        _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        _modelConfig = modelConfig ?? throw new ArgumentNullException(nameof(modelConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<KnowledgeBaseResponse> QueryKnowledgeBaseAsync(
        string query, string context, int maxResults, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Querying Gemini with model {Model}: {Query}", _modelConfig.AdvisoryModel, query);
            var prompt = BuildPromptWithContext(query, context);

            var answer = await _geminiService.GenerateContentAsync(
                _modelConfig.AdvisoryModel, prompt, 0.7f, 2048, cancellationToken);

            var citations = new[] { new Citation("AI-Generated Agricultural Advisory", "gemini://direct",
                answer.Length > 200 ? answer[..200] + "..." : answer, 1.0f) };

            var confidence = CalculateConfidenceScore(answer);

            return new KnowledgeBaseResponse(Answer: answer, Citations: citations, ConfidenceScore: confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Gemini directly");
            throw;
        }
    }

    private string BuildPromptWithContext(string query, string additionalContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine(AgriculturalContext);
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(additionalContext))
        {
            sb.AppendLine("Additional Context:");
            sb.AppendLine(additionalContext);
            sb.AppendLine();
        }
        sb.AppendLine("Farmer's Question:");
        sb.AppendLine(query);
        sb.AppendLine();
        sb.AppendLine("Please provide a detailed, practical answer:");
        return sb.ToString();
    }

    private static float CalculateConfidenceScore(string answer)
    {
        float confidence = 0.5f;
        if (answer.Length > 500) confidence += 0.1f;
        if (answer.Contains("1.") || answer.Contains("-")) confidence += 0.1f;
        var terms = new[] { "crop", "soil", "fertilizer", "irrigation", "pest", "yield", "harvest" };
        if (terms.Count(t => answer.Contains(t, StringComparison.OrdinalIgnoreCase)) >= 3) confidence += 0.1f;
        confidence += 0.2f; // base for successful generation
        return Math.Min(1.0f, confidence);
    }
}
