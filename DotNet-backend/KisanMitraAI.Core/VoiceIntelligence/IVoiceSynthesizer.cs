namespace KisanMitraAI.Core.VoiceIntelligence;

/// <summary>
/// Service for synthesizing text to speech using Amazon Polly
/// </summary>
public interface IVoiceSynthesizer
{
    /// <summary>
    /// Synthesizes text to speech audio
    /// </summary>
    /// <param name="text">Text to synthesize</param>
    /// <param name="voiceId">Polly voice ID</param>
    /// <param name="languageCode">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing audio stream in MP3 format and S3 URL</returns>
    Task<(Stream AudioStream, string S3Url)> SynthesizeAsync(
        string text,
        string voiceId,
        string languageCode,
        CancellationToken cancellationToken);
}
