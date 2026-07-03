namespace KisanMitraAI.Core.Monitoring;

/// <summary>
/// Interface for publishing metrics to CloudWatch
/// </summary>
public interface IMetricsPublisher
{
    /// <summary>
    /// Publish a metric value
    /// </summary>
    Task PublishMetricAsync(string metricName, double value, MetricUnit unit, Dictionary<string, string>? dimensions = null);

    /// <summary>
    /// Publish API request count
    /// </summary>
    Task PublishApiRequestAsync(string endpoint, string method, int statusCode);

    /// <summary>
    /// Publish API latency
    /// </summary>
    Task PublishApiLatencyAsync(string endpoint, string method, long durationMs);

    /// <summary>
    /// Publish error rate
    /// </summary>
    Task PublishErrorAsync(string module, string operation, string errorCode);

    /// <summary>
    /// Publish AI service invocation
    /// </summary>
    Task PublishAiServiceInvocationAsync(string serviceName, bool success, long durationMs);

    /// <summary>
    /// Publish Step Function execution
    /// </summary>
    Task PublishStepFunctionExecutionAsync(string workflowName, string status, long durationMs);

    /// <summary>
    /// Publish queue depth
    /// </summary>
    Task PublishQueueDepthAsync(string queueName, int depth);

    /// <summary>
    /// Publish storage usage
    /// </summary>
    Task PublishStorageUsageAsync(string storageType, long bytesUsed);
}

/// <summary>
/// Metric units
/// </summary>
public enum MetricUnit
{
    None,
    Count,
    Milliseconds,
    Seconds,
    Bytes,
    Kilobytes,
    Megabytes,
    Percent
}
