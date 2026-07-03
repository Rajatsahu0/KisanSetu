using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.QualityGrading;

public interface IBatchGradingAggregator
{
    Task<BatchGradingResult> AggregateBatchGradingAsync(
        IEnumerable<IndividualGradingResult> individualResults,
        string batchId,
        CancellationToken cancellationToken = default);
}

public record IndividualGradingResult(
    string ImageS3Key,
    QualityGrade Grade,
    ImageAnalysisResult Analysis,
    decimal CertifiedPrice,
    float ConfidenceScore);

public record BatchGradingResult(
    string BatchId,
    QualityGrade AggregatedGrade,
    decimal BatchCertifiedPrice,
    int TotalImages,
    IEnumerable<string> ImageS3Keys,
    float AverageConfidenceScore,
    string AggregationMethod);
