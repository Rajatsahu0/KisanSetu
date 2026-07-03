namespace KisanMitraAI.Core.Models;

/// <summary>
/// Domain model representing a farm profile
/// </summary>
public record FarmProfile
{
    public string FarmId { get; init; }
    public string FarmerId { get; init; }
    public float AreaInAcres { get; init; }
    public string SoilType { get; init; }
    public string IrrigationType { get; init; }
    public IEnumerable<string> CurrentCrops { get; init; }
    public GeoCoordinates Coordinates { get; init; }

    public FarmProfile(
        string farmId,
        string farmerId,
        float areaInAcres,
        string soilType,
        string irrigationType,
        IEnumerable<string> currentCrops,
        GeoCoordinates coordinates)
    {
        // Auto-generate farmId if not provided
        if (string.IsNullOrWhiteSpace(farmId))
        {
            farmId = Guid.NewGuid().ToString();
        }

        if (string.IsNullOrWhiteSpace(farmerId))
        {
            throw new ArgumentException("Farmer ID cannot be null or empty", nameof(farmerId));
        }

        // Allow zero or negative area - will be validated at business logic layer if needed
        FarmId = farmId;
        FarmerId = farmerId;
        AreaInAcres = areaInAcres > 0 ? areaInAcres : 1.0f; // Default to 1 acre if not provided or invalid
        SoilType = soilType ?? string.Empty;
        IrrigationType = irrigationType ?? string.Empty;
        CurrentCrops = currentCrops ?? Enumerable.Empty<string>();
        Coordinates = coordinates ?? new GeoCoordinates(0, 0); // Default coordinates if not provided
    }
}
