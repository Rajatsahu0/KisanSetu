using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.VoiceIntelligence.Models;

/// <summary>
/// Response to a voice query containing audio and price data
/// </summary>
public record VoiceQueryResponse(
    string Transcription,
    IEnumerable<MandiPrice> Prices,
    string AudioResponseUrl,
    string ResponseText,
    float Confidence,
    string Dialect,
    DateTimeOffset ProcessedAt);
