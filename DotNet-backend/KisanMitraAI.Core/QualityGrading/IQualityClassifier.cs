using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.QualityGrading;

public interface IQualityClassifier
{
    Task<QualityGrade> ClassifyQualityAsync(
        ImageAnalysisResult analysis,
        string produceType,
        CancellationToken cancellationToken = default);
}
