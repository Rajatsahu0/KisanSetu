using KisanMitraAI.Core.Advisory;
using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.VoiceIntelligence;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// Voice KB service — grounded responses using real data via Gemini.
/// When GroundedData is provided, LLM formats facts (never invents).
/// When no grounded data, LLM answers with a disclaimer.
/// </summary>
public class VoiceKnowledgeBaseService : IKnowledgeBaseService
{
    private readonly GeminiService _geminiService;
    private readonly GeminiModelConfig _modelConfig;
    private readonly ILogger<VoiceKnowledgeBaseService> _logger;

    private GroundedData? _currentGroundedData;

    private static readonly Dictionary<string, string> DialectInstructions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["English"]      = "Respond in simple English for Indian farmers.",
        ["en-IN"]        = "Respond in simple English for Indian farmers.",
        ["Hindi"]        = "सरल हिंदी में उत्तर दें।",
        ["hi-IN"]        = "सरल हिंदी में उत्तर दें।",
        ["Bundelkhandi"] = "बुंदेलखंडी बोली में सरल भाषा में उत्तर दें।",
        ["Bhojpuri"]     = "भोजपुरी बोली में सरल भाषा में उत्तर दें।",
        ["Tamil"]        = "எளிய தமிழில் பதிலளிக்கவும்.",
        ["ta-IN"]        = "எளிய தமிழில் பதிலளிக்கவும்.",
        ["Telugu"]       = "సరళమైన తెలుగులో సమాధానం ఇవ్వండి.",
        ["te-IN"]        = "సరళమైన తెలుగులో సమాధానం ఇవ్వండి.",
        ["Bengali"]      = "সহজ বাংলায় উত্তর দিন।",
        ["bn-IN"]        = "সহজ বাংলায় উত্তর দিন।",
        ["Marathi"]      = "सोप्या मराठीत उत्तर द्या.",
        ["mr-IN"]        = "सोप्या मराठीत उत्तर द्या.",
        ["Gujarati"]     = "સરળ ગુજરાતીમાં જવાબ આપો.",
        ["gu-IN"]        = "સરળ ગુજરાતીમાં જવાબ આપો.",
        ["Kannada"]      = "ಸರಳ ಕನ್ನಡದಲ್ಲಿ ಉತ್ತರಿಸಿ.",
        ["kn-IN"]        = "ಸರಳ ಕನ್ನಡದಲ್ಲಿ ಉತ್ತರಿಸಿ.",
        ["Malayalam"]    = "ലളിതമായ മലയാളത്തിൽ ഉത്തരം നൽകുക.",
        ["ml-IN"]        = "ലളിതമായ മലയാളത്തിൽ ഉത്തരം നൽകുക.",
        ["Punjabi"]      = "ਸਰਲ ਪੰਜਾਬੀ ਵਿੱਚ ਜਵਾਬ ਦਿਓ।",
        ["pa-IN"]        = "ਸਰਲ ਪੰਜਾਬੀ ਵਿੱਚ ਜਵਾਬ ਦਿਓ।",
    };

    public VoiceKnowledgeBaseService(
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        ILogger<VoiceKnowledgeBaseService> logger)
    {
        _geminiService = geminiService;
        _modelConfig = modelConfig;
        _logger = logger;
    }

    public void SetGroundedData(GroundedData? data) => _currentGroundedData = data;

    public async Task<KnowledgeBaseResponse> QueryKnowledgeBaseAsync(
        string query, string context, int maxResults, CancellationToken cancellationToken)
    {
        var dialect = ExtractDialect(context);
        var langInstruction = DialectInstructions.GetValueOrDefault(dialect) ?? DialectInstructions["Hindi"];

        _logger.LogInformation("VoiceKB: Model={Model}, Dialect={Dialect}, Query={Query}",
            _modelConfig.VoiceGeneralModel, dialect, query);

        try
        {
            var groundedData = _currentGroundedData;
            _currentGroundedData = null;

            var dataSection = "";
            var guardrail = "";

            if (groundedData != null && groundedData.HasRealData)
            {
                dataSection = $@"
REAL DATA (from {groundedData.DataSource}):
{groundedData.DataJson}

CRITICAL RULES:
- ONLY use the data provided above. Do NOT invent, estimate, or hallucinate any numbers.
- Present the data in a farmer-friendly format with the language instructed.
- If the data is incomplete, say what is available and suggest how to get more.
- Mention the data source so the farmer knows it is real.
";
            }
            else if (groundedData != null && !string.IsNullOrEmpty(groundedData.FallbackDisclaimer))
            {
                guardrail = $@"
IMPORTANT DISCLAIMER — you MUST include this at the end of your response:
{groundedData.FallbackDisclaimer}

Do NOT present any specific numbers (prices, temperatures, dates, percentages) as facts.
";
            }
            else
            {
                guardrail = @"
IMPORTANT: If this question requires real-time data (weather forecasts, market prices, soil test results),
do NOT invent specific numbers. Instead say you don't have live data and suggest the farmer check
the relevant module (weather, mandi prices, soil analysis) in the app.
";
            }

            var prompt = $@"You are an expert agricultural advisor for Indian farmers.

CURRENT DATE AND TIME: {DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(5.5)):dddd, dd MMMM yyyy, hh:mm tt} IST

LANGUAGE: {langInstruction}
You MUST respond in the language instructed above. Do NOT switch languages.
{dataSection}{guardrail}
RESPONSE FORMAT:
- Give a practical, detailed and informative answer
- If the farmer asks about today's date, day, or time, tell them the current date/time from above
- Include specific details: names, numbers, amounts, locations, dates where relevant
- Use bullet points or numbered lists for structured information
- Aim for 8-15 sentences with practical actionable information

Farmer's question: {query}

Answer:";

            var answer = await _geminiService.GenerateContentAsync(
                _modelConfig.VoiceGeneralModel, prompt, 0.7f, 4096, cancellationToken);

            _logger.LogInformation("VoiceKB: Dialect={Dialect}, ResponseLength={Length}", dialect, answer.Length);

            return new KnowledgeBaseResponse(
                Answer: answer,
                Citations: new[] { new Citation(
                    "AI Agricultural Advisor", "gemini://voice",
                    answer.Length > 100 ? answer[..100] + "..." : answer, 0.85f) },
                ConfidenceScore: 0.85f);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VoiceKB failed");
            var fallback = dialect.Contains("en", StringComparison.OrdinalIgnoreCase)
                ? "Sorry, there was a problem answering your question. Please try again later."
                : "क्षमा करें, अभी उत्तर देने में समस्या हो रही है। कृपया बाद में पुनः प्रयास करें।";
            return new KnowledgeBaseResponse(
                Answer: fallback,
                Citations: Enumerable.Empty<Citation>(),
                ConfidenceScore: 0.3f);
        }
    }

    private static string ExtractDialect(string context)
    {
        if (string.IsNullOrEmpty(context)) return "Hindi";
        var idx = context.IndexOf("Dialect:", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var part = context[(idx + 8)..].Trim();
            var comma = part.IndexOf(',');
            return comma >= 0 ? part[..comma].Trim() : part.Trim();
        }
        return "Hindi";
    }
}
