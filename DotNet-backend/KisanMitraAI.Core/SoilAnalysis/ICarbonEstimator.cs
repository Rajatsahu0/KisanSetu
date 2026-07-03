using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.SoilAnalysis;

public interface ICarbonEstimator
{
    Task<CarbonSequestrationEstimate> EstimateCarbonSequestrationAsync(
        RegenerativePlan plan,
        SoilHealthData currentSoil,
        CancellationToken cancellationToken);
}
