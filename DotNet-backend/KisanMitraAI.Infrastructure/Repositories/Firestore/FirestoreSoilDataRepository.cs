using Google.Cloud.Firestore;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Infrastructure.Repositories.Timestream;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Repositories.Firestore;

public class FirestoreSoilDataRepository : ISoilDataRepository
{
    private readonly FirestoreDb _db;
    private readonly FirestoreConfiguration _config;
    private readonly ILogger<FirestoreSoilDataRepository> _logger;

    public FirestoreSoilDataRepository(FirestoreDb db, FirestoreConfiguration config, ILogger<FirestoreSoilDataRepository> logger)
    { _db = db; _config = config; _logger = logger; }

    public async Task StoreSoilDataAsync(SoilHealthData soilData, CancellationToken ct = default)
    {
        var docId = $"{soilData.FarmerId}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        await _db.Collection(_config.SoilDataCollection).Document(docId).SetAsync(soilData, cancellationToken: ct);
        _logger.LogInformation("Stored soil data for {FarmerId}", soilData.FarmerId);
    }

    public async Task<IEnumerable<SoilHealthData>> GetSoilHistoryAsync(
        string farmerId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, CancellationToken ct = default)
    {
        var query = _db.Collection(_config.SoilDataCollection).WhereEqualTo("FarmerId", farmerId);
        var snapshot = await query.GetSnapshotAsync(ct);
        return snapshot.Documents.Select(d => d.ConvertTo<SoilHealthData>());
    }
}
