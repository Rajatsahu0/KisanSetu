# Monitoring and Alerting Infrastructure

This module provides comprehensive monitoring and alerting capabilities using AWS CloudWatch for the Kisan Mitra AI platform.

## Components

### IMetricsPublisher
Interface for publishing metrics to CloudWatch.

**Methods:**
- `PublishMetricAsync(name, value, unit, dimensions)` - Publish custom metric
- `PublishApiRequestAsync(endpoint, method, statusCode)` - Track API requests
- `PublishApiLatencyAsync(endpoint, method, durationMs)` - Track API latency
- `PublishErrorAsync(module, operation, errorCode)` - Track errors
- `PublishAiServiceInvocationAsync(service, success, duration)` - Track AI service calls
- `PublishStepFunctionExecutionAsync(workflow, status, duration)` - Track workflows
- `PublishQueueDepthAsync(queueName, depth)` - Track queue depth
- `PublishStorageUsageAsync(storageType, bytesUsed)` - Track storage usage

### CloudWatchMetricsPublisher
Implementation of metrics publisher with CloudWatch integration.

**Features:**
- Automatic metric aggregation
- Dimension support for filtering
- Best-effort delivery (doesn't impact app performance)
- Standard metric units

### AlertConfiguration
Configuration for CloudWatch alarms with thresholds:
- Error rate > 5%
- API latency p95 > 10 seconds
- Step Function failure rate > 10%
- Storage usage > 80%
- Rate limit violations > 100/hour

### AlarmDefinitions
Pre-configured CloudWatch alarms for critical metrics.

## Metrics

### API Metrics
- **ApiRequestCount** - Total API requests
  - Dimensions: Endpoint, Method, StatusCode
- **ApiLatency** - API response time
  - Dimensions: Endpoint, Method
  - Statistics: p50, p95, p99

### Error Metrics
- **ErrorCount** - Total errors
  - Dimensions: Module, Operation, ErrorCode

### AI Service Metrics
- **AiServiceInvocation** - AI service calls
  - Dimensions: ServiceName, Success
- **AiServiceDuration** - AI service latency
  - Dimensions: ServiceName, Success

### Workflow Metrics
- **StepFunctionExecution** - Workflow executions
  - Dimensions: WorkflowName, Status
- **StepFunctionDuration** - Workflow duration
  - Dimensions: WorkflowName, Status

### Queue Metrics
- **QueueDepth** - Offline queue depth
  - Dimensions: QueueName

### Storage Metrics
- **StorageUsage** - Storage consumption
  - Dimensions: StorageType

## Alarms

### HighErrorRate
- **Threshold:** Error rate > 5%
- **Period:** 5 minutes
- **Evaluation:** 2 out of 2 data points
- **Action:** SNS notification

### HighApiLatency
- **Threshold:** p95 latency > 10 seconds
- **Period:** 5 minutes
- **Evaluation:** 2 out of 2 data points
- **Action:** SNS notification

### HighStepFunctionFailureRate
- **Threshold:** Failure rate > 10%
- **Period:** 5 minutes
- **Evaluation:** 2 out of 2 data points
- **Action:** SNS notification

### StorageApproachingLimit
- **Threshold:** Usage > 80%
- **Period:** 1 hour
- **Evaluation:** 1 out of 1 data point
- **Action:** SNS notification

### HighRateLimitViolations
- **Threshold:** > 100 violations per hour
- **Period:** 1 hour
- **Evaluation:** 1 out of 1 data point
- **Action:** SNS notification

## Configuration

Add to `appsettings.json`:

```json
{
  "Monitoring": {
    "CloudWatch": {
      "Namespace": "KisanMitraAI"
    },
    "Alerts": {
      "ErrorRateThreshold": 5.0,
      "ApiLatencyP95Threshold": 10000,
      "StepFunctionFailureRateThreshold": 10.0,
      "StorageUsageThreshold": 80.0,
      "RateLimitViolationsThreshold": 100,
      "SnsTopicArn": "arn:aws:sns:region:account:KisanMitraAI-Alerts",
      "EvaluationPeriods": 2,
      "DataPointsToAlarm": 2
    }
  }
}
```

## Usage

### Registration
```csharp
services.AddMonitoring(configuration);
```

### Publishing Metrics

```csharp
public class VoiceQueryController : ControllerBase
{
    private readonly IMetricsPublisher _metrics;

    public VoiceQueryController(IMetricsPublisher metrics)
    {
        _metrics = metrics;
    }

    [HttpPost("query")]
    public async Task<IActionResult> ProcessVoiceQuery()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Process query
            var result = await _voiceQueryHandler.ProcessAsync();
            
            // Publish success metrics
            await _metrics.PublishApiRequestAsync("/api/v1/voicequery/query", "POST", 200);
            await _metrics.PublishApiLatencyAsync("/api/v1/voicequery/query", "POST", stopwatch.ElapsedMilliseconds);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Publish error metrics
            await _metrics.PublishErrorAsync("KrishiVani", "ProcessVoiceQuery", "SERVICE_ERROR");
            await _metrics.PublishApiRequestAsync("/api/v1/voicequery/query", "POST", 500);
            
            throw;
        }
    }
}
```

### AI Service Metrics
```csharp
var stopwatch = Stopwatch.StartNew();
try
{
    var result = await _bedrockClient.InvokeModelAsync(request);
    await _metrics.PublishAiServiceInvocationAsync("Bedrock", true, stopwatch.ElapsedMilliseconds);
}
catch
{
    await _metrics.PublishAiServiceInvocationAsync("Bedrock", false, stopwatch.ElapsedMilliseconds);
    throw;
}
```

### Queue Depth Monitoring
```csharp
var queueDepth = await _offlineQueue.GetDepthAsync();
await _metrics.PublishQueueDepthAsync("OfflineQueue", queueDepth);
```

## CloudWatch Dashboard

Metrics can be visualized in CloudWatch dashboards showing:
- API request rate and latency trends
- Error rate by module and operation
- AI service performance
- Step Function execution status
- Queue depth over time
- Storage usage trends

## SNS Notifications

Critical alarms trigger SNS notifications to:
- Email addresses
- SMS numbers
- Slack/Teams webhooks
- PagerDuty integrations

## Requirements

Validates Requirements 9.5: Monitoring, metrics, and alerting
