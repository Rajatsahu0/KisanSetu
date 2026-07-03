using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.QualityGrading;

public interface IPriceCalculator
{
    Task<decimal> CalculateCertifiedPriceAsync(
        QualityGrade grade,
        string commodity,
        string location,
        CancellationToken cancellationToken = default);
}
