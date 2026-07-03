namespace KisanMitraAI.Core.VoiceIntelligence;

/// <summary>
/// Service for handling offline voice query queueing
/// </summary>
public interface IOfflineQueueHandler
{
    /// <summary>
    /// Queues a voice query for processing when connectivity returns
    /// </summary>
    /// <param name="farmerId">ID of the farmer</param>
    /// <param name="audioS3Key">S3 key of the uploaded audio</param>
    /// <param name="dialect">Dialect of the audio</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue operation ID</returns>
    Task<string> QueueVoiceQueryAsync(
        string farmerId,
        string audioS3Key,
        string dialect,
        CancellationToken cancellationToken);

    /// <summary>
    /// Processes all queued voice queries for a farmer
    /// </summary>
    /// <param name="farmerId">ID of the farmer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of queries processed</returns>
    Task<int> ProcessQueuedQueriesAsync(
        string farmerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current queue depth for a farmer
    /// </summary>
    /// <param name="farmerId">ID of the farmer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of queued operations</returns>
    Task<int> GetQueueDepthAsync(
        string farmerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if offline mode is active
    /// </summary>
    /// <returns>True if offline, false otherwise</returns>
    Task<bool> IsOfflineAsync();
}
