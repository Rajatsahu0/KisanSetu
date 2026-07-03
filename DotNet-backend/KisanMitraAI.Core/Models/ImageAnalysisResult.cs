namespace KisanMitraAI.Core.Models;

/// <summary>
/// Domain model representing image analysis result
/// </summary>
public record ImageAnalysisResult
{
    public float AverageSize { get; init; }
    public ColorProfile ColorProfile { get; init; }
    public IEnumerable<Defect> Defects { get; init; }
    public float ConfidenceScore { get; init; }

    /// <summary>
    /// AI vision grade from Bedrock Nova (null if vision grading unavailable).
    /// When present, QualityClassifier uses this as primary grading signal.
    /// </summary>
    public string? VisionGrade { get; init; }

    /// <summary>
    /// AI reasoning for the vision grade (e.g., "Fresh tomatoes with uniform red color")
    /// </summary>
    public string? VisionReasoning { get; init; }

    /// <summary>Nova vision scores (0-100) for composite scoring cross-validation</summary>
    public float? VisionFreshness { get; init; }
    public float? VisionDefectScore { get; init; }
    public float? VisionColorQuality { get; init; }
    public float? VisionSizeUniformity { get; init; }
    public float? VisionRipeness { get; init; }

    public ImageAnalysisResult(
        float averageSize,
        ColorProfile colorProfile,
        IEnumerable<Defect> defects,
        float confidenceScore,
        string? visionGrade = null,
        string? visionReasoning = null,
        float? visionFreshness = null,
        float? visionDefectScore = null,
        float? visionColorQuality = null,
        float? visionSizeUniformity = null,
        float? visionRipeness = null)
    {
        if (averageSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(averageSize), 
                "Average size cannot be negative");
        }

        if (confidenceScore < 0 || confidenceScore > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(confidenceScore), 
                "Confidence score must be between 0 and 100");
        }

        AverageSize = averageSize;
        ColorProfile = colorProfile ?? throw new ArgumentNullException(nameof(colorProfile));
        Defects = defects ?? Enumerable.Empty<Defect>();
        ConfidenceScore = confidenceScore;
        VisionGrade = visionGrade;
        VisionReasoning = visionReasoning;
        VisionFreshness = visionFreshness;
        VisionDefectScore = visionDefectScore;
        VisionColorQuality = visionColorQuality;
        VisionSizeUniformity = visionSizeUniformity;
        VisionRipeness = visionRipeness;
    }
}

/// <summary>
/// Domain model representing color profile
/// </summary>
public record ColorProfile
{
    public string DominantColor { get; init; }
    public float ColorUniformity { get; init; }
    public float Brightness { get; init; }

    public ColorProfile(
        string dominantColor,
        float colorUniformity,
        float brightness)
    {
        if (string.IsNullOrWhiteSpace(dominantColor))
        {
            throw new ArgumentException("Dominant color cannot be null or empty", 
                nameof(dominantColor));
        }

        if (colorUniformity < 0 || colorUniformity > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(colorUniformity), 
                "Color uniformity must be between 0 and 100");
        }

        if (brightness < 0 || brightness > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(brightness), 
                "Brightness must be between 0 and 100");
        }

        DominantColor = dominantColor;
        ColorUniformity = colorUniformity;
        Brightness = brightness;
    }
}

/// <summary>
/// Domain model representing a defect
/// </summary>
public record Defect
{
    public string DefectType { get; init; }
    public float Severity { get; init; }
    public BoundingBox Location { get; init; }
    public float Confidence { get; init; }

    public Defect(
        string defectType,
        float severity,
        BoundingBox location,
        float confidence)
    {
        if (string.IsNullOrWhiteSpace(defectType))
        {
            throw new ArgumentException("Defect type cannot be null or empty", 
                nameof(defectType));
        }

        if (severity < 0 || severity > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(severity), 
                "Severity must be between 0 and 100");
        }

        if (confidence < 0 || confidence > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), 
                "Confidence must be between 0 and 100");
        }

        DefectType = defectType;
        Severity = severity;
        Location = location ?? throw new ArgumentNullException(nameof(location));
        Confidence = confidence;
    }
}
