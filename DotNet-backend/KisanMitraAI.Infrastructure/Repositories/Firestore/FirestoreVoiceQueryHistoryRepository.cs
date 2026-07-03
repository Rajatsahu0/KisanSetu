using Google.Cloud.Firestore;
using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Core.VoiceIntelligence.Models;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Repositories.Firestore;

public class FirestoreVoiceQueryHistoryRepository : IVoiceQueryHistoryRepository
{
    private readonly FirestoreDb _db;
    private readonly FirestoreConfiguration _config;
    private readonly ILogger<FirestoreVoiceQueryHistoryRepository> _logger;

    public FirestoreVoiceQueryHistoryRepository(FirestoreDb db, FirestoreConfiguration config, ILogger<FirestoreVoiceQueryHistoryRepository> logger)
    { _db = db; _config = config; _logger = logger; }

    public async Task SaveQueryAsync(VoiceQueryHistoryItem item, CancellationToken ct = default)
    {
        var docRef = _db.Collection(_config.VoiceQueryHistoryCollection).Document(item.QueryId);
        await docRef.SetAsync(item, cancellationToken: ct);
    }

    public async Task<IEnumerable<VoiceQueryHistoryItem>> GetHistoryAsync(string farmerId, int limit = 50, CancellationToken ct = default)
    {
        var query = _db.Collection(_config.VoiceQueryHistoryCollection)
            .WhereEqualTo("FarmerId", farmerId)
            .OrderByDescending("Timestamp")
            .Limit(limit);
        var snapshot = await query.GetSnapshotAsync(ct);
        return snapshot.Documents.Select(d => d.ConvertTo<VoiceQueryHistoryItem>());
    }

    public async Task<IEnumerable<VoiceQueryHistoryItem>> GetFavoritesAsync(string farmerId, CancellationToken ct = default)
    {
        var query = _db.Collection(_config.VoiceQueryHistoryCollection)
            .WhereEqualTo("FarmerId", farmerId).WhereEqualTo("IsFavorite", true);
        var snapshot = await query.GetSnapshotAsync(ct);
        return snapshot.Documents.Select(d => d.ConvertTo<VoiceQueryHistoryItem>());
    }

    public async Task ToggleFavoriteAsync(string farmerId, string queryId, bool isFavorite, CancellationToken ct = default)
    {
        var docRef = _db.Collection(_config.VoiceQueryHistoryCollection).Document(queryId);
        await docRef.UpdateAsync("IsFavorite", isFavorite, cancellationToken: ct);
    }

    public async Task DeleteQueryAsync(string farmerId, string queryId, CancellationToken ct = default)
    {
        await _db.Collection(_config.VoiceQueryHistoryCollection).Document(queryId).DeleteAsync(cancellationToken: ct);
    }

    public async Task<VoiceQueryHistoryItem?> GetQueryByIdAsync(string farmerId, string queryId, CancellationToken ct = default)
    {
        var doc = await _db.Collection(_config.VoiceQueryHistoryCollection).Document(queryId).GetSnapshotAsync(ct);
        return doc.Exists ? doc.ConvertTo<VoiceQueryHistoryItem>() : null;
    }
}
