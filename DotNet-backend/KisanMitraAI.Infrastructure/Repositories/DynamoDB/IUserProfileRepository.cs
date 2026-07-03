using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Infrastructure.Repositories.DynamoDB;

/// <summary>
/// Repository for user profile data in DynamoDB
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>
    /// Saves a farmer profile
    /// </summary>
    Task SaveProfileAsync(
        FarmerProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a farmer profile by ID
    /// </summary>
    Task<FarmerProfile?> GetProfileAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates farmer preferences (language, dialect)
    /// </summary>
    Task UpdatePreferencesAsync(
        string farmerId,
        string preferredLanguage,
        string preferredDialect,
        CancellationToken cancellationToken = default);
}
