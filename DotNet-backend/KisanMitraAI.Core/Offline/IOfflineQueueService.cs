namespace KisanMitraAI.Core.Offline;

/// <summary>
/// Service for queueing operations when offline
/// </summary>
public interface IOfflineQueueService
{
    /// <summary>
    /// Enqueue an operation for later processing
    /// </summary>
    Task EnqueueAsync(
        string farmerId,
        OfflineOperation operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process all queued operations for a farmer
    /// </summary>
    Task ProcessQueueAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get queue depth (number of pending operations)
    /// </summary>
    Task<int> GetQueueDepthAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total queue size in bytes
    /// </summary>
    Task<long> GetQueueSizeAsync(
        string farmerId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an operation queued for offline processing
/// </summary>
public record OfflineOperation(
    string OperationId,
    string FarmerId,
    OfflineOperationType OperationType,
    string Payload,
    DateTimeOffset QueuedAt,
    int RetryCount = 0,
    DateTimeOffset? LastRetryAt = null);

/// <summary>
/// Types of operations that can be queued offline
/// </summary>
public enum OfflineOperationType
{
    VoiceQuery,
    ImageUpload,
    DocumentUpload
}
