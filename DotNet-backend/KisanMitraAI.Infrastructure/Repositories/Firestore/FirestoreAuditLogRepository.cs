using Google.Cloud.Firestore;
using KisanMitraAI.Infrastructure.Repositories.PostgreSQL;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Repositories.Firestore;

public class FirestoreAuditLogRepository : IAuditLogRepository
{
    private readonly FirestoreDb _db;
    private readonly FirestoreConfiguration _config;
    private readonly ILogger<FirestoreAuditLogRepository> _logger;

    public FirestoreAuditLogRepository(FirestoreDb db, FirestoreConfiguration config, ILogger<FirestoreAuditLogRepository> logger)
    { _db = db; _config = config; _logger = logger; }

    public async Task LogActionAsync(string farmerId, string action, string resourceType,
        string resourceId, string? details = null, string? ipAddress = null,
        string? userAgent = null, string status = "Success", CancellationToken ct = default)
    {
        var entry = new Dictionary<string, object>
        {
            ["FarmerId"] = farmerId,
            ["Action"] = action,
            ["ResourceType"] = resourceType,
            ["ResourceId"] = resourceId,
            ["Details"] = details ?? "",
            ["IpAddress"] = ipAddress ?? "",
            ["UserAgent"] = userAgent ?? "",
            ["Status"] = status,
            ["Timestamp"] = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
        };
        await _db.Collection(_config.AuditLogsCollection).AddAsync(entry, ct);
    }

    public async Task<IEnumerable<AuditLogEntry>> GetAuditTrailAsync(
        string farmerId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, CancellationToken ct = default)
    {
        var query = _db.Collection(_config.AuditLogsCollection)
            .WhereEqualTo("FarmerId", farmerId)
            .OrderByDescending("Timestamp").Limit(100);
        var snapshot = await query.GetSnapshotAsync(ct);

        return snapshot.Documents.Select(d => new AuditLogEntry(
            0, d.GetValue<string>("FarmerId"), d.GetValue<string>("Action"),
            d.GetValue<string>("ResourceType"), d.GetValue<string>("ResourceId"),
            d.GetValue<string>("Details"), d.GetValue<string>("IpAddress"),
            d.GetValue<string>("UserAgent"),
            d.GetValue<Timestamp>("Timestamp").ToDateTimeOffset(), d.GetValue<string>("Status")));
    }
}
