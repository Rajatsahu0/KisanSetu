using KisanMitraAI.Core.VoiceIntelligence.Models;

namespace KisanMitraAI.Core.VoiceIntelligence;

/// <summary>
/// Service for transcribing audio to text using Amazon Transcribe
/// </summary>
public interface ITranscriptionService
{
    /// <summary>
    /// Transcribes audio stream to text
    /// </summary>
    /// <param name="audioStream">Audio stream to transcribe</param>
    /// <param name="languageCode">Language code for transcription</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcription result with text and confidence</returns>
    Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        string languageCode,
        CancellationToken cancellationToken);
}
