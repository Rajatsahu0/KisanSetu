using KisanMitraAI.Core.VoiceIntelligence.Models;

namespace KisanMitraAI.Core.VoiceIntelligence;

/// <summary>
/// Repository interface for voice query history
/// </summary>
public interface IVoiceQueryHistoryRepository
{
    /// <summary>
    /// Save a voice query to history
    /// </summary>
    Task SaveQueryAsync(
        VoiceQueryHistoryItem item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get voice query history for a farmer
    /// </summary>
    Task<IEnumerable<VoiceQueryHistoryItem>> GetHistoryAsync(
        string farmerId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get favorite queries for a farmer
    /// </summary>
    Task<IEnumerable<VoiceQueryHistoryItem>> GetFavoritesAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggle favorite status for a query
    /// </summary>
    Task ToggleFavoriteAsync(
        string farmerId,
        string queryId,
        bool isFavorite,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a query from history
    /// </summary>
    Task DeleteQueryAsync(
        string farmerId,
        string queryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific query by ID
    /// </summary>
    Task<VoiceQueryHistoryItem?> GetQueryByIdAsync(
        string farmerId,
        string queryId,
        CancellationToken cancellationToken = default);
}
