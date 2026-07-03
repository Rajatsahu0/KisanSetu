using Google.Cloud.DocumentAI.V1;
using Google.Protobuf;
using KisanMitraAI.Core.SoilAnalysis;
using KisanMitraAI.Infrastructure.AI;
using KisanMitraAI.Infrastructure.Storage.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.SoilAnalysis;

/// <summary>
/// 2-Layer Soil Card Text Extraction using Google Cloud Document AI + Gemini Vision fallback:
///   Layer 1: Document AI Form Parser (fast, structured)
///   Layer 2: Gemini Vision (fallback for low confidence or complex documents)
/// </summary>
public class TextExtractor : ITextExtractor
{
    private readonly IS3StorageService _storageService;
    private readonly BedrockSoilCardExtractor _geminiExtractor;
    private readonly ILogger<TextExtractor> _logger;
    private readonly string _processorId;
    private readonly string _projectId;
    private readonly string _location;
    private const float MinConfidence = 50f;
    private const int MaxZeroFieldsBeforeFallback = 5;

    private static readonly string[] NutrientKeys = {
        "nitrogen", "phosphorus", "potassium", "ph", "organic carbon",
        "sulfur", "zinc", "boron", "iron", "manganese", "copper"
    };

    public TextExtractor(
        IS3StorageService storageService,
        BedrockSoilCardExtractor geminiExtractor,
        IConfiguration config,
        ILogger<TextExtractor> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _geminiExtractor = geminiExtractor ?? throw new ArgumentNullException(nameof(geminiExtractor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processorId = config["GCP:DocumentAI:ProcessorId"] ?? "e2bcf04d3b3d83f6";
        _projectId = config["GCP:ProjectId"] ?? "kisansetu-501110";
        _location = config["GCP:DocumentAI:Location"] ?? "us";
    }

    public async Task<TextExtractionResult> ExtractTextAsync(
        string documentS3Key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documentS3Key))
            throw new ArgumentException("Document key is required", nameof(documentS3Key));

        var fileExtension = Path.GetExtension(documentS3Key).ToLowerInvariant();

        // TXT → direct read
        if (fileExtension == ".txt")
            return await ExtractTextFromTxtFileAsync(documentS3Key, cancellationToken);

        // Try Document AI first, fallback to Gemini Vision
        return await ExtractWithDocumentAIAndFallbackAsync(documentS3Key, cancellationToken);
    }

    private async Task<TextExtractionResult> ExtractWithDocumentAIAndFallbackAsync(
        string documentS3Key, CancellationToken cancellationToken)
    {
        TextExtractionResult? docAiResult = null;

        try
        {
            _logger.LogInformation("Layer 1: Document AI extraction for {Key}", documentS3Key);

            // Download document from GCS
            var stream = await _storageService.DownloadAsync(documentS3Key, "", cancellationToken);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            var documentBytes = ms.ToArray();

            // Determine MIME type
            var mimeType = Path.GetExtension(documentS3Key).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            // Call Document AI
            var client = await DocumentProcessorServiceClient.CreateAsync(cancellationToken);
            var processorName = $"projects/{_projectId}/locations/{_location}/processors/{_processorId}";

            var request = new ProcessRequest
            {
                Name = processorName,
                RawDocument = new RawDocument
                {
                    Content = ByteString.CopyFrom(documentBytes),
                    MimeType = mimeType
                }
            };

            var response = await client.ProcessDocumentAsync(request, cancellationToken);
            var document = response.Document;

            // Extract fields from Document AI entities
            var extractedFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entity in document.Entities)
            {
                if (!string.IsNullOrWhiteSpace(entity.Type) && !string.IsNullOrWhiteSpace(entity.MentionText))
                {
                    extractedFields[entity.Type] = entity.MentionText;
                }
            }

            // Also extract from form fields (key-value pairs)
            foreach (var page in document.Pages)
            {
                foreach (var field in page.FormFields)
                {
                    var key = GetTextFromLayout(field.FieldName, document.Text);
                    var value = GetTextFromLayout(field.FieldValue, document.Text);
                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                    {
                        extractedFields[key.Trim()] = value.Trim();
                    }
                }
            }

            var confidence = document.Entities.Any()
                ? (float)(document.Entities.Average(e => e.Confidence) * 100)
                : (extractedFields.Count > 5 ? 70f : 40f);

            docAiResult = new TextExtractionResult(
                documentS3Key, extractedFields, new Dictionary<string, TableData>(),
                confidence, DateTimeOffset.UtcNow);

            _logger.LogInformation(
                "Document AI: {FieldCount} fields, confidence {Confidence:F1}% for {Key}",
                extractedFields.Count, confidence, documentS3Key);

            if (IsResultAcceptable(docAiResult))
            {
                _logger.LogInformation("Document AI result acceptable for {Key}", documentS3Key);
                return docAiResult;
            }

            _logger.LogInformation("Document AI below threshold — falling back to Gemini for {Key}", documentS3Key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Document AI failed for {Key} — falling back to Gemini", documentS3Key);
        }

        // Layer 2: Gemini Vision fallback
        try
        {
            var geminiResult = await _geminiExtractor.ExtractFromGcsImageAsync(documentS3Key, cancellationToken);

            if (docAiResult != null && geminiResult.ConfidenceScore > 0)
            {
                var docAiNonZero = CountNonZeroNutrients(docAiResult.ExtractedFields);
                var geminiNonZero = CountNonZeroNutrients(geminiResult.ExtractedFields);
                return geminiNonZero > docAiNonZero ? geminiResult : docAiResult;
            }

            return geminiResult.ConfidenceScore > 0 ? geminiResult : (docAiResult ?? geminiResult);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini fallback also failed for {Key}", documentS3Key);
            return docAiResult ?? new TextExtractionResult(
                documentS3Key, new Dictionary<string, string>(),
                new Dictionary<string, TableData>(), 0f, DateTimeOffset.UtcNow);
        }
    }

    private static string GetTextFromLayout(Document.Types.Page.Types.Layout? layout, string fullText)
    {
        if (layout?.TextAnchor?.TextSegments == null || !layout.TextAnchor.TextSegments.Any())
            return "";

        var segments = layout.TextAnchor.TextSegments;
        var result = string.Join("", segments.Select(s =>
            fullText.Substring((int)s.StartIndex, (int)(s.EndIndex - s.StartIndex))));
        return result.Trim();
    }

    private bool IsResultAcceptable(TextExtractionResult result)
    {
        if (result.ConfidenceScore < MinConfidence) return false;
        return CountZeroNutrients(result.ExtractedFields) < MaxZeroFieldsBeforeFallback;
    }

    private static int CountZeroNutrients(Dictionary<string, string> fields)
    {
        int zeroCount = 0;
        foreach (var key in NutrientKeys)
        {
            var found = false;
            foreach (var field in fields)
            {
                if (field.Key.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    if (string.IsNullOrWhiteSpace(field.Value) ||
                        (float.TryParse(field.Value.Trim(), out var val) && val == 0))
                        zeroCount++;
                    break;
                }
            }
            if (!found) zeroCount++;
        }
        return zeroCount;
    }

    private static int CountNonZeroNutrients(Dictionary<string, string> fields)
        => NutrientKeys.Length - CountZeroNutrients(fields);

    private async Task<TextExtractionResult> ExtractTextFromTxtFileAsync(
        string documentS3Key, CancellationToken cancellationToken)
    {
        var stream = await _storageService.DownloadAsync(documentS3Key, "", cancellationToken);
        using var reader = new StreamReader(stream);
        var textContent = await reader.ReadToEndAsync(cancellationToken);

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = textContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            foreach (var separator in new[] { ':', '=' })
            {
                var parts = line.Split(new[] { separator }, 2);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    fields[parts[0].Trim()] = parts[1].Trim();
                    break;
                }
            }
        }

        return new TextExtractionResult(documentS3Key, fields,
            new Dictionary<string, TableData>(), 100.0f, DateTimeOffset.UtcNow);
    }
}
