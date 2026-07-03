namespace KisanMitraAI.Infrastructure.Repositories.DynamoDB;

/// <summary>
/// Configuration for DynamoDB
/// </summary>
public class DynamoDBConfiguration
{
    public string Region { get; set; } = "us-east-1";
    public string UserProfileTableName { get; set; } = "KisanMitraAI-UserProfiles";
    public string OfflineQueueTableName { get; set; } = "KisanMitraAI-OfflineQueue";
    public string SessionTableName { get; set; } = "KisanMitraAI-Sessions";
    
    // Time-series history tables (30-day retention)
    public string? SoilDataHistoryTableName { get; set; }
    public string? MandiPricesHistoryTableName { get; set; }
    public string? GradingHistoryTableName { get; set; }
    
    // Session limits
    public int MaxExchangesPerSession { get; set; } = 10;
    public int SessionExpirationHours { get; set; } = 24;
    
    // Queue limits
    public long MaxQueueSizeBytes { get; set; } = 50 * 1024 * 1024; // 50 MB
}
