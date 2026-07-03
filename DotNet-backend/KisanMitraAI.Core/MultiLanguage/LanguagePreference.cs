namespace KisanMitraAI.Core.MultiLanguage;

using KisanMitraAI.Core.Models;

/// <summary>
/// Represents a farmer's language and dialect preference
/// </summary>
public record LanguagePreference(
    string FarmerId,
    Language Language,
    Dialect? Dialect,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
