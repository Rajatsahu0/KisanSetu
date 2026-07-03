using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Infrastructure.Repositories.PostgreSQL;

/// <summary>
/// Repository for regenerative plans in PostgreSQL
/// </summary>
public interface IRegenerativePlanRepository
{
    /// <summary>
    /// Saves a regenerative plan
    /// </summary>
    Task<string> SavePlanAsync(RegenerativePlan plan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a plan by ID
    /// </summary>
    Task<RegenerativePlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all plans for a farmer
    /// </summary>
    Task<IEnumerable<RegenerativePlan>> GetPlansByFarmerAsync(string farmerId, CancellationToken cancellationToken = default);
}
