namespace KisanMitraAI.Core.Logging;

/// <summary>
/// Structured logging service for consistent logging across the platform
/// </summary>
public interface IStructuredLogger
{
    /// <summary>
    /// Log informational message with context
    /// </summary>
    void LogInformation(string message, LogContext context);

    /// <summary>
    /// Log warning message with context
    /// </summary>
    void LogWarning(string message, LogContext context, Exception? exception = null);

    /// <summary>
    /// Log error message with context
    /// </summary>
    void LogError(string message, LogContext context, Exception exception);
}

/// <summary>
/// Context information for structured logging
/// </summary>
public record LogContext(
    string? FarmerId = null,
    string? RequestId = null,
    string? Module = null,
    string? Operation = null,
    long? DurationMs = null,
    string? ErrorCode = null,
    Dictionary<string, object>? AdditionalProperties = null);
