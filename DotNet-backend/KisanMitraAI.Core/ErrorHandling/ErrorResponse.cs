namespace KisanMitraAI.Core.ErrorHandling;

/// <summary>
/// Standardized error response format
/// </summary>
public record ErrorResponse(
    string ErrorCode,
    string Message,
    string UserFriendlyMessage,
    IEnumerable<string> SuggestedActions,
    DateTimeOffset Timestamp,
    string RequestId);

/// <summary>
/// Error codes for all error categories
/// </summary>
public static class ErrorCodes
{
    // Image/Vision errors
    public const string IMAGE_TOO_BLURRY = "IMAGE_TOO_BLURRY";
    public const string IMAGE_POOR_LIGHTING = "IMAGE_POOR_LIGHTING";
    public const string IMAGE_FORMAT_INVALID = "IMAGE_FORMAT_INVALID";
    public const string IMAGE_TOO_LARGE = "IMAGE_TOO_LARGE";

    // Audio errors
    public const string AUDIO_FORMAT_INVALID = "AUDIO_FORMAT_INVALID";
    public const string AUDIO_TOO_SHORT = "AUDIO_TOO_SHORT";
    public const string AUDIO_TOO_LONG = "AUDIO_TOO_LONG";
    public const string AUDIO_QUALITY_POOR = "AUDIO_QUALITY_POOR";

    // Document errors
    public const string SOIL_CARD_UNREADABLE = "SOIL_CARD_UNREADABLE";
    public const string DOCUMENT_FORMAT_INVALID = "DOCUMENT_FORMAT_INVALID";
    public const string DOCUMENT_TOO_LARGE = "DOCUMENT_TOO_LARGE";

    // Query errors
    public const string AMBIGUOUS_QUERY = "AMBIGUOUS_QUERY";
    public const string INVALID_COMMODITY = "INVALID_COMMODITY";
    public const string INVALID_LOCATION = "INVALID_LOCATION";

    // Rate limiting errors
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";
    public const string QUOTA_EXCEEDED = "QUOTA_EXCEEDED";

    // Service errors
    public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
    public const string SERVICE_TIMEOUT = "SERVICE_TIMEOUT";
    public const string EXTERNAL_SERVICE_ERROR = "EXTERNAL_SERVICE_ERROR";

    // Authentication/Authorization errors
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string FORBIDDEN = "FORBIDDEN";
    public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";

    // Data errors
    public const string DATA_NOT_FOUND = "DATA_NOT_FOUND";
    public const string INVALID_INPUT = "INVALID_INPUT";
    public const string VALIDATION_FAILED = "VALIDATION_FAILED";

    // Network errors
    public const string NETWORK_UNAVAILABLE = "NETWORK_UNAVAILABLE";
    public const string OFFLINE_MODE = "OFFLINE_MODE";

    // General errors
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string UNKNOWN_ERROR = "UNKNOWN_ERROR";
}
