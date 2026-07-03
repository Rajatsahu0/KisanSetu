using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.PlantingAdvisory;
using KisanMitraAI.Infrastructure.Repositories.Timestream;
using KisanMitraAI.Infrastructure.Storage.S3;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.PlantingAdvisory;

/// <summary>
/// Adapter that bridges ISoilDataRepository (DynamoDB) to ISoilDataRetriever interface
/// Cost-optimized replacement for Timestream-based SoilDataRetriever
/// </summary>
public class SoilDataRetrieverAdapter : ISoilDataRetriever
{
    private readonly ISoilDataRepository _soilDataRepository;
    private readonly IS3StorageService _s3StorageService;
    private readonly ILogger<SoilDataRetrieverAdapter> _logger;

    public SoilDataRetrieverAdapter(
        ISoilDataRepository soilDataRepository,
        IS3StorageService s3StorageService,
        ILogger<SoilDataRetrieverAdapter> logger)
    {
        _soilDataRepository = soilDataRepository ?? throw new ArgumentNullException(nameof(soilDataRepository));
        _s3StorageService = s3StorageService ?? throw new ArgumentNullException(nameof(s3StorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SoilHealthData?> GetLatestSoilDataAsync(
        string farmerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(farmerId))
            throw new ArgumentException("Farmer ID cannot be null or empty", nameof(farmerId));

        _logger.LogInformation("Retrieving latest soil data for farmer {FarmerId}", farmerId);

        try
        {
            // Get soil history from DynamoDB (last 30 days)
            var soilHistory = await _soilDataRepository.GetSoilHistoryAsync(
                farmerId,
                startDate: DateTimeOffset.UtcNow.AddDays(-30),
                endDate: DateTimeOffset.UtcNow,
                cancellationToken: cancellationToken);

            // Return the most recent entry (already sorted by TestDate descending)
            var latestSoilData = soilHistory.FirstOrDefault();

            if (latestSoilData == null)
            {
                _logger.LogWarning("No soil data found for farmer {FarmerId}", farmerId);
                return null;
            }

            _logger.LogInformation("Retrieved soil data for farmer {FarmerId} from {TestDate}",
                farmerId, latestSoilData.TestDate);

            return latestSoilData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve soil data for farmer {FarmerId}", farmerId);
            throw;
        }
    }

    public async Task<SoilHealthData?> GetSoilDataFromPlanAsync(
        string farmerId,
        string planId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(farmerId))
            throw new ArgumentException("Farmer ID cannot be null or empty", nameof(farmerId));

        if (string.IsNullOrWhiteSpace(planId))
            throw new ArgumentException("Plan ID cannot be null or empty", nameof(planId));

        _logger.LogInformation(
            "Retrieving soil data from saved plan {PlanId} for farmer {FarmerId}",
            planId,
            farmerId);

        try
        {
            // List all plan files for this farmer to find the matching plan
            var prefix = $"misc/{farmerId}/";
            var s3Keys = await _s3StorageService.ListObjectsAsync(prefix, cancellationToken);

            var planKey = s3Keys.FirstOrDefault(k => k.Contains($"{planId}.json"));

            if (planKey == null)
            {
                _logger.LogWarning(
                    "Plan {PlanId} not found in S3 for farmer {FarmerId}",
                    planId,
                    farmerId);
                return null;
            }

            // Download and deserialize the plan
            using var stream = await _s3StorageService.DownloadAsync(planKey, farmerId, cancellationToken);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync(cancellationToken);
            var plan = JsonSerializer.Deserialize<RegenerativePlan>(json);

            if (plan == null)
            {
                _logger.LogWarning(
                    "Failed to deserialize plan {PlanId} for farmer {FarmerId}",
                    planId,
                    farmerId);
                return null;
            }

            // Extract soil data from the plan
            var soilData = plan.SoilData;

            if (soilData == null)
            {
                _logger.LogWarning(
                    "Plan {PlanId} for farmer {FarmerId} does not contain soil data. Falling back to latest soil data.",
                    planId,
                    farmerId);
                
                // Fallback: Try to get latest soil data from DynamoDB
                // This handles backward compatibility with plans created before SoilData was added
                soilData = await GetLatestSoilDataAsync(farmerId, cancellationToken);
                
                if (soilData == null)
                {
                    _logger.LogWarning(
                        "No soil data available for farmer {FarmerId} (neither in plan nor in history)",
                        farmerId);
                    return null;
                }
                
                _logger.LogInformation(
                    "Using latest soil data as fallback for plan {PlanId} for farmer {FarmerId}",
                    planId,
                    farmerId);
            }

            _logger.LogInformation(
                "Successfully retrieved soil data from plan {PlanId} for farmer {FarmerId}",
                planId,
                farmerId);

            return soilData;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving soil data from plan {PlanId} for farmer {FarmerId}",
                planId,
                farmerId);
            throw;
        }
    }
}
