# Logging Infrastructure

This module provides structured logging capabilities with AWS CloudWatch Logs integration for the Kisan Mitra AI platform.

## Components

### IStructuredLogger
Interface for structured logging with context information.

**Methods:**
- `LogInformation(message, context)` - Log informational messages
- `LogWarning(message, context, exception?)` - Log warnings
- `LogError(message, context, exception)` - Log errors

### StructuredLogger
Implementation of structured logging with CloudWatch integration.

**Features:**
- Structured JSON log format
- Context-aware logging (FarmerId, RequestId, Module, Operation, Duration, ErrorCode)
- AWS CloudWatch Logs integration
- Fallback to local logging if CloudWatch fails
- Log aggregation and search support

### LogContext
Record containing context information for logs:
- `FarmerId` - Farmer identifier
- `RequestId` - Request correlation ID
- `Module` - Module name (e.g., "KrishiVani", "QualityGrader")
- `Operation` - Operation name (e.g., "ProcessVoiceQuery", "GradeImage")
- `DurationMs` - Operation duration in milliseconds
- `ErrorCode` - Error code if applicable
- `AdditionalProperties` - Additional custom properties

## Configuration

Add to `appsettings.json`:

```json
{
  "Logging": {
    "CloudWatch": {
      "LogGroupName": "KisanMitraAI",
      "LogStreamName": "app-{MachineName}",
      "RetentionDays": 365
    }
  }
}
```

## Usage

### Registration
```csharp
services.AddStructuredLogging(configuration);
```

### Logging Examples

```csharp
// Information logging
_logger.LogInformation("Voice query processed successfully", new LogContext(
    FarmerId: "farmer123",
    RequestId: "req-456",
    Module: "KrishiVani",
    Operation: "ProcessVoiceQuery",
    DurationMs: 2500
));

// Warning logging
_logger.LogWarning("Image quality below threshold", new LogContext(
    FarmerId: "farmer123",
    RequestId: "req-789",
    Module: "QualityGrader",
    Operation: "AnalyzeImage",
    ErrorCode: "IMAGE_TOO_BLURRY"
), exception);

// Error logging
_logger.LogError("Failed to retrieve Mandi prices", new LogContext(
    FarmerId: "farmer123",
    RequestId: "req-101",
    Module: "KrishiVani",
    Operation: "RetrievePrices",
    ErrorCode: "SERVICE_UNAVAILABLE"
), exception);
```

## Log Retention

Logs are retained for 1 year (365 days) in CloudWatch Logs as per platform requirements.

## Requirements

Validates Requirements 9.5: Error logging with detailed context
