using KisanMitraAI.Core.AI;
using KisanMitraAI.Infrastructure.AI;
using KisanMitraAI.Infrastructure.Storage.S3;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.Vision;

/// <summary>
/// AI-powered produce quality grader using Vertex AI Gemini vision model.
/// Grades produce as an experienced Indian mandi trader would — using AGMARK-aligned
/// standards with detailed farmer-facing reasoning.
/// </summary>
public class BedrockVisionGrader
{
    private readonly GeminiService _geminiService;
    private readonly IS3StorageService _storageService;
    private readonly GeminiModelConfig _modelConfig;
    private readonly ILogger<BedrockVisionGrader> _logger;

    public BedrockVisionGrader(
        GeminiService geminiService,
        IS3StorageService storageService,
        GeminiModelConfig modelConfig,
        ILogger<BedrockVisionGrader> logger)
    {
        _geminiService = geminiService;
        _storageService = storageService;
        _modelConfig = modelConfig;
        _logger = logger;
    }

    public async Task<VisionGradeResult?> GradeImageAsync(
        string s3Key, string produceType, CancellationToken cancellationToken)
        => await GradeImageAsync(s3Key, produceType, "Hindi", cancellationToken);

    public async Task<VisionGradeResult?> GradeImageAsync(
        string s3Key, string produceType, string language, CancellationToken cancellationToken)
    {
        try
        {
            // Download image from GCS
            var imageStream = await _storageService.DownloadAsync(s3Key, "", cancellationToken);
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms, cancellationToken);
            var imageBytes = ms.ToArray();

            if (imageBytes.Length == 0)
            {
                _logger.LogWarning("Could not download image {Key} for vision grading", s3Key);
                return null;
            }

            if (imageBytes.Length > 3_500_000)
            {
                _logger.LogWarning("Image {Key} too large ({Size}B), skipping AI grading", s3Key, imageBytes.Length);
                return null;
            }

            var prompt = BuildGradingPrompt(produceType, language);
            var mimeType = s3Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";

            var responseText = await _geminiService.GenerateContentWithImageAsync(
                _modelConfig.QualityGradingModel, prompt, imageBytes, mimeType, 0.1f, 500, cancellationToken);

            if (string.IsNullOrEmpty(responseText))
            {
                _logger.LogWarning("Empty response from vision grader for {Key}", s3Key);
                return null;
            }

            return ParseVisionResult(responseText, s3Key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vision grading failed for {Key}", s3Key);
            return null;
        }
    }

    private static readonly Dictionary<string, string> LanguageInstructions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["English"] = "Write the reasoning field in simple English.",
        ["Hindi"] = "Write the reasoning field in simple Hindi (हिंदी में लिखें).",
        ["Tamil"] = "Write the reasoning field in simple Tamil (தமிழில் எழுதுங்கள்).",
        ["Telugu"] = "Write the reasoning field in simple Telugu (తెలుగులో రాయండి).",
        ["Bengali"] = "Write the reasoning field in simple Bengali (বাংলায় লিখুন).",
        ["Marathi"] = "Write the reasoning field in simple Marathi (मराठीत लिहा).",
        ["Gujarati"] = "Write the reasoning field in simple Gujarati (ગુજરાતીમાં લખો).",
        ["Kannada"] = "Write the reasoning field in simple Kannada (ಕನ್ನಡದಲ್ಲಿ ಬರೆಯಿರಿ).",
        ["Malayalam"] = "Write the reasoning field in simple Malayalam (മലയാളത്തിൽ എഴുതുക).",
        ["Punjabi"] = "Write the reasoning field in simple Punjabi (ਪੰਜਾਬੀ ਵਿੱਚ ਲਿਖੋ).",
    };

    private static string BuildGradingPrompt(string produceType, string language)
    {
        var langInstruction = LanguageInstructions.GetValueOrDefault(language)
            ?? LanguageInstructions.GetValueOrDefault(language.Split('-')[0])
            ?? LanguageInstructions["Hindi"];

        return $@"You are a certified AGMARK quality inspector at an Indian APMC mandi with 20 years of experience grading {produceType}.

CRITICAL: This image contains {produceType.ToUpper()}. Inspect ONLY the {produceType}.

YOUR TASK: Grade this {produceType} examining freshness, size, color, defects, and ripeness.

GRADING STANDARDS (Indian mandi system):
- Grade A (Premium): Top 10-15%. Uniform large size, vibrant color, ZERO defects, perfectly fresh.
- Grade B (Good): Minor size variation, good color, 1-2 very minor blemishes.
- FAQ (Fair Average Quality): Standard grade — average size, acceptable color, minor imperfections.
- Grade C (Fair): Below average. Noticeable variation, some discoloration, visible minor defects.
- Non-FAQ (Below Standard): Poor quality. Multiple defects, significant discoloration.
- Reject: Rot, mold, heavy pest damage. UNFIT for market.

Return ONLY this JSON:
{{
  ""freshness"": <0-100>,
  ""sizeUniformity"": <0-100>,
  ""colorQuality"": <0-100>,
  ""defectScore"": <0-100, 100=zero defects>,
  ""ripeness"": <0-100>,
  ""overallGrade"": ""Grade A"" or ""Grade B"" or ""FAQ"" or ""Grade C"" or ""Non-FAQ"" or ""Reject"",
  ""reasoning"": ""<2-3 sentences describing observations>""
}}

LANGUAGE INSTRUCTION: {langInstruction}
Return ONLY the JSON.";
    }

    private VisionGradeResult? ParseVisionResult(string responseText, string s3Key)
    {
        try
        {
            var json = responseText.Trim();
            if (json.StartsWith("```"))
            {
                var start = json.IndexOf('{');
                var end = json.LastIndexOf('}');
                if (start >= 0 && end > start)
                    json = json[start..(end + 1)];
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<VisionGradeResult>(json, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse vision JSON for {Key}: {Response}", s3Key, responseText);
            return null;
        }
    }
}

public class VisionGradeResult
{
    public float Freshness { get; set; }
    public float SizeUniformity { get; set; }
    public float ColorQuality { get; set; }
    public float DefectScore { get; set; }
    public float Ripeness { get; set; }
    public string OverallGrade { get; set; } = "FAQ";
    public string Reasoning { get; set; } = "";
}
