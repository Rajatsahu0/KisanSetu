namespace KisanMitraAI.Infrastructure.Repositories.Firestore;

/// <summary>
/// Configuration for Firestore collections (replaces DynamoDBConfiguration)
/// </summary>
public class FirestoreConfiguration
{
    public string UserProfileCollection { get; set; } = "users";
    public string OfflineQueueCollection { get; set; } = "offlineQueue";
    public string SessionCollection { get; set; } = "sessions";
    public string SoilDataCollection { get; set; } = "soilData";
    public string MandiPricesCollection { get; set; } = "mandiPrices";
    public string GradingHistoryCollection { get; set; } = "gradingHistory";
    public string VoiceQueryHistoryCollection { get; set; } = "voiceQueries";
    public string AuditLogsCollection { get; set; } = "auditLogs";

    // Session limits (same as before)
    public int MaxExchangesPerSession { get; set; } = 10;
    public int SessionExpirationHours { get; set; } = 24;
}
