namespace KisanMitraAI.Infrastructure.Repositories.Timestream;

/// <summary>
/// Base interface for Timestream repository operations
/// </summary>
public interface ITimestreamRepository
{
    /// <summary>
    /// Writes a record to Timestream
    /// </summary>
    Task WriteAsync<T>(T record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries Timestream with the provided query string
    /// </summary>
    Task<IEnumerable<T>> QueryAsync<T>(string query, CancellationToken cancellationToken = default);
}
