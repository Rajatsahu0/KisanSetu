using Google.Cloud.Firestore;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Infrastructure.Repositories.DynamoDB;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Repositories.Firestore;

public class FirestoreUserProfileRepository : IUserProfileRepository
{
    private readonly FirestoreDb _db;
    private readonly FirestoreConfiguration _config;
    private readonly ILogger<FirestoreUserProfileRepository> _logger;

    public FirestoreUserProfileRepository(FirestoreDb db, FirestoreConfiguration config, ILogger<FirestoreUserProfileRepository> logger)
    {
        _db = db; _config = config; _logger = logger;
    }

    public async Task SaveProfileAsync(FarmerProfile profile, CancellationToken ct = default)
    {
        var docRef = _db.Collection(_config.UserProfileCollection).Document(profile.FarmerId);
        var data = new Dictionary<string, object>
        {
            ["FarmerId"] = profile.FarmerId,
            ["Name"] = profile.Name,
            ["PhoneNumber"] = profile.PhoneNumber,
            ["Email"] = profile.Email ?? "",
            ["PreferredLanguage"] = profile.PreferredLanguage.ToString(),
            ["PreferredDialect"] = profile.PreferredDialect?.ToString() ?? "",
            ["Location"] = profile.Location,
            ["RegisteredAt"] = profile.RegisteredAt.UtcDateTime,
            ["UpdatedAt"] = DateTime.UtcNow
        };
        await docRef.SetAsync(data, SetOptions.MergeAll, cancellationToken: ct);
        _logger.LogInformation("Saved profile for {FarmerId}", profile.FarmerId);
    }

    public async Task<FarmerProfile?> GetProfileAsync(string farmerId, CancellationToken ct = default)
    {
        var docRef = _db.Collection(_config.UserProfileCollection).Document(farmerId);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return null;

        try
        {
            var dict = snapshot.ToDictionary();
            var langStr = dict.GetValueOrDefault("PreferredLanguage")?.ToString() ?? "Hindi";
            Enum.TryParse<Language>(langStr, out var lang);
            
            Dialect? dialect = null;
            var dialectStr = dict.GetValueOrDefault("PreferredDialect")?.ToString();
            if (!string.IsNullOrEmpty(dialectStr) && Enum.TryParse<Dialect>(dialectStr, out var d))
                dialect = d;

            return new FarmerProfile(
                dict.GetValueOrDefault("FarmerId")?.ToString() ?? farmerId,
                dict.GetValueOrDefault("Name")?.ToString() ?? "",
                dict.GetValueOrDefault("PhoneNumber")?.ToString() ?? "",
                lang,
                dialect,
                dict.GetValueOrDefault("Location")?.ToString() ?? "",
                Enumerable.Empty<FarmProfile>(),
                dict.ContainsKey("RegisteredAt")
                    ? ((Timestamp)dict["RegisteredAt"]).ToDateTimeOffset()
                    : DateTimeOffset.UtcNow,
                dict.GetValueOrDefault("Email")?.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize profile for {FarmerId}", farmerId);
            return null;
        }
    }

    public async Task UpdatePreferencesAsync(string farmerId, string lang, string dialect, CancellationToken ct = default)
    {
        var docRef = _db.Collection(_config.UserProfileCollection).Document(farmerId);
        await docRef.SetAsync(new Dictionary<string, object>
        {
            ["PreferredLanguage"] = lang,
            ["PreferredDialect"] = dialect,
            ["UpdatedAt"] = DateTime.UtcNow
        }, SetOptions.MergeAll, cancellationToken: ct);
    }
}
