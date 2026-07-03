using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Core.VoiceIntelligence.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// Gemini-based query parser fallback when dictionary parsing fails.
/// </summary>
public class QueryParser : IQueryParser
{
    private readonly GeminiService _geminiService;
    private readonly GeminiModelConfig _modelConfig;
    private readonly ILogger<QueryParser> _logger;

    public QueryParser(
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        ILogger<QueryParser> logger)
    {
        _geminiService = geminiService;
        _modelConfig = modelConfig;
        _logger = logger;
    }

    public async Task<ParsedQuery> ParseQueryAsync(string transcribedText, string context, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $@"Parse this Indian farmer's voice query and extract:
1. commodity (if asking about a crop/produce price or info)
2. location (if mentioned)
3. intent: one of [price_query, general_question, weather_query, soil_query, unknown]
4. needsKnowledgeBase: true if this is a farming advice question

Query: ""{transcribedText}""
Context: {context}

Return ONLY JSON:
{{""commodity"": """", ""location"": """", ""intent"": """", ""needsKnowledgeBase"": false, ""confidence"": 0.8}}";

            var response = await _geminiService.GenerateContentAsync(
                _modelConfig.QueryParserModel, prompt, 0.1f, 200, cancellationToken);

            var json = response.Trim();
            if (json.StartsWith("```"))
            {
                var start = json.IndexOf('{');
                var end = json.LastIndexOf('}');
                if (start >= 0 && end > start)
                    json = json[start..(end + 1)];
            }
            
            // If response doesn't contain valid JSON, fall back to general question
            if (!json.StartsWith("{"))
            {
                _logger.LogWarning("Gemini returned non-JSON response: {Response}", json);
                return new ParsedQuery("", "", "general_question", true, null, 0.5f);
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new ParsedQuery(
                root.GetProperty("commodity").GetString() ?? "",
                root.GetProperty("location").GetString() ?? "",
                root.GetProperty("intent").GetString() ?? "general_question",
                root.GetProperty("needsKnowledgeBase").GetBoolean(),
                null,
                (float)(root.TryGetProperty("confidence", out var c) ? c.GetDouble() : 0.8));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini query parsing failed, defaulting to general_question");
            return new ParsedQuery("", "", "general_question", true, null, 0.5f);
        }
    }
}
