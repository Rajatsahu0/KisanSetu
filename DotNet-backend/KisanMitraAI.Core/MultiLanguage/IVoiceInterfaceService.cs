namespace KisanMitraAI.Core.MultiLanguage;

using KisanMitraAI.Core.Models;

/// <summary>
/// Service for managing voice input/output across all features
/// </summary>
public interface IVoiceInterfaceService
{
    /// <summary>
    /// Processes voice input for any feature
    /// </summary>
    /// <param name="audioStream">The audio stream containing voice input</param>
    /// <param name="farmerId">The farmer's unique identifier</param>
    /// <param name="feature">The feature being accessed (e.g., "market_intelligence", "quality_grading")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transcribed text and parsed intent</returns>
    Task<VoiceInputResult> ProcessVoiceInputAsync(
        Stream audioStream,
        string farmerId,
        string feature,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates voice output for any response
    /// </summary>
    /// <param name="text">The text to convert to speech</param>
    /// <param name="farmerId">The farmer's unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The audio stream containing synthesized speech</returns>
    Task<Stream> GenerateVoiceOutputAsync(
        string text,
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a feature supports voice interface
    /// </summary>
    /// <param name="feature">The feature name</param>
    /// <returns>True if voice interface is supported</returns>
    bool IsVoiceSupported(string feature);
}

/// <summary>
/// Result of voice input processing
/// </summary>
public record VoiceInputResult(
    string TranscribedText,
    string Intent,
    Dictionary<string, string> Parameters,
    float ConfidenceScore);
