namespace KisanMitraAI.Infrastructure.Repositories.PostgreSQL;

/// <summary>
/// Repository for audit logs in PostgreSQL
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Logs an action for compliance
    /// </summary>
    Task LogActionAsync(
        string farmerId,
        string action,
        string resourceType,
        string resourceId,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        string status = "Success",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit trail for a farmer
    /// </summary>
    Task<IEnumerable<AuditLogEntry>> GetAuditTrailAsync(
        string farmerId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an audit log entry
/// </summary>
public record AuditLogEntry(
    long LogId,
    string FarmerId,
    string Action,
    string ResourceType,
    string ResourceId,
    string? Details,
    string IpAddress,
    string UserAgent,
    DateTimeOffset Timestamp,
    string Status);
