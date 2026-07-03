namespace KisanMitraAI.Core.Models;

/// <summary>
/// Represents a detected pattern in historical data
/// </summary>
public record DataPattern(
    PatternType Type,
    string Description,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    float Confidence,
    IEnumerable<string> SupportingEvidence);

/// <summary>
/// Types of data patterns
/// </summary>
public enum PatternType
{
    Seasonal,
    Cyclical,
    Trending,
    Volatile,
    Stable,
    Anomalous
}

/// <summary>
/// Represents an insight generated from data analysis
/// </summary>
public record Insight(
    string Title,
    string Description,
    InsightSeverity Severity,
    float Confidence,
    IEnumerable<string> SupportingData,
    IEnumerable<ActionSuggestion> Suggestions);

/// <summary>
/// Severity levels for insights
/// </summary>
public enum InsightSeverity
{
    Info,
    Warning,
    Critical,
    Positive
}

/// <summary>
/// Represents a suggested action based on insights
/// </summary>
public record ActionSuggestion(
    string Action,
    string Rationale,
    ActionPriority Priority,
    IEnumerable<string> ExpectedOutcomes);

/// <summary>
/// Priority levels for action suggestions
/// </summary>
public enum ActionPriority
{
    Low,
    Medium,
    High,
    Urgent
}

/// <summary>
/// Represents a comprehensive trend analysis
/// </summary>
public record TrendAnalysis(
    TrendDirection Direction,
    TrendStrength Strength,
    string Summary,
    IEnumerable<TrendFactor> ContributingFactors,
    IEnumerable<string> Predictions);

/// <summary>
/// Strength of a trend
/// </summary>
public enum TrendStrength
{
    Weak,
    Moderate,
    Strong,
    VeryStrong
}

/// <summary>
/// Represents a factor contributing to a trend
/// </summary>
public record TrendFactor(
    string Name,
    float Impact,
    string Description);
