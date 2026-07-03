using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.SoilAnalysis;
using KisanMitraAI.Infrastructure.AI;
using KisanMitraAI.Infrastructure.Storage.S3;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.SoilAnalysis;

/// <summary>
/// Gemini Vision-based soil card extractor.
/// Used as fallback when Document AI confidence is low or too many fields are 0.
/// Accepts multiple images in a single call for multi-page documents.
/// </summary>
public class BedrockSoilCardExtractor
{
    private readonly GeminiService _geminiService;
    private readonly IS3StorageService _storageService;
    private readonly GeminiModelConfig _modelConfig;
    private readonly ILogger<BedrockSoilCardExtractor> _logger;

    public BedrockSoilCardExtractor(
        GeminiService geminiService,
        IS3StorageService storageService,
        GeminiModelConfig modelConfig,
        ILogger<BedrockSoilCardExtractor> logger)
    {
        _geminiService = geminiService;
        _storageService = storageService;
        _modelConfig = modelConfig;
        _logger = logger;
    }

    /// <summary>
    /// Extract soil data from a GCS image using Gemini Vision.
    /// </summary>
    public async Task<TextExtractionResult> ExtractFromGcsImageAsync(
        string gcsKey, CancellationToken cancellationToken)
    {
        var imageBytes = await DownloadBytesAsync(gcsKey, cancellationToken);
        if (imageBytes == null || imageBytes.Length == 0)
            return EmptyResult(gcsKey);

        return await ExtractFromImageBytesAsync(imageBytes, gcsKey, cancellationToken);
    }

    /// <summary>
    /// Extract soil data from image bytes using Gemini Vision.
    /// </summary>
    public async Task<TextExtractionResult> ExtractFromImageBytesAsync(
        byte[] imageBytes, string documentKey, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Gemini soil card extraction for {Key}", documentKey);

        var prompt = BuildExtractionPrompt();
        var mimeType = "image/jpeg"; // Default; could detect from bytes

        try
        {
            var responseText = await _geminiService.GenerateContentWithImageAsync(
                _modelConfig.SoilCardExtractionModel, prompt, imageBytes, mimeType,
                0.1f, 2000, cancellationToken);

            if (string.IsNullOrEmpty(responseText))
            {
                _logger.LogWarning("Empty response from Gemini soil card extractor for {Key}", documentKey);
                return EmptyResult(documentKey);
            }

            return ParseResponse(responseText, documentKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini soil card extraction failed for {Key}", documentKey);
            return EmptyResult(documentKey);
        }
    }

    private static string BuildExtractionPrompt()
    {
        return @"You are an expert at reading Indian Soil Health Cards issued by the government.

Extract ALL soil health data from this image. The card may be in English, Hindi, or regional languages.

Look for these fields:
- Farmer ID / Registration Number
- Location / Village / District
- pH value (typically 4.0-9.0)
- Organic Carbon / OC (typically 0.1-2.0 %)
- Nitrogen / N (typically 100-600 kg/ha)
- Phosphorus / P / P2O5 (typically 5-80 kg/ha)
- Potassium / K / K2O (typically 50-500 kg/ha)
- Sulfur / S (typically 5-40 ppm)
- Zinc / Zn (typically 0.1-5.0 ppm)
- Boron / B (typically 0.1-3.0 ppm)
- Iron / Fe (typically 1-20 ppm)
- Manganese / Mn (typically 1-20 ppm)
- Copper / Cu (typically 0.1-5.0 ppm)
- Test Date / Sampling Date
- Lab ID / Laboratory Name

RULES:
- Extract the ACTUAL measured value, NOT the optimal range
- If a field is not visible or unreadable, use ""0"" for numbers and ""UNKNOWN"" for text

Return ONLY this JSON:
{
  ""farmerId"": ""<farmer id or UNKNOWN>"",
  ""location"": ""<village, district or UNKNOWN>"",
  ""pH"": <number>,
  ""organicCarbon"": <number in %>,
  ""nitrogen"": <number in kg/ha>,
  ""phosphorus"": <number in kg/ha>,
  ""potassium"": <number in kg/ha>,
  ""sulfur"": <number in ppm>,
  ""zinc"": <number in ppm>,
  ""boron"": <number in ppm>,
  ""iron"": <number in ppm>,
  ""manganese"": <number in ppm>,
  ""copper"": <number in ppm>,
  ""testDate"": ""<date as dd/MM/yyyy or UNKNOWN>"",
  ""labId"": ""<lab id or UNKNOWN>""
}

Return ONLY valid JSON, no markdown.";
    }

    private TextExtractionResult ParseResponse(string responseText, string documentKey)
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

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            TryAdd(fields, "farmer id", root, "farmerId");
            TryAdd(fields, "location", root, "location");
            TryAdd(fields, "ph", root, "pH");
            TryAdd(fields, "organic carbon", root, "organicCarbon");
            TryAdd(fields, "nitrogen", root, "nitrogen");
            TryAdd(fields, "phosphorus", root, "phosphorus");
            TryAdd(fields, "potassium", root, "potassium");
            TryAdd(fields, "sulfur", root, "sulfur");
            TryAdd(fields, "zinc", root, "zinc");
            TryAdd(fields, "boron", root, "boron");
            TryAdd(fields, "iron", root, "iron");
            TryAdd(fields, "manganese", root, "manganese");
            TryAdd(fields, "copper", root, "copper");
            TryAdd(fields, "test date", root, "testDate");
            TryAdd(fields, "lab id", root, "labId");

            var nonZeroCount = fields.Values.Count(v =>
                float.TryParse(v, out var f) ? f > 0 : !string.IsNullOrWhiteSpace(v) && v != "UNKNOWN" && v != "0");
            var confidence = nonZeroCount >= 10 ? 90f : nonZeroCount >= 6 ? 70f : 50f;

            _logger.LogInformation("Gemini extraction: {FieldCount} fields, {NonZero} non-zero, confidence {Confidence}%",
                fields.Count, nonZeroCount, confidence);

            return new TextExtractionResult(documentKey, fields,
                new Dictionary<string, TableData>(), confidence, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini soil card response for {Key}", documentKey);
            return EmptyResult(documentKey);
        }
    }

    private static void TryAdd(Dictionary<string, string> fields, string key, JsonElement root, string jsonProp)
    {
        if (!root.TryGetProperty(jsonProp, out var val)) return;
        var value = val.ValueKind switch
        {
            JsonValueKind.Number => val.GetDouble().ToString(),
            JsonValueKind.String => val.GetString() ?? "",
            _ => val.ToString()
        };
        if (!string.IsNullOrWhiteSpace(value))
            fields[key] = value;
    }

    private async Task<byte[]?> DownloadBytesAsync(string key, CancellationToken ct)
    {
        try
        {
            var stream = await _storageService.DownloadAsync(key, "", ct);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download {Key}", key);
            return null;
        }
    }

    private static TextExtractionResult EmptyResult(string key) =>
        new(key, new Dictionary<string, string>(), new Dictionary<string, TableData>(), 0f, DateTimeOffset.UtcNow);
}
