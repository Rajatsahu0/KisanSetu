namespace KisanMitraAI.Core.Models;

/// <summary>
/// Domain model representing planting recommendations
/// </summary>
public record PlantingRecommendation
{
    public IEnumerable<PlantingWindow> PlantingWindows { get; init; }
    public IEnumerable<SeedRecommendation> SeedRecommendations { get; init; }

    public PlantingRecommendation(
        IEnumerable<PlantingWindow> plantingWindows,
        IEnumerable<SeedRecommendation> seedRecommendations)
    {
        PlantingWindows = plantingWindows ?? Enumerable.Empty<PlantingWindow>();
        SeedRecommendations = seedRecommendations ?? Enumerable.Empty<SeedRecommendation>();
    }
}

/// <summary>
/// Domain model representing a planting window
/// </summary>
public record PlantingWindow
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public string Rationale { get; init; }
    public float ConfidenceScore { get; init; }
    public IEnumerable<string> RiskFactors { get; init; }

    public PlantingWindow(
        DateOnly startDate,
        DateOnly endDate,
        string rationale,
        float confidenceScore,
        IEnumerable<string> riskFactors)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("End date cannot be before start date", nameof(endDate));
        }

        if (string.IsNullOrWhiteSpace(rationale))
        {
            throw new ArgumentException("Rationale cannot be null or empty", nameof(rationale));
        }

        if (confidenceScore < 0 || confidenceScore > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(confidenceScore), 
                "Confidence score must be between 0 and 100");
        }

        StartDate = startDate;
        EndDate = endDate;
        Rationale = rationale;
        ConfidenceScore = confidenceScore;
        RiskFactors = riskFactors ?? Enumerable.Empty<string>();
    }
}

/// <summary>
/// Domain model representing a seed variety recommendation
/// </summary>
public record SeedRecommendation
{
    public string VarietyName { get; init; }
    public string SeedCompany { get; init; }
    public int MaturityDays { get; init; }
    public string SuitabilityReason { get; init; }
    public float YieldPotential { get; init; }
    public IEnumerable<string> KeyCharacteristics { get; init; }

    public SeedRecommendation(
        string varietyName,
        string seedCompany,
        int maturityDays,
        string suitabilityReason,
        float yieldPotential,
        IEnumerable<string> keyCharacteristics)
    {
        if (string.IsNullOrWhiteSpace(varietyName))
        {
            throw new ArgumentException("Variety name cannot be null or empty", nameof(varietyName));
        }

        if (string.IsNullOrWhiteSpace(seedCompany))
        {
            throw new ArgumentException("Seed company cannot be null or empty", nameof(seedCompany));
        }

        if (maturityDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maturityDays), 
                "Maturity days must be greater than zero");
        }

        if (string.IsNullOrWhiteSpace(suitabilityReason))
        {
            throw new ArgumentException("Suitability reason cannot be null or empty", 
                nameof(suitabilityReason));
        }

        if (yieldPotential < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(yieldPotential), 
                "Yield potential cannot be negative");
        }

        VarietyName = varietyName;
        SeedCompany = seedCompany;
        MaturityDays = maturityDays;
        SuitabilityReason = suitabilityReason;
        YieldPotential = yieldPotential;
        KeyCharacteristics = keyCharacteristics ?? Enumerable.Empty<string>();
    }
}
