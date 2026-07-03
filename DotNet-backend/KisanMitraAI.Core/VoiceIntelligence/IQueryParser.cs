using KisanMitraAI.Core.VoiceIntelligence.Models;

namespace KisanMitraAI.Core.VoiceIntelligence;

/// <summary>
/// Service for parsing voice queries to extract commodity and location
/// </summary>
public interface IQueryParser
{
    /// <summary>
    /// Parses transcribed text to extract structured query information
    /// </summary>
    /// <param name="transcribedText">Text from voice transcription</param>
    /// <param name="context">Additional context for parsing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed query with commodity and location</returns>
    Task<ParsedQuery> ParseQueryAsync(
        string transcribedText,
        string context,
        CancellationToken cancellationToken);
}
