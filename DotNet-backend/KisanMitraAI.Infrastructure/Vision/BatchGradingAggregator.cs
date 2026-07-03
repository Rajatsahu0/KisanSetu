using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.QualityGrading;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Vision;

public class BatchGradingAggregator : IBatchGradingAggregator
{
    private readonly ILogger<BatchGradingAggregator> _logger;

    // Grade weights for weighted average calculation
    private static readonly Dictionary<QualityGrade, int> GradeWeights = new()
    {
        [QualityGrade.A] = 4,
        [QualityGrade.B] = 3,
        [QualityGrade.C] = 2,
        [QualityGrade.Reject] = 1
    };

    public BatchGradingAggregator(ILogger<BatchGradingAggregator> logger)
    {
        _logger = logger;
    }

    public Task<BatchGradingResult> AggregateBatchGradingAsync(
        IEnumerable<IndividualGradingResult> individualResults,
        string batchId,
        CancellationToken cancellationToken = default)
    {
        var results = individualResults.ToList();

        if (!results.Any())
        {
            throw new ArgumentException("No individual grading results provided for batch aggregation", nameof(individualResults));
        }

        // Calculate weighted average grade based on confidence scores
        var aggregatedGrade = CalculateWeightedAverageGrade(results);

        // Calculate batch-level certified price (average of individual prices)
        var batchCertifiedPrice = results.Average(r => r.CertifiedPrice);

        // Calculate average confidence score
        var averageConfidenceScore = results.Average(r => r.ConfidenceScore);

        // Collect all image S3 keys
        var imageS3Keys = results.Select(r => r.ImageS3Key).ToList();

        _logger.LogInformation(
            "Batch grading aggregation completed for batch {BatchId}. " +
            "Total images: {TotalImages}, Aggregated grade: {Grade}, " +
            "Batch price: {Price}, Average confidence: {Confidence}",
            batchId, results.Count, aggregatedGrade, batchCertifiedPrice, averageConfidenceScore);

        var result = new BatchGradingResult(
            batchId,
            aggregatedGrade,
            batchCertifiedPrice,
            results.Count,
            imageS3Keys,
            averageConfidenceScore,
            "WeightedAverageByConfidence");

        return Task.FromResult(result);
    }

    private QualityGrade CalculateWeightedAverageGrade(List<IndividualGradingResult> results)
    {
        // Calculate weighted score for each result (grade weight * confidence)
        var weightedScores = results.Select(r => new
        {
            Result = r,
            WeightedScore = GradeWeights[r.Grade] * r.ConfidenceScore
        }).ToList();

        // Calculate total weighted score
        var totalWeightedScore = weightedScores.Sum(ws => ws.WeightedScore);
        var totalConfidence = results.Sum(r => r.ConfidenceScore);

        // Calculate average weighted grade value
        var averageGradeValue = totalWeightedScore / totalConfidence;

        // Map back to grade
        // A=4, B=3, C=2, Reject=1
        if (averageGradeValue >= 3.5)
            return QualityGrade.A;
        if (averageGradeValue >= 2.5)
            return QualityGrade.B;
        if (averageGradeValue >= 1.5)
            return QualityGrade.C;
        
        return QualityGrade.Reject;
    }
}
