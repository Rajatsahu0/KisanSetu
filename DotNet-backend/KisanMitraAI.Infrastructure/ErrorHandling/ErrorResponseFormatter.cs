using KisanMitraAI.Core.ErrorHandling;

namespace KisanMitraAI.Infrastructure.ErrorHandling;

/// <summary>
/// Formats technical errors into user-friendly error responses
/// </summary>
public class ErrorResponseFormatter
{
    private static readonly Dictionary<string, ErrorTemplate> ErrorTemplates = new()
    {
        // Image/Vision errors
        [ErrorCodes.IMAGE_TOO_BLURRY] = new ErrorTemplate(
            "Image is too blurry to analyze",
            "The image you uploaded is not clear enough. Please take a clearer photo.",
            new[] { "Ensure good lighting", "Hold the camera steady", "Clean the camera lens", "Move closer to the produce" }
        ),
        [ErrorCodes.IMAGE_POOR_LIGHTING] = new ErrorTemplate(
            "Image has poor lighting",
            "The image is too dark or too bright. Please take a photo with better lighting.",
            new[] { "Take photo in natural daylight", "Avoid direct sunlight", "Use additional lighting if indoors" }
        ),
        [ErrorCodes.IMAGE_FORMAT_INVALID] = new ErrorTemplate(
            "Invalid image format",
            "The image format is not supported. Please use JPEG or PNG format.",
            new[] { "Convert image to JPEG or PNG", "Take a new photo with your camera app" }
        ),
        [ErrorCodes.IMAGE_TOO_LARGE] = new ErrorTemplate(
            "Image file is too large",
            "The image file size exceeds 10 MB. Please upload a smaller image.",
            new[] { "Compress the image", "Take a photo at lower resolution", "Use a different image" }
        ),

        // Audio errors
        [ErrorCodes.AUDIO_FORMAT_INVALID] = new ErrorTemplate(
            "Invalid audio format",
            "The audio format is not supported. Please use MP3, WAV, or OGG format.",
            new[] { "Record audio using the app's voice recorder", "Convert audio to supported format" }
        ),
        [ErrorCodes.AUDIO_TOO_SHORT] = new ErrorTemplate(
            "Audio recording is too short",
            "Your voice message is too short. Please speak for at least 2 seconds.",
            new[] { "Record a longer message", "Speak clearly and at normal pace" }
        ),
        [ErrorCodes.AUDIO_TOO_LONG] = new ErrorTemplate(
            "Audio recording is too long",
            "Your voice message is too long. Please keep it under 60 seconds.",
            new[] { "Record a shorter message", "Break your query into multiple parts" }
        ),
        [ErrorCodes.AUDIO_QUALITY_POOR] = new ErrorTemplate(
            "Audio quality is poor",
            "We couldn't understand your voice clearly. Please record again in a quieter place.",
            new[] { "Find a quiet location", "Speak closer to the microphone", "Reduce background noise" }
        ),

        // Document errors
        [ErrorCodes.SOIL_CARD_UNREADABLE] = new ErrorTemplate(
            "Soil Health Card is unreadable",
            "We couldn't read the information from your Soil Health Card. Please take a clearer photo.",
            new[] { "Ensure the entire card is visible", "Avoid shadows and glare", "Place card on a flat surface", "Take photo from directly above" }
        ),
        [ErrorCodes.DOCUMENT_FORMAT_INVALID] = new ErrorTemplate(
            "Invalid document format",
            "The document format is not supported. Please use JPEG, PNG, or PDF format.",
            new[] { "Convert document to supported format", "Take a photo of the document" }
        ),
        [ErrorCodes.DOCUMENT_TOO_LARGE] = new ErrorTemplate(
            "Document file is too large",
            "The document file size exceeds 10 MB. Please upload a smaller file.",
            new[] { "Compress the document", "Take a photo at lower resolution" }
        ),

        // Query errors
        [ErrorCodes.AMBIGUOUS_QUERY] = new ErrorTemplate(
            "Query is ambiguous",
            "We need more information to answer your question. Please be more specific.",
            new[] { "Specify the crop variety", "Mention your location", "Provide more details" }
        ),
        [ErrorCodes.INVALID_COMMODITY] = new ErrorTemplate(
            "Invalid commodity",
            "We don't have information about this commodity. Please check the name and try again.",
            new[] { "Check the spelling", "Use common crop names", "Try a different commodity" }
        ),
        [ErrorCodes.INVALID_LOCATION] = new ErrorTemplate(
            "Invalid location",
            "We couldn't find this location. Please provide a valid city or district name.",
            new[] { "Use your district name", "Try nearby city name", "Check the spelling" }
        ),

        // Rate limiting errors
        [ErrorCodes.RATE_LIMIT_EXCEEDED] = new ErrorTemplate(
            "Rate limit exceeded",
            "You've made too many requests. Please wait a moment and try again.",
            new[] { "Wait for 1 minute", "Reduce request frequency" }
        ),
        [ErrorCodes.QUOTA_EXCEEDED] = new ErrorTemplate(
            "Quota exceeded",
            "You've reached your daily limit. Please try again tomorrow.",
            new[] { "Wait until tomorrow", "Contact support for increased quota" }
        ),

        // Service errors
        [ErrorCodes.SERVICE_UNAVAILABLE] = new ErrorTemplate(
            "Service temporarily unavailable",
            "The service is temporarily unavailable. Please try again in a few minutes.",
            new[] { "Wait a few minutes", "Check your internet connection", "Try again later" }
        ),
        [ErrorCodes.SERVICE_TIMEOUT] = new ErrorTemplate(
            "Request timed out",
            "The request took too long to complete. Please try again.",
            new[] { "Check your internet connection", "Try again with a smaller file", "Try again later" }
        ),
        [ErrorCodes.EXTERNAL_SERVICE_ERROR] = new ErrorTemplate(
            "External service error",
            "We're having trouble connecting to external services. Please try again later.",
            new[] { "Wait a few minutes", "Try again later", "Contact support if problem persists" }
        ),

        // Authentication/Authorization errors
        [ErrorCodes.UNAUTHORIZED] = new ErrorTemplate(
            "Unauthorized access",
            "You need to log in to access this feature.",
            new[] { "Log in to your account", "Register if you don't have an account" }
        ),
        [ErrorCodes.FORBIDDEN] = new ErrorTemplate(
            "Access forbidden",
            "You don't have permission to access this resource.",
            new[] { "Check if you're logged in with the correct account", "Contact support if you need access" }
        ),
        [ErrorCodes.TOKEN_EXPIRED] = new ErrorTemplate(
            "Session expired",
            "Your session has expired. Please log in again.",
            new[] { "Log in again", "Enable 'Remember me' for longer sessions" }
        ),

        // Data errors
        [ErrorCodes.DATA_NOT_FOUND] = new ErrorTemplate(
            "Data not found",
            "We couldn't find the requested information.",
            new[] { "Check if the data exists", "Try a different search", "Contact support" }
        ),
        [ErrorCodes.INVALID_INPUT] = new ErrorTemplate(
            "Invalid input",
            "The information you provided is not valid. Please check and try again.",
            new[] { "Review your input", "Follow the required format", "Check for typos" }
        ),
        [ErrorCodes.VALIDATION_FAILED] = new ErrorTemplate(
            "Validation failed",
            "Some information is missing or incorrect. Please review and try again.",
            new[] { "Fill in all required fields", "Check for errors", "Follow the format guidelines" }
        ),

        // Network errors
        [ErrorCodes.NETWORK_UNAVAILABLE] = new ErrorTemplate(
            "Network unavailable",
            "No internet connection. Your request will be processed when you're back online.",
            new[] { "Check your internet connection", "Your request is saved and will be processed automatically" }
        ),
        [ErrorCodes.OFFLINE_MODE] = new ErrorTemplate(
            "Offline mode",
            "You're currently offline. Some features may not be available.",
            new[] { "Connect to internet for full features", "Cached data is available for viewing" }
        ),

        // General errors
        [ErrorCodes.INTERNAL_ERROR] = new ErrorTemplate(
            "Internal server error",
            "Something went wrong on our end. We're working to fix it.",
            new[] { "Try again in a few minutes", "Contact support if problem persists" }
        ),
        [ErrorCodes.UNKNOWN_ERROR] = new ErrorTemplate(
            "Unknown error",
            "An unexpected error occurred. Please try again.",
            new[] { "Try again", "Contact support if problem persists" }
        )
    };

    public static ErrorResponse FormatError(
        string errorCode,
        string requestId,
        Exception? exception = null)
    {
        if (!ErrorTemplates.TryGetValue(errorCode, out var template))
        {
            template = ErrorTemplates[ErrorCodes.UNKNOWN_ERROR];
            errorCode = ErrorCodes.UNKNOWN_ERROR;
        }

        var message = exception?.Message ?? template.Message;

        return new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            UserFriendlyMessage: template.UserFriendlyMessage,
            SuggestedActions: template.SuggestedActions,
            Timestamp: DateTimeOffset.UtcNow,
            RequestId: requestId
        );
    }

    public static ErrorResponse FormatException(
        Exception exception,
        string requestId)
    {
        var errorCode = MapExceptionToErrorCode(exception);
        return FormatError(errorCode, requestId, exception);
    }

    private static string MapExceptionToErrorCode(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => ErrorCodes.UNAUTHORIZED,
            TimeoutException => ErrorCodes.SERVICE_TIMEOUT,
            ArgumentException => ErrorCodes.INVALID_INPUT,
            InvalidOperationException => ErrorCodes.VALIDATION_FAILED,
            _ => ErrorCodes.INTERNAL_ERROR
        };
    }

    private record ErrorTemplate(
        string Message,
        string UserFriendlyMessage,
        string[] SuggestedActions);
}
