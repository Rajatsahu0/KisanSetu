using Google.Cloud.Firestore;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Infrastructure.Repositories.PostgreSQL;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Repositories.Firestore;

public class FirestoreFarmRepository : IFarmRepository
{
    private readonly FirestoreDb _db;
    private readonly ILogger<FirestoreFarmRepository> _logger;
    private const string Collection = "farms";

    public FirestoreFarmRepository(FirestoreDb db, ILogger<FirestoreFarmRepository> logger)
    { _db = db; _logger = logger; }

    public async Task<string> CreateAsync(FarmProfile farm, CancellationToken ct = default)
    {
        var docRef = _db.Collection(Collection).Document(farm.FarmId);
        await docRef.SetAsync(farm, cancellationToken: ct);
        return farm.FarmId;
    }

    public async Task<FarmProfile?> GetByIdAsync(string farmId, CancellationToken ct = default)
    {
        var doc = await _db.Collection(Collection).Document(farmId).GetSnapshotAsync(ct);
        return doc.Exists ? doc.ConvertTo<FarmProfile>() : null;
    }

    public async Task<IEnumerable<FarmProfile>> GetByFarmerIdAsync(string farmerId, CancellationToken ct = default)
    {
        var query = _db.Collection(Collection).WhereEqualTo("FarmerId", farmerId);
        var snapshot = await query.GetSnapshotAsync(ct);
        return snapshot.Documents.Select(d => d.ConvertTo<FarmProfile>());
    }

    public async Task UpdateAsync(FarmProfile farm, CancellationToken ct = default)
    {
        await _db.Collection(Collection).Document(farm.FarmId).SetAsync(farm, SetOptions.MergeAll, ct);
    }

    public async Task DeleteAsync(string farmId, CancellationToken ct = default)
    {
        await _db.Collection(Collection).Document(farmId).DeleteAsync(cancellationToken: ct);
    }
}
