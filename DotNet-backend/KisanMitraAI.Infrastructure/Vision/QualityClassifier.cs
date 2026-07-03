using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.QualityGrading;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Vision;

public class QualityClassifier : IQualityClassifier
{
    private readonly ILogger<QualityClassifier> _logger;

    // AGMARK-aligned composite score weights
    private const float W_FRESHNESS = 0.25f;
    private const float W_COLOR = 0.20f;
    private const float W_SIZE = 0.15f;
    private const float W_DEFECT = 0.25f;
    private const float W_RIPENESS = 0.15f;

    // Rekognition-only fallback thresholds
    private static readonly GradingCriteria BulkVegetableCriteria = new() { MinSizeForA = 50, MinSizeForB = 30, MinSizeForC = 15, MaxDefectForA = 12, MaxDefectForB = 32, MaxDefectForC = 52, MinColorForA = 55, MinColorForB = 35, MinColorForC = 20 };
    private static readonly GradingCriteria FruitCriteria = new() { MinSizeForA = 45, MinSizeForB = 28, MinSizeForC = 12, MaxDefectForA = 8, MaxDefectForB = 25, MaxDefectForC = 45, MinColorForA = 58, MinColorForB = 40, MinColorForC = 25 };
    private static readonly GradingCriteria GrainCriteria = new() { MinSizeForA = 40, MinSizeForB = 25, MinSizeForC = 10, MaxDefectForA = 15, MaxDefectForB = 35, MaxDefectForC = 55, MinColorForA = 50, MinColorForB = 32, MinColorForC = 18 };
    private static readonly GradingCriteria SpiceCriteria = new() { MinSizeForA = 35, MinSizeForB = 20, MinSizeForC = 10, MaxDefectForA = 10, MaxDefectForB = 28, MaxDefectForC = 48, MinColorForA = 60, MinColorForB = 42, MinColorForC = 25 };
    private static readonly GradingCriteria DefaultCriteria = new() { MinSizeForA = 45, MinSizeForB = 28, MinSizeForC = 14, MaxDefectForA = 12, MaxDefectForB = 32, MaxDefectForC = 52, MinColorForA = 55, MinColorForB = 38, MinColorForC = 22 };

    private static readonly Dictionary<string, GradingCriteria> ProduceCriteria = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tomato"] = new() { MinSizeForA = 50, MinSizeForB = 30, MinSizeForC = 15, MaxDefectForA = 10, MaxDefectForB = 30, MaxDefectForC = 50, MinColorForA = 60, MinColorForB = 40, MinColorForC = 20 },
        ["onion"] = new() { MinSizeForA = 50, MinSizeForB = 30, MinSizeForC = 15, MaxDefectForA = 15, MaxDefectForB = 35, MaxDefectForC = 55, MinColorForA = 55, MinColorForB = 35, MinColorForC = 20 },
        ["potato"] = new() { MinSizeForA = 50, MinSizeForB = 30, MinSizeForC = 15, MaxDefectForA = 12, MaxDefectForB = 32, MaxDefectForC = 52, MinColorForA = 50, MinColorForB = 30, MinColorForC = 20 },
        ["cauliflower"] = BulkVegetableCriteria, ["cabbage"] = BulkVegetableCriteria,
        ["brinjal"] = BulkVegetableCriteria, ["capsicum"] = BulkVegetableCriteria,
        ["carrot"] = BulkVegetableCriteria, ["peas"] = BulkVegetableCriteria,
        ["ladyfinger"] = BulkVegetableCriteria, ["okra"] = BulkVegetableCriteria,
        ["mango"] = FruitCriteria, ["apple"] = FruitCriteria, ["banana"] = FruitCriteria,
        ["grapes"] = FruitCriteria, ["pomegranate"] = FruitCriteria, ["guava"] = FruitCriteria,
        ["papaya"] = FruitCriteria, ["lemon"] = FruitCriteria, ["orange"] = FruitCriteria,
        ["watermelon"] = FruitCriteria,
        ["wheat"] = GrainCriteria, ["rice"] = GrainCriteria, ["corn"] = GrainCriteria,
        ["maize"] = GrainCriteria, ["bajra"] = GrainCriteria, ["jowar"] = GrainCriteria,
        ["ragi"] = GrainCriteria,
        ["chilli"] = SpiceCriteria, ["turmeric"] = SpiceCriteria, ["ginger"] = SpiceCriteria,
        ["coriander"] = SpiceCriteria, ["cumin"] = SpiceCriteria,
    };

    public QualityClassifier(ILogger<QualityClassifier> logger) => _logger = logger;

    public Task<QualityGrade> ClassifyQualityAsync(
        ImageAnalysisResult analysis, string produceType,
        CancellationToken cancellationToken = default)
    {
        QualityGrade grade;

        // PRIMARY: Composite score from Nova vision scores (when available)
        if (analysis.VisionDefectScore.HasValue && analysis.VisionFreshness.HasValue)
        {
            grade = ComputeCompositeGrade(analysis, produceType);
        }
        else
        {
            // FALLBACK: Rekognition-only threshold grading
            grade = GradeFromRekognition(analysis, produceType);
        }

        return Task.FromResult(grade);
    }

    private QualityGrade ComputeCompositeGrade(ImageAnalysisResult analysis, string produceType)
    {
        var freshness = analysis.VisionFreshness!.Value;
        var defectScore = analysis.VisionDefectScore!.Value;
        var colorQuality = analysis.VisionColorQuality ?? 70f;
        var sizeUniformity = analysis.VisionSizeUniformity ?? analysis.AverageSize;
        var ripeness = analysis.VisionRipeness ?? 70f;

        // === VETO RULES (hard limits — override everything) ===

        // Heavy defects = Reject regardless of other scores
        if (defectScore < 30)
        {
            _logger.LogInformation(
                "REJECT (veto): {ProduceType} — DefectScore={Defect} < 30 (heavy rot/mold/damage). " +
                "Nova said '{VisionGrade}' but scores say Reject.",
                produceType, defectScore, analysis.VisionGrade);
            return QualityGrade.Reject;
        }

        // Both defective AND not fresh = Reject
        if (defectScore < 60 && freshness < 60)
        {
            _logger.LogInformation(
                "REJECT (veto): {ProduceType} — DefectScore={Defect} AND Freshness={Fresh} both below 60. " +
                "Nova said '{VisionGrade}' but combined scores say Reject.",
                produceType, defectScore, freshness, analysis.VisionGrade);
            return QualityGrade.Reject;
        }

        // Significant defects = cap at NonFAQ max
        var defectCap = defectScore < 50 ? QualityGrade.NonFAQ : (QualityGrade?)null;

        // Very low freshness = cap at NonFAQ max
        var freshnessCap = freshness < 40 ? QualityGrade.NonFAQ : (QualityGrade?)null;

        // === COMPOSITE SCORE ===
        var composite = freshness * W_FRESHNESS
                      + colorQuality * W_COLOR
                      + sizeUniformity * W_SIZE
                      + defectScore * W_DEFECT
                      + ripeness * W_RIPENESS;

        // Map composite to grade
        var compositeGrade = composite switch
        {
            >= 80 => QualityGrade.A,
            >= 70 => QualityGrade.B,
            >= 55 => QualityGrade.FAQ,
            >= 40 => QualityGrade.C,
            >= 25 => QualityGrade.NonFAQ,
            _ => QualityGrade.Reject
        };

        // Apply veto caps
        var finalGrade = compositeGrade;
        if (defectCap.HasValue && GradeRank(finalGrade) > GradeRank(defectCap.Value))
        {
            _logger.LogInformation("Grade capped by defects: {Original} → {Capped} (DefectScore={Defect})",
                finalGrade, defectCap.Value, defectScore);
            finalGrade = defectCap.Value;
        }
        if (freshnessCap.HasValue && GradeRank(finalGrade) > GradeRank(freshnessCap.Value))
        {
            _logger.LogInformation("Grade capped by freshness: {Original} → {Capped} (Freshness={Fresh})",
                finalGrade, freshnessCap.Value, freshness);
            finalGrade = freshnessCap.Value;
        }

        _logger.LogInformation(
            "Composite grading for {ProduceType}: Freshness={Fresh}, Color={Color}, Size={Size}, " +
            "Defects={Defect}, Ripeness={Ripe} → Composite={Composite:.1f} → {CompositeGrade}" +
            "{VetoCap}. Nova said '{VisionGrade}'.",
            produceType, freshness, colorQuality, sizeUniformity, defectScore, ripeness,
            composite, compositeGrade,
            finalGrade != compositeGrade ? $" → CAPPED to {finalGrade}" : "",
            analysis.VisionGrade);

        return finalGrade;
    }

    private static int GradeRank(QualityGrade g) => g switch
    {
        QualityGrade.A => 6,
        QualityGrade.B => 5,
        QualityGrade.FAQ => 4,
        QualityGrade.C => 3,
        QualityGrade.NonFAQ => 2,
        QualityGrade.Reject => 1,
        _ => 0
    };

    private QualityGrade GradeFromRekognition(ImageAnalysisResult analysis, string produceType)
    {
        var criteria = ProduceCriteria.GetValueOrDefault(produceType, DefaultCriteria);
        var defects = analysis.Defects.Any() ? analysis.Defects.Average(d => d.Severity) : 0f;
        var size = analysis.AverageSize;
        var color = analysis.ColorProfile.ColorUniformity;

        if (analysis.ConfidenceScore < 40)
        {
            _logger.LogWarning("Rekognition confidence too low: {Confidence}", analysis.ConfidenceScore);
            return QualityGrade.Reject;
        }

        QualityGrade grade;
        if (size >= criteria.MinSizeForA && defects <= criteria.MaxDefectForA && color >= criteria.MinColorForA)
            grade = QualityGrade.A;
        else if (size >= criteria.MinSizeForB && defects <= criteria.MaxDefectForB && color >= criteria.MinColorForB)
            grade = QualityGrade.B;
        else if (size >= criteria.MinSizeForC && defects <= criteria.MaxDefectForC && color >= criteria.MinColorForC)
            grade = QualityGrade.FAQ;
        else if (size >= 5 && analysis.ConfidenceScore >= 50)
            grade = QualityGrade.NonFAQ;
        else
            grade = QualityGrade.Reject;

        _logger.LogInformation(
            "Rekognition fallback for {ProduceType}: Size={Size}, Defects={Defects}, Color={Color} → {Grade}",
            produceType, size, defects, color, grade);

        return grade;
    }

    private class GradingCriteria
    {
        public float MinSizeForA { get; init; }
        public float MinSizeForB { get; init; }
        public float MinSizeForC { get; init; }
        public float MaxDefectForA { get; init; }
        public float MaxDefectForB { get; init; }
        public float MaxDefectForC { get; init; }
        public float MinColorForA { get; init; }
        public float MinColorForB { get; init; }
        public float MinColorForC { get; init; }
    }
}
