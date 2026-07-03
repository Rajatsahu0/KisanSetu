using Google.Cloud.Vision.V1;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.QualityGrading;
using KisanMitraAI.Infrastructure.AI;
using KisanMitraAI.Infrastructure.Storage.S3;
using Microsoft.Extensions.Logging;
using CoreBoundingBox = KisanMitraAI.Core.Models.BoundingBox;

namespace KisanMitraAI.Infrastructure.Vision;

/// <summary>
/// Image analysis pipeline using Google Cloud Vision + Gemini:
///   1. Cloud Vision — validates image contains food/produce (label detection)
///   2. Gemini Vision — grades produce quality (freshness, defects, color, size, ripeness)
///   3. If Gemini fails — falls back to Vision-only basic analysis
/// </summary>
public class ImageAnalyzer : IImageAnalyzer
{
    private readonly ImageAnnotatorClient _visionClient;
    private readonly IS3StorageService _storageService;
    private readonly BedrockVisionGrader? _visionGrader;
    private readonly ILogger<ImageAnalyzer> _logger;

    private static readonly HashSet<string> FoodLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "food", "fruit", "vegetable", "produce", "plant", "crop",
        "tomato", "potato", "onion", "apple", "banana", "mango", "rice", "wheat",
        "cauliflower", "cabbage", "brinjal", "capsicum", "carrot", "peas",
        "grapes", "pomegranate", "guava", "papaya", "lemon", "orange",
        "grocery", "natural foods", "local food", "superfood", "ingredient"
    };

    public ImageAnalyzer(
        ImageAnnotatorClient visionClient,
        IS3StorageService storageService,
        ILogger<ImageAnalyzer> logger,
        BedrockVisionGrader? visionGrader = null)
    {
        _visionClient = visionClient;
        _storageService = storageService;
        _logger = logger;
        _visionGrader = visionGrader;
    }

    public Task<ImageAnalysisResult> AnalyzeImageAsync(
        string imageS3Key, CancellationToken cancellationToken = default)
        => AnalyzeImageAsync(imageS3Key, string.Empty, "Hindi", cancellationToken);

    public Task<ImageAnalysisResult> AnalyzeImageAsync(
        string imageS3Key, string produceType, CancellationToken cancellationToken = default)
        => AnalyzeImageAsync(imageS3Key, produceType, "Hindi", cancellationToken);

    public async Task<ImageAnalysisResult> AnalyzeImageAsync(
        string imageS3Key, string produceType, string language, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Download image from GCS
            var imageStream = await _storageService.DownloadAsync(imageS3Key, "", cancellationToken);
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms, cancellationToken);
            var imageBytes = ms.ToArray();

            // Step 2: Cloud Vision — label detection for validation
            var image = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);
            var labels = await _visionClient.DetectLabelsAsync(image, maxResults: 15);

            var isFoodImage = labels.Any(l => FoodLabels.Contains(l.Description));
            var visionConfidence = labels.Any() ? (float)labels.Average(l => l.Score) * 100f : 0f;

            _logger.LogInformation(
                "Vision validation for {Key}: IsFoodImage={IsFood}, Confidence={Confidence}, Labels=[{Labels}]",
                imageS3Key, isFoodImage, visionConfidence,
                string.Join(", ", labels.Take(5).Select(l => $"{l.Description}:{l.Score:F2}")));

            // Step 3: Gemini Vision — grade produce quality (primary grader)
            VisionGradeResult? visionResult = null;
            if (_visionGrader != null && !string.IsNullOrEmpty(produceType))
            {
                try
                {
                    visionResult = await _visionGrader.GradeImageAsync(imageS3Key, produceType, language, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gemini Vision grading failed for {Key}, falling back", imageS3Key);
                }
            }

            // Step 4: Build result
            if (visionResult != null)
            {
                return BuildGeminiResult(visionResult, visionConfidence, imageS3Key, produceType);
            }

            // Fallback: Vision-only (Gemini unavailable)
            return BuildVisionFallback(labels, visionConfidence, imageS3Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing {Key}", imageS3Key);
            throw new ImageAnalysisException("Image analysis failed. Please try again.", ex);
        }
    }

    private ImageAnalysisResult BuildGeminiResult(
        VisionGradeResult vision, float visionConfidence, string s3Key, string produceType)
    {
        var colorProfile = new ColorProfile("AI-assessed", vision.ColorQuality, 50f);
        var defects = new List<Defect>();
        if (vision.DefectScore < 80)
        {
            var severity = 100f - vision.DefectScore;
            var label = vision.DefectScore < 30 ? "Rot/Decay detected"
                : vision.DefectScore < 50 ? "Significant defects detected"
                : "Minor defects detected";
            defects.Add(new Defect(label, severity, new CoreBoundingBox(0, 0, 1, 1), severity));
        }

        var confidence = Math.Min((vision.Freshness * 0.7f + visionConfidence * 0.3f), 100f);

        return new ImageAnalysisResult(
            averageSize: vision.SizeUniformity,
            colorProfile: colorProfile,
            defects: defects,
            confidenceScore: confidence,
            visionGrade: vision.OverallGrade,
            visionReasoning: vision.Reasoning,
            visionFreshness: vision.Freshness,
            visionDefectScore: vision.DefectScore,
            visionColorQuality: vision.ColorQuality,
            visionSizeUniformity: vision.SizeUniformity,
            visionRipeness: vision.Ripeness);
    }

    private ImageAnalysisResult BuildVisionFallback(
        IReadOnlyList<EntityAnnotation> labels, float confidence, string s3Key)
    {
        _logger.LogWarning("Vision FALLBACK for {Key}. Gemini was unavailable.", s3Key);
        return new ImageAnalysisResult(
            averageSize: 50f,
            colorProfile: new ColorProfile("Vision", 50f, 50f),
            defects: new List<Defect>(),
            confidenceScore: confidence);
    }
}

public class ImageAnalysisException : Exception
{
    public ImageAnalysisException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
