using KisanMitraAI.Core.Offline;
using KisanMitraAI.Infrastructure.Repositories.DynamoDB;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.Offline;

/// <summary>
/// Implementation of offline queue service
/// </summary>
public class OfflineQueueService : IOfflineQueueService
{
    private readonly IOfflineQueueRepository _queueRepository;
    private readonly ILogger<OfflineQueueService> _logger;
    private const long MaxQueueSizeBytes = 50 * 1024 * 1024; // 50 MB
    private const int MaxRetryCount = 3;

    public OfflineQueueService(
        IOfflineQueueRepository queueRepository,
        ILogger<OfflineQueueService> logger)
    {
        _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnqueueAsync(
        string farmerId,
        OfflineOperation operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(farmerId))
            throw new ArgumentException("Farmer ID is required", nameof(farmerId));

        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        try
        {
            // Check current queue size
            var currentSize = await GetQueueSizeAsync(farmerId, cancellationToken);
            var operationSize = System.Text.Encoding.UTF8.GetByteCount(operation.Payload);

            if (currentSize + operationSize > MaxQueueSizeBytes)
            {
                _logger.LogWarning(
                    "Queue size limit would be exceeded for farmer {FarmerId}. Current: {CurrentSize}, New: {NewSize}, Limit: {Limit}",
                    farmerId, currentSize, operationSize, MaxQueueSizeBytes);
                throw new InvalidOperationException($"Queue size limit of {MaxQueueSizeBytes} bytes would be exceeded");
            }

            // Enqueue the operation
            await _queueRepository.EnqueueOperationAsync(
                farmerId,
                operation.OperationType.ToString(),
                operation.Payload,
                cancellationToken);

            _logger.LogInformation(
                "Enqueued {OperationType} operation {OperationId} for farmer {FarmerId}, size: {Size} bytes",
                operation.OperationType, operation.OperationId, farmerId, operationSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error enqueueing operation {OperationId} for farmer {FarmerId}",
                operation.OperationId, farmerId);
            throw;
        }
    }

    public async Task ProcessQueueAsync(
        string farmerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(farmerId))
            throw new ArgumentException("Farmer ID is required", nameof(farmerId));

        try
        {
            var operations = await _queueRepository.DequeueOperationsAsync(farmerId, 50, cancellationToken);
            var operationsList = operations.ToList();

            if (!operationsList.Any())
            {
                _logger.LogInformation("No queued operations to process for farmer {FarmerId}", farmerId);
                return;
            }

            _logger.LogInformation(
                "Processing {Count} queued operations for farmer {FarmerId}",
                operationsList.Count, farmerId);

            var processedCount = 0;
            var failedCount = 0;

            foreach (var operation in operationsList)
            {
                try
                {
                    // Parse operation type
                    if (!Enum.TryParse<OfflineOperationType>(operation.OperationType, out var operationType))
                    {
                        _logger.LogWarning(
                            "Unknown operation type {OperationType} for operation {OperationId}",
                            operation.OperationType, operation.OperationId);
                        continue;
                    }

                    // Process based on operation type
                    var success = await ProcessOperationAsync(
                        farmerId,
                        operationType,
                        operation.OperationData,
                        cancellationToken);

                    if (success)
                    {
                        // Remove from queue
                        await _queueRepository.DeleteOperationAsync(
                            farmerId,
                            operation.OperationId,
                            cancellationToken);
                        processedCount++;

                        _logger.LogInformation(
                            "Successfully processed operation {OperationId} of type {OperationType}",
                            operation.OperationId, operationType);
                    }
                    else
                    {
                        // Increment retry count
                        var newRetryCount = operation.RetryCount + 1;

                        if (newRetryCount >= MaxRetryCount)
                        {
                            _logger.LogWarning(
                                "Operation {OperationId} exceeded max retry count, removing from queue",
                                operation.OperationId);
                            
                            await _queueRepository.DeleteOperationAsync(
                                farmerId,
                                operation.OperationId,
                                cancellationToken);
                            failedCount++;
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Operation {OperationId} failed, retry count: {RetryCount}",
                                operation.OperationId, newRetryCount);
                            failedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing operation {OperationId}",
                        operation.OperationId);
                    failedCount++;
                }
            }

            _logger.LogInformation(
                "Queue processing complete for farmer {FarmerId}. Processed: {Processed}, Failed: {Failed}",
                farmerId, processedCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queue for farmer {FarmerId}", farmerId);
            throw;
        }
    }

    public async Task<int> GetQueueDepthAsync(
        string farmerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(farmerId))
            throw new ArgumentException("Farmer ID is required", nameof(farmerId));

        try
        {
            return await _queueRepository.GetQueueDepthAsync(farmerId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue depth for farmer {FarmerId}", farmerId);
            throw;
        }
    }

    public async Task<long> GetQueueSizeAsync(
        string farmerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(farmerId))
            throw new ArgumentException("Farmer ID is required", nameof(farmerId));

        try
        {
            var operations = await _queueRepository.DequeueOperationsAsync(farmerId, 50, cancellationToken);
            
            var totalSize = operations.Sum(op => 
                System.Text.Encoding.UTF8.GetByteCount(op.OperationData));

            return totalSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue size for farmer {FarmerId}", farmerId);
            throw;
        }
    }

    private async Task<bool> ProcessOperationAsync(
        string farmerId,
        OfflineOperationType operationType,
        string payload,
        CancellationToken cancellationToken)
    {
        try
        {
            // In a real implementation, this would delegate to the appropriate service
            // based on the operation type:
            // - VoiceQuery -> IVoiceQueryHandler
            // - ImageUpload -> IImageUploadHandler
            // - DocumentUpload -> IDocumentUploadHandler

            _logger.LogInformation(
                "Processing {OperationType} operation for farmer {FarmerId}",
                operationType, farmerId);

            // Simulate processing
            await Task.Delay(100, cancellationToken);

            // For now, return success
            // In production, this would return the actual result of processing
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing {OperationType} operation for farmer {FarmerId}",
                operationType, farmerId);
            return false;
        }
    }
}
