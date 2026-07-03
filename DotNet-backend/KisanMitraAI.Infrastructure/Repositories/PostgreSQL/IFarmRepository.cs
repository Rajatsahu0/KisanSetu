using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Infrastructure.Repositories.PostgreSQL;

/// <summary>
/// Repository for farm data in PostgreSQL
/// </summary>
public interface IFarmRepository
{
    /// <summary>
    /// Creates a new farm
    /// </summary>
    Task<string> CreateAsync(FarmProfile farm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a farm by ID
    /// </summary>
    Task<FarmProfile?> GetByIdAsync(string farmId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all farms for a farmer
    /// </summary>
    Task<IEnumerable<FarmProfile>> GetByFarmerIdAsync(string farmerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a farm
    /// </summary>
    Task UpdateAsync(FarmProfile farm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a farm
    /// </summary>
    Task DeleteAsync(string farmId, CancellationToken cancellationToken = default);
}
