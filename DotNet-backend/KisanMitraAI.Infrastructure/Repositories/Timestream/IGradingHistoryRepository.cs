using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Infrastructure.Repositories.Timestream;

/// <summary>
/// Repository for grading history in Timestream (2-year retention)
/// </summary>
public interface IGradingHistoryRepository
{
    /// <summary>
    /// Stores a grading record
    /// </summary>
    Task StoreGradingAsync(
        GradingRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets grading history for a farmer
    /// </summary>
    Task<IEnumerable<GradingRecord>> GetGradingHistoryAsync(
        string farmerId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        CancellationToken cancellationToken = default);
}
