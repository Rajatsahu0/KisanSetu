using Google.Cloud.Firestore;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Infrastructure.Repositories.Timestream;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.Repositories.Firestore;

public class FirestoreGradingHistoryRepository : IGradingHistoryRepository
{
    private readonly FirestoreDb _db;
    private readonly FirestoreConfiguration _config;
    private readonly ILogger<FirestoreGradingHistoryRepository> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public FirestoreGradingHistoryRepository(FirestoreDb db, FirestoreConfiguration config, ILogger<FirestoreGradingHistoryRepository> logger)
    { _db = db; _config = config; _logger = logger; }

    public async Task StoreGradingAsync(GradingRecord record, CancellationToken ct = default)
    {
        var docId = $"{record.FarmerId}_{record.Timestamp:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
        // Serialize to dictionary for Firestore (avoids needing [FirestoreData] on complex nested types)
        var data = new Dictionary<string, object>
        {
            ["RecordId"] = record.RecordId,
            ["FarmerId"] = record.FarmerId,
            ["ProduceType"] = record.ProduceType,
            ["Grade"] = record.Grade.ToString(),
            ["CertifiedPrice"] = (double)record.CertifiedPrice,
            ["ImageS3Key"] = record.ImageS3Key ?? "",
            ["Timestamp"] = record.Timestamp.UtcDateTime,
            ["AnalysisJson"] = JsonSerializer.Serialize(record.Analysis)
        };
        await _db.Collection(_config.GradingHistoryCollection).Document(docId).SetAsync(data, cancellationToken: ct);
        _logger.LogInformation("Stored grading record {RecordId} for farmer {FarmerId}", record.RecordId, record.FarmerId);
    }

    public async Task<IEnumerable<GradingRecord>> GetGradingHistoryAsync(
        string farmerId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, CancellationToken ct = default)
    {
        var query = _db.Collection(_config.GradingHistoryCollection).WhereEqualTo("FarmerId", farmerId);
        var snapshot = await query.GetSnapshotAsync(ct);
        var results = new List<GradingRecord>();
        foreach (var doc in snapshot.Documents)
        {
            try
            {
                var dict = doc.ToDictionary();
                var gradeStr = dict.GetValueOrDefault("Grade")?.ToString() ?? "FAQ";
                Enum.TryParse<QualityGrade>(gradeStr, out var grade);
                var analysisJson = dict.GetValueOrDefault("AnalysisJson")?.ToString() ?? "{}";
                var analysis = JsonSerializer.Deserialize<ImageAnalysisResult>(analysisJson, _jsonOptions)
                    ?? new ImageAnalysisResult(0, new ColorProfile("", 0, 0), new List<Defect>(), 0);
                
                results.Add(new GradingRecord(
                    dict.GetValueOrDefault("RecordId")?.ToString() ?? doc.Id,
                    dict.GetValueOrDefault("FarmerId")?.ToString() ?? farmerId,
                    dict.GetValueOrDefault("ProduceType")?.ToString() ?? "",
                    grade,
                    Convert.ToDecimal(dict.GetValueOrDefault("CertifiedPrice") ?? 0),
                    dict.GetValueOrDefault("ImageS3Key")?.ToString() ?? "",
                    ((Google.Cloud.Firestore.Timestamp)dict["Timestamp"]).ToDateTimeOffset(),
                    analysis));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize grading record {DocId}", doc.Id);
            }
        }
        return results;
    }
}
