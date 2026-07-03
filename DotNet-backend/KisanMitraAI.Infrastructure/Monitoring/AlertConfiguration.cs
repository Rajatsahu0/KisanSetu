namespace KisanMitraAI.Infrastructure.Monitoring;

/// <summary>
/// Configuration for CloudWatch alarms
/// </summary>
public class AlertConfiguration
{
    /// <summary>
    /// Error rate threshold (percentage)
    /// </summary>
    public double ErrorRateThreshold { get; set; } = 5.0;

    /// <summary>
    /// API latency p95 threshold (milliseconds)
    /// </summary>
    public double ApiLatencyP95Threshold { get; set; } = 10000;

    /// <summary>
    /// Step Function failure rate threshold (percentage)
    /// </summary>
    public double StepFunctionFailureRateThreshold { get; set; } = 10.0;

    /// <summary>
    /// Storage usage threshold (percentage of limit)
    /// </summary>
    public double StorageUsageThreshold { get; set; } = 80.0;

    /// <summary>
    /// Rate limit violations per hour threshold
    /// </summary>
    public int RateLimitViolationsThreshold { get; set; } = 100;

    /// <summary>
    /// SNS topic ARN for critical alerts
    /// </summary>
    public string? SnsTopicArn { get; set; }

    /// <summary>
    /// Evaluation periods for alarms
    /// </summary>
    public int EvaluationPeriods { get; set; } = 2;

    /// <summary>
    /// Data points to alarm
    /// </summary>
    public int DataPointsToAlarm { get; set; } = 2;
}

/// <summary>
/// Alarm definitions for CloudWatch
/// </summary>
public static class AlarmDefinitions
{
    public static List<AlarmDefinition> GetAlarmDefinitions(AlertConfiguration config)
    {
        return new List<AlarmDefinition>
        {
            new AlarmDefinition
            {
                AlarmName = "HighErrorRate",
                MetricName = "ErrorCount",
                Statistic = "Sum",
                Threshold = config.ErrorRateThreshold,
                ComparisonOperator = "GreaterThanThreshold",
                Period = 300, // 5 minutes
                EvaluationPeriods = config.EvaluationPeriods,
                DataPointsToAlarm = config.DataPointsToAlarm,
                Description = "Error rate exceeds 5%",
                SnsTopicArn = config.SnsTopicArn
            },
            new AlarmDefinition
            {
                AlarmName = "HighApiLatency",
                MetricName = "ApiLatency",
                Statistic = "p95",
                Threshold = config.ApiLatencyP95Threshold,
                ComparisonOperator = "GreaterThanThreshold",
                Period = 300,
                EvaluationPeriods = config.EvaluationPeriods,
                DataPointsToAlarm = config.DataPointsToAlarm,
                Description = "API latency p95 exceeds 10 seconds",
                SnsTopicArn = config.SnsTopicArn
            },
            new AlarmDefinition
            {
                AlarmName = "HighStepFunctionFailureRate",
                MetricName = "StepFunctionExecution",
                Statistic = "Sum",
                Threshold = config.StepFunctionFailureRateThreshold,
                ComparisonOperator = "GreaterThanThreshold",
                Period = 300,
                EvaluationPeriods = config.EvaluationPeriods,
                DataPointsToAlarm = config.DataPointsToAlarm,
                Description = "Step Function failure rate exceeds 10%",
                SnsTopicArn = config.SnsTopicArn,
                Dimensions = new Dictionary<string, string> { ["Status"] = "Failed" }
            },
            new AlarmDefinition
            {
                AlarmName = "StorageApproachingLimit",
                MetricName = "StorageUsage",
                Statistic = "Maximum",
                Threshold = config.StorageUsageThreshold,
                ComparisonOperator = "GreaterThanThreshold",
                Period = 3600, // 1 hour
                EvaluationPeriods = 1,
                DataPointsToAlarm = 1,
                Description = "Storage usage approaching limits (>80%)",
                SnsTopicArn = config.SnsTopicArn
            },
            new AlarmDefinition
            {
                AlarmName = "HighRateLimitViolations",
                MetricName = "ErrorCount",
                Statistic = "Sum",
                Threshold = config.RateLimitViolationsThreshold,
                ComparisonOperator = "GreaterThanThreshold",
                Period = 3600, // 1 hour
                EvaluationPeriods = 1,
                DataPointsToAlarm = 1,
                Description = "Rate limit violations exceed 100 per hour",
                SnsTopicArn = config.SnsTopicArn,
                Dimensions = new Dictionary<string, string> { ["ErrorCode"] = "RATE_LIMIT_EXCEEDED" }
            }
        };
    }
}

/// <summary>
/// Alarm definition
/// </summary>
public class AlarmDefinition
{
    public string AlarmName { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Statistic { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public string ComparisonOperator { get; set; } = string.Empty;
    public int Period { get; set; }
    public int EvaluationPeriods { get; set; }
    public int DataPointsToAlarm { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SnsTopicArn { get; set; }
    public Dictionary<string, string>? Dimensions { get; set; }
}
