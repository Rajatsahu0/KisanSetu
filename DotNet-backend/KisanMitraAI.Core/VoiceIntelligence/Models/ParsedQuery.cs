namespace KisanMitraAI.Core.VoiceIntelligence.Models;

/// <summary>
/// Parsed query containing extracted commodity and location
/// </summary>
public record ParsedQuery(
    string Commodity,
    string Location,
    string Intent,
    bool RequiresClarification,
    string? ClarificationPrompt,
    float Confidence)
{
    /// <summary>
    /// Multiple locations for comparison queries (e.g., "बैंगलोर aur nashik")
    /// </summary>
    public List<string> Locations { get; init; } = new();

    /// <summary>
    /// Multiple commodities for multi-commodity queries (e.g., "आलू aur प्याज")
    /// </summary>
    public List<string> Commodities { get; init; } = new();
}
