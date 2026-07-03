namespace KisanMitraAI.Core.Models;

/// <summary>
/// Represents trend data for a specific metric over time
/// </summary>
public record TrendData<T>(
    IEnumerable<DataPoint<T>> DataPoints,
    TrendDirection Direction,
    T? MinValue,
    T? MaxValue,
    T? AverageValue,
    IEnumerable<Anomaly<T>> Anomalies)
{
    /// <summary>
    /// Calculates moving average for the data points
    /// </summary>
    public IEnumerable<DataPoint<T>> CalculateMovingAverage(int windowSize)
    {
        var points = DataPoints.ToList();
        var result = new List<DataPoint<T>>();

        for (int i = windowSize - 1; i < points.Count; i++)
        {
            var window = points.Skip(i - windowSize + 1).Take(windowSize);
            var timestamp = points[i].Timestamp;
            
            // Calculate average based on type
            if (typeof(T) == typeof(decimal))
            {
                var avg = window.Average(p => Convert.ToDecimal(p.Value));
                result.Add(new DataPoint<T>((T)(object)avg, timestamp));
            }
            else if (typeof(T) == typeof(float))
            {
                var avg = window.Average(p => Convert.ToSingle(p.Value));
                result.Add(new DataPoint<T>((T)(object)avg, timestamp));
            }
            else if (typeof(T) == typeof(double))
            {
                var avg = window.Average(p => Convert.ToDouble(p.Value));
                result.Add(new DataPoint<T>((T)(object)avg, timestamp));
            }
        }

        return result;
    }
}

/// <summary>
/// Represents a single data point in a time series
/// </summary>
public record DataPoint<T>(
    T Value,
    DateTimeOffset Timestamp);

/// <summary>
/// Represents the direction of a trend
/// </summary>
public enum TrendDirection
{
    Increasing,
    Decreasing,
    Stable,
    Volatile
}

/// <summary>
/// Represents an anomaly in the data
/// </summary>
public record Anomaly<T>(
    DataPoint<T> Point,
    string Reason,
    float Severity);
