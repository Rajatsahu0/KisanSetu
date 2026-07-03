namespace KisanMitraAI.Core.Models;

/// <summary>
/// Represents a time period for historical data queries
/// </summary>
public record TimePeriod(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string Label)
{
    /// <summary>
    /// Creates a time period for the last N days
    /// </summary>
    public static TimePeriod LastDays(int days)
    {
        var end = DateTimeOffset.UtcNow;
        var start = end.AddDays(-days);
        return new TimePeriod(start, end, $"Last {days} days");
    }

    /// <summary>
    /// Creates a time period for the last N months
    /// </summary>
    public static TimePeriod LastMonths(int months)
    {
        var end = DateTimeOffset.UtcNow;
        var start = end.AddMonths(-months);
        return new TimePeriod(start, end, $"Last {months} months");
    }

    /// <summary>
    /// Creates a time period for the last N years
    /// </summary>
    public static TimePeriod LastYears(int years)
    {
        var end = DateTimeOffset.UtcNow;
        var start = end.AddYears(-years);
        return new TimePeriod(start, end, $"Last {years} years");
    }

    /// <summary>
    /// Creates a time period for a specific season
    /// </summary>
    public static TimePeriod Season(int year, string seasonName, int startMonth, int endMonth)
    {
        var start = new DateTimeOffset(year, startMonth, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(year, endMonth, DateTime.DaysInMonth(year, endMonth), 23, 59, 59, TimeSpan.Zero);
        return new TimePeriod(start, end, $"{seasonName} {year}");
    }

    /// <summary>
    /// Creates a custom time period
    /// </summary>
    public static TimePeriod Custom(DateTimeOffset start, DateTimeOffset end, string label)
    {
        return new TimePeriod(start, end, label);
    }
}
