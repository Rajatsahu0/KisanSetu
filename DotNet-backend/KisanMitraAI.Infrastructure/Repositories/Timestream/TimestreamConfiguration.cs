namespace KisanMitraAI.Infrastructure.Repositories.Timestream;

/// <summary>
/// Configuration for Timestream database
/// </summary>
public class TimestreamConfiguration
{
    public string DatabaseName { get; set; } = "KisanMitraAI";
    public string MandiPricesTableName { get; set; } = "MandiPrices";
    public string SoilDataTableName { get; set; } = "SoilData";
    public string GradingHistoryTableName { get; set; } = "GradingHistory";
    public string Region { get; set; } = "us-east-1";
    
    // Retention policies in days
    public int MandiPricesRetentionDays { get; set; } = 1825; // 5 years
    public int SoilDataRetentionDays { get; set; } = 3650; // 10 years
    public int GradingHistoryRetentionDays { get; set; } = 730; // 2 years
}
