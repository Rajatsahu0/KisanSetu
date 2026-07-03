using KisanMitraAI.Core.VoiceIntelligence.Models;

namespace KisanMitraAI.Core.VoiceIntelligence;

/// <summary>
/// Handles voice query processing for market intelligence
/// </summary>
public interface IVoiceQueryHandler
{
    /// <summary>
    /// Processes a voice query and returns market intelligence response
    /// </summary>
    /// <param name="audioStream">Audio stream containing the voice query</param>
    /// <param name="dialect">Regional dialect of the audio</param>
    /// <param name="farmerId">ID of the farmer making the query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Voice query response with audio and price data</returns>
    Task<VoiceQueryResponse> ProcessVoiceQueryAsync(
        Stream audioStream,
        string dialect,
        string farmerId,
        CancellationToken cancellationToken);
}
