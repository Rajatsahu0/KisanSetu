using Google.Cloud.Firestore;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Infrastructure.Repositories.Timestream;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Repositories.Firestore;

public class FirestoreMandiPricesRepository : IMandiPriceRepository
{
    private readonly FirestoreDb _db;
    private readonly FirestoreConfiguration _config;
    private readonly ILogger<FirestoreMandiPricesRepository> _logger;

    public FirestoreMandiPricesRepository(FirestoreDb db, FirestoreConfiguration config, ILogger<FirestoreMandiPricesRepository> logger)
    { _db = db; _config = config; _logger = logger; }

    public async Task StorePriceAsync(MandiPrice price, CancellationToken ct = default)
    {
        var docId = $"{price.Commodity}_{price.Location}_{price.PriceDate:yyyyMMdd}_{Guid.NewGuid():N}";
        var data = new Dictionary<string, object>
        {
            ["Commodity"] = price.Commodity ?? "",
            ["Location"] = price.Location ?? "",
            ["MandiName"] = price.MandiName ?? "",
            ["MinPrice"] = (double)price.MinPrice,
            ["MaxPrice"] = (double)price.MaxPrice,
            ["ModalPrice"] = (double)price.ModalPrice,
            ["Unit"] = price.Unit ?? "Quintal",
            ["PriceDate"] = price.PriceDate.UtcDateTime
        };
        await _db.Collection(_config.MandiPricesCollection).Document(docId).SetAsync(data, cancellationToken: ct);
    }

    public async Task<IEnumerable<MandiPrice>> GetCurrentPricesAsync(string commodity, string location, CancellationToken ct = default)
    {
        var query = _db.Collection(_config.MandiPricesCollection)
            .WhereEqualTo("Commodity", commodity).WhereEqualTo("Location", location)
            .Limit(10);
        var snapshot = await query.GetSnapshotAsync(ct);
        return snapshot.Documents.Select(d => DocToMandiPrice(d)).Where(p => p != null)!;
    }

    public async Task<IEnumerable<MandiPrice>> GetHistoricalPricesAsync(
        string commodity, string location, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default)
    {
        var query = _db.Collection(_config.MandiPricesCollection)
            .WhereEqualTo("Commodity", commodity).WhereEqualTo("Location", location);
        var snapshot = await query.GetSnapshotAsync(ct);
        return snapshot.Documents.Select(d => DocToMandiPrice(d)).Where(p => p != null)!;
    }

    public async Task<IEnumerable<MandiPrice>> GetPriceTrendsAsync(
        string commodity, string location, int daysBack = 30, CancellationToken ct = default)
    {
        var query = _db.Collection(_config.MandiPricesCollection)
            .WhereEqualTo("Commodity", commodity).WhereEqualTo("Location", location)
            .Limit(daysBack);
        var snapshot = await query.GetSnapshotAsync(ct);
        return snapshot.Documents.Select(d => DocToMandiPrice(d)).Where(p => p != null)!;
    }

    private static MandiPrice? DocToMandiPrice(DocumentSnapshot doc)
    {
        try
        {
            var dict = doc.ToDictionary();
            return new MandiPrice(
                dict.GetValueOrDefault("Commodity")?.ToString() ?? "",
                dict.GetValueOrDefault("Location")?.ToString() ?? "",
                dict.GetValueOrDefault("MandiName")?.ToString() ?? "",
                Convert.ToDecimal(dict.GetValueOrDefault("MinPrice") ?? 0),
                Convert.ToDecimal(dict.GetValueOrDefault("MaxPrice") ?? 0),
                Convert.ToDecimal(dict.GetValueOrDefault("ModalPrice") ?? 0),
                dict.ContainsKey("PriceDate") 
                    ? ((Google.Cloud.Firestore.Timestamp)dict["PriceDate"]).ToDateTimeOffset() 
                    : DateTimeOffset.UtcNow,
                dict.GetValueOrDefault("Unit")?.ToString() ?? "Quintal");
        }
        catch { return null; }
    }
}
