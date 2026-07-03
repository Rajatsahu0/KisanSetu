using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.VoiceIntelligence.Models;

namespace KisanMitraAI.Core.VoiceIntelligence;

/// <summary>
/// Service for generating natural language responses
/// </summary>
public interface IResponseGenerator
{
    /// <summary>
    /// Generates a natural language response for price query results
    /// </summary>
    /// <param name="query">Parsed query</param>
    /// <param name="prices">Mandi prices to include in response</param>
    /// <param name="dialect">Dialect for response generation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Natural language response text</returns>
    Task<string> GenerateResponseAsync(
        ParsedQuery query,
        IEnumerable<MandiPrice> prices,
        string dialect,
        CancellationToken cancellationToken);
}
