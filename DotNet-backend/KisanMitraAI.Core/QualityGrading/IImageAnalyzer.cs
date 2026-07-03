using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.QualityGrading;

public interface IImageAnalyzer
{
    Task<ImageAnalysisResult> AnalyzeImageAsync(
        string imageS3Key,
        CancellationToken cancellationToken = default);

    Task<ImageAnalysisResult> AnalyzeImageAsync(
        string imageS3Key,
        string produceType,
        CancellationToken cancellationToken = default);

    Task<ImageAnalysisResult> AnalyzeImageAsync(
        string imageS3Key,
        string produceType,
        string language,
        CancellationToken cancellationToken = default);
}
