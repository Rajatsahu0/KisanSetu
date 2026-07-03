# Error Handling Infrastructure

This module provides standardized error handling and formatting for the Kisan Mitra AI platform.

## Components

### ErrorResponse
Standardized error response format containing:
- `ErrorCode` - Machine-readable error code
- `Message` - Technical error message
- `UserFriendlyMessage` - User-friendly explanation
- `SuggestedActions` - Actionable steps to resolve the error
- `Timestamp` - When the error occurred
- `RequestId` - Request correlation ID

### ErrorCodes
Comprehensive set of error codes for all error categories:

**Image/Vision Errors:**
- `IMAGE_TOO_BLURRY` - Image quality insufficient
- `IMAGE_POOR_LIGHTING` - Lighting issues
- `IMAGE_FORMAT_INVALID` - Unsupported format
- `IMAGE_TOO_LARGE` - File size exceeds limit

**Audio Errors:**
- `AUDIO_FORMAT_INVALID` - Unsupported format
- `AUDIO_TOO_SHORT` - Recording too short
- `AUDIO_TOO_LONG` - Recording too long
- `AUDIO_QUALITY_POOR` - Poor audio quality

**Document Errors:**
- `SOIL_CARD_UNREADABLE` - Cannot extract data
- `DOCUMENT_FORMAT_INVALID` - Unsupported format
- `DOCUMENT_TOO_LARGE` - File size exceeds limit

**Query Errors:**
- `AMBIGUOUS_QUERY` - Needs clarification
- `INVALID_COMMODITY` - Unknown commodity
- `INVALID_LOCATION` - Unknown location

**Rate Limiting:**
- `RATE_LIMIT_EXCEEDED` - Too many requests
- `QUOTA_EXCEEDED` - Daily limit reached

**Service Errors:**
- `SERVICE_UNAVAILABLE` - Service down
- `SERVICE_TIMEOUT` - Request timeout
- `EXTERNAL_SERVICE_ERROR` - External API failure

**Authentication/Authorization:**
- `UNAUTHORIZED` - Not authenticated
- `FORBIDDEN` - No permission
- `TOKEN_EXPIRED` - Session expired

**Data Errors:**
- `DATA_NOT_FOUND` - Resource not found
- `INVALID_INPUT` - Invalid data
- `VALIDATION_FAILED` - Validation error

**Network Errors:**
- `NETWORK_UNAVAILABLE` - No connection
- `OFFLINE_MODE` - Operating offline

**General:**
- `INTERNAL_ERROR` - Server error
- `UNKNOWN_ERROR` - Unexpected error

### ErrorResponseFormatter
Formats technical errors into user-friendly responses.

**Methods:**
- `FormatError(errorCode, requestId, exception?)` - Format error by code
- `FormatException(exception, requestId)` - Format from exception

## Usage

### Format Error by Code
```csharp
var errorResponse = ErrorResponseFormatter.FormatError(
    ErrorCodes.IMAGE_TOO_BLURRY,
    requestId: "req-123"
);

return BadRequest(errorResponse);
```

### Format Exception
```csharp
try
{
    // Operation
}
catch (Exception ex)
{
    var errorResponse = ErrorResponseFormatter.FormatException(ex, requestId);
    return StatusCode(500, errorResponse);
}
```

### Custom Error with Exception
```csharp
var errorResponse = ErrorResponseFormatter.FormatError(
    ErrorCodes.SERVICE_UNAVAILABLE,
    requestId: "req-456",
    exception: ex
);
```

## Error Response Example

```json
{
  "errorCode": "IMAGE_TOO_BLURRY",
  "message": "Image is too blurry to analyze",
  "userFriendlyMessage": "The image you uploaded is not clear enough. Please take a clearer photo.",
  "suggestedActions": [
    "Ensure good lighting",
    "Hold the camera steady",
    "Clean the camera lens",
    "Move closer to the produce"
  ],
  "timestamp": "2026-02-16T10:30:00Z",
  "requestId": "req-123"
}
```

## Requirements

Validates Requirements 9.5: User-friendly error messages with actionable suggestions
