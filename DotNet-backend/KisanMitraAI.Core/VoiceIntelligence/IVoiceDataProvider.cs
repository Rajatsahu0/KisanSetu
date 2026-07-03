namespace KisanMitraAI.Core.VoiceIntelligence;

/// <summary>
/// Provides grounded (real) data for voice query intents.
/// LLM formats the data into farmer-friendly language — never invents facts.
/// </summary>
public interface IVoiceDataProvider
{
    Task<GroundedData> GetDataForIntentAsync(
        string intent,
        string query,
        string farmerId,
        string? location,
        string? commodity,
        CancellationToken cancellationToken);
}

/// <summary>
/// Real data fetched from APIs/databases, ready for LLM formatting.
/// </summary>
public record GroundedData(
    string Intent,
    string DataJson,
    string DataSource,
    bool HasRealData,
    string? FallbackDisclaimer);
