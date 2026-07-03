namespace KisanMitraAI.Core.MultiLanguage;

using KisanMitraAI.Core.Models;

/// <summary>
/// Service for managing farmer language and dialect preferences
/// </summary>
public interface ILanguagePreferenceService
{
    /// <summary>
    /// Saves a farmer's language and dialect preference
    /// </summary>
    /// <param name="farmerId">The farmer's unique identifier</param>
    /// <param name="language">The preferred language</param>
    /// <param name="dialect">The preferred dialect (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SavePreferenceAsync(
        string farmerId,
        Language language,
        Dialect? dialect,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a farmer's language and dialect preference
    /// </summary>
    /// <param name="farmerId">The farmer's unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The farmer's language preference, or null if not set</returns>
    Task<LanguagePreference?> GetPreferenceAsync(
        string farmerId,
        CancellationToken cancellationToken = default);
}
