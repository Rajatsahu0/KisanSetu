using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.QualityGrading;

public interface IGradingRecordStore
{
    Task<string> StoreGradingRecordAsync(
        GradingRecord record,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<GradingRecord>> GetFarmerGradingHistoryAsync(
        string farmerId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    Task<GradingRecord?> GetGradingRecordAsync(
        string recordId,
        CancellationToken cancellationToken = default);
}
