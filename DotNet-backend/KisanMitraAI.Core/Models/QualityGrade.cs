namespace KisanMitraAI.Core.Models;

/// <summary>
/// Quality grades aligned with Indian mandi (APMC market) grading system.
/// These are the actual grades used at mandis and reported via data.gov.in API.
/// </summary>
public enum QualityGrade
{
    /// <summary>Premium quality — best produce, highest price at mandi</summary>
    A,
    /// <summary>Good quality — standard good quality</summary>
    B,
    /// <summary>Fair Average Quality — most common grade at mandis, the "default"</summary>
    FAQ,
    /// <summary>Fair/acceptable — lower quality but sellable</summary>
    C,
    /// <summary>Below FAQ — substandard quality</summary>
    NonFAQ,
    /// <summary>Rejected — unfit for market (rot, mold, pest damage)</summary>
    Reject
}

public static class QualityGradeExtensions
{
    /// <summary>
    /// Maps our grade to the data.gov.in API grade filter value.
    /// Used when fetching grade-specific mandi prices.
    /// </summary>
    public static string ToMandiGradeFilter(this QualityGrade grade) => grade switch
    {
        QualityGrade.A => "Grade A",
        QualityGrade.B => "Grade B",
        QualityGrade.FAQ => "FAQ",
        QualityGrade.C => "Grade C",
        QualityGrade.NonFAQ => "Non-FAQ",
        QualityGrade.Reject => "",
        _ => "FAQ"
    };

    /// <summary>
    /// Fallback multiplier applied to FAQ modal price when grade-specific prices unavailable.
    /// Only used when the API doesn't have prices for the specific grade.
    /// </summary>
    public static decimal GetFallbackMultiplier(this QualityGrade grade) => grade switch
    {
        QualityGrade.A => 1.25m,
        QualityGrade.B => 1.10m,
        QualityGrade.FAQ => 1.0m,
        QualityGrade.C => 0.85m,
        QualityGrade.NonFAQ => 0.70m,
        QualityGrade.Reject => 0.0m,
        _ => 1.0m
    };

    /// <summary>
    /// Human-readable label for display to farmers
    /// </summary>
    public static string GetDisplayLabel(this QualityGrade grade) => grade switch
    {
        QualityGrade.A => "Grade A (Premium)",
        QualityGrade.B => "Grade B (Good)",
        QualityGrade.FAQ => "FAQ (Fair Average Quality)",
        QualityGrade.C => "Grade C (Fair)",
        QualityGrade.NonFAQ => "Non-FAQ (Below Average)",
        QualityGrade.Reject => "Rejected",
        _ => "Unknown"
    };
}
