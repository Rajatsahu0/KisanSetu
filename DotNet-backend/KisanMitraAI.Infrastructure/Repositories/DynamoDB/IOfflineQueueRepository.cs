namespace KisanMitraAI.Infrastructure.Repositories.DynamoDB;

/// <summary>
/// Repository for offline operation queue in DynamoDB
/// </summary>
public interface IOfflineQueueRepository
{
    /// <summary>
    /// Enqueues an operation for later processing
    /// </summary>
    Task EnqueueOperationAsync(
        string farmerId,
        string operationType,
        string operationData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues operations for a farmer
    /// </summary>
    Task<IEnumerable<QueuedOperation>> DequeueOperationsAsync(
        string farmerId,
        int maxItems = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the queue depth (number of pending operations) for a farmer
    /// </summary>
    Task<int> GetQueueDepthAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a processed operation from the queue
    /// </summary>
    Task DeleteOperationAsync(
        string farmerId,
        string operationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a queued operation
/// </summary>
public record QueuedOperation(
    string OperationId,
    string FarmerId,
    string OperationType,
    string OperationData,
    DateTimeOffset QueuedAt,
    int RetryCount);
