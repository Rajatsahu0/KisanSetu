using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.SoilAnalysis;

public interface IRegenerativePlanGenerator
{
    Task<RegenerativePlan> GeneratePlanAsync(
        SoilHealthData soilData,
        FarmProfile farmProfile,
        CancellationToken cancellationToken,
        string? language = null);
}
