namespace KisanMitraAI.Core.VoiceIntelligence.Models;

/// <summary>
/// Result of audio transcription
/// </summary>
public record TranscriptionResult(
    string TranscribedText,
    float Confidence,
    string LanguageCode,
    DateTimeOffset TranscribedAt);
