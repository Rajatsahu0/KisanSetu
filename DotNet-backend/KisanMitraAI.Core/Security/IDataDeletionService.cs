namespace KisanMitraAI.Core.Security;

/// <summary>
/// Service for handling GDPR-compliant data deletion requests
/// </summary>
public interface IDataDeletionService
{
    /// <summary>
    /// Deletes all personal data for a farmer from all storage systems
    /// </summary>
    /// <param name="farmerId">ID of the farmer whose data should be deleted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion report with details of deleted data</returns>
    Task<DataDeletionReport> DeleteFarmerDataAsync(string farmerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a data deletion request for processing within 30 days
    /// </summary>
    /// <param name="farmerId">ID of the farmer whose data should be deleted</param>
    /// <param name="requestedBy">User who requested the deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion request ID</returns>
    Task<string> ScheduleDeletionAsync(
        string farmerId, 
        string requestedBy, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a deletion request
    /// </summary>
    /// <param name="requestId">Deletion request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion request status</returns>
    Task<DeletionRequestStatus> GetDeletionStatusAsync(
        string requestId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Report of data deletion operation
/// </summary>
public record DataDeletionReport(
    string FarmerId,
    DateTimeOffset DeletedAt,
    Dictionary<string, int> DeletedRecordCounts,
    List<string> PreservedAuditLogs,
    bool IsComplete,
    string? ErrorMessage);

/// <summary>
/// Status of a data deletion request
/// </summary>
public record DeletionRequestStatus(
    string RequestId,
    string FarmerId,
    DeletionStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ScheduledFor,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage);

/// <summary>
/// Status of a deletion request
/// </summary>
public enum DeletionStatus
{
    Pending,
    Scheduled,
    InProgress,
    Completed,
    Failed
}
