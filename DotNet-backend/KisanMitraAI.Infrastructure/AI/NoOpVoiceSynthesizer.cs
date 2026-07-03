using KisanMitraAI.Core.VoiceIntelligence;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// No-op voice synthesizer — TTS is handled client-side via Web Speech API.
/// This implementation satisfies the IVoiceSynthesizer interface without
/// calling any cloud service.
/// </summary>
public class NoOpVoiceSynthesizer : IVoiceSynthesizer
{
    public Task<(Stream AudioStream, string S3Url)> SynthesizeAsync(
        string text, string voiceId, string languageCode, CancellationToken cancellationToken)
    {
        // Return empty stream and empty URL — frontend handles TTS via Web Speech API
        return Task.FromResult<(Stream, string)>((Stream.Null, ""));
    }
}
