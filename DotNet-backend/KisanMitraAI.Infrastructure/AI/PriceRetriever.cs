using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Infrastructure.Repositories.Timestream;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// Price retriever — uses LiveMandiPriceService (data.gov.in API + DynamoDB cache).
/// Falls back to direct DynamoDB repository if live service fails.
/// </summary>
public class PriceRetriever : IPriceRetriever
{
    private readonly LiveMandiPriceService _liveService;
    private readonly IMandiPriceRepository _repository;
    private readonly ILogger<PriceRetriever> _logger;

    public PriceRetriever(
        LiveMandiPriceService liveService,
        IMandiPriceRepository repository,
        ILogger<PriceRetriever> logger)
    {
        _liveService = liveService ?? throw new ArgumentNullException(nameof(liveService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<MandiPrice>> GetCurrentPricesAsync(
        string commodity, string location, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(commodity))
            throw new ArgumentException("Commodity cannot be null or empty", nameof(commodity));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location cannot be null or empty", nameof(location));

        try
        {
            var prices = await _liveService.GetCurrentPricesAsync(commodity, location, cancellationToken);
            var list = prices.ToList();
            _logger.LogInformation("PriceRetriever: {Count} prices for {Commodity} in {Location}", list.Count, commodity, location);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LiveMandiPriceService failed, falling back to DynamoDB for {Commodity}", commodity);
            return await _repository.GetCurrentPricesAsync(commodity, location, cancellationToken);
        }
    }

    public async Task<IEnumerable<MandiPrice>> GetHistoricalPricesAsync(
        string commodity, string location,
        DateTimeOffset startDate, DateTimeOffset endDate,
        CancellationToken cancellationToken)
    {
        return await _repository.GetHistoricalPricesAsync(commodity, location, startDate, endDate, cancellationToken);
    }
}
