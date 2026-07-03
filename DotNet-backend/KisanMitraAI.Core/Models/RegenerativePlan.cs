namespace KisanMitraAI.Core.Models;

/// <summary>
/// Domain model representing a regenerative farming plan
/// </summary>
public record RegenerativePlan
{
    public string PlanId { get; init; }
    public string FarmerId { get; init; }
    public SoilHealthData? SoilData { get; init; }
    public IEnumerable<PlanRecommendation> Recommendations { get; init; }
    public IEnumerable<MonthlyAction> MonthlyActions { get; init; }
    public CarbonSequestrationEstimate CarbonEstimate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ValidUntil { get; init; }
    public decimal EstimatedCostSavings { get; init; }

    public RegenerativePlan(
        string planId,
        string farmerId,
        IEnumerable<PlanRecommendation> recommendations,
        IEnumerable<MonthlyAction> monthlyActions,
        CarbonSequestrationEstimate carbonEstimate,
        DateTimeOffset createdAt,
        DateTimeOffset? validUntil = null,
        decimal estimatedCostSavings = 0,
        SoilHealthData? soilData = null)
    {
        if (string.IsNullOrWhiteSpace(planId))
        {
            throw new ArgumentException("Plan ID cannot be null or empty", nameof(planId));
        }

        if (string.IsNullOrWhiteSpace(farmerId))
        {
            throw new ArgumentException("Farmer ID cannot be null or empty", nameof(farmerId));
        }

        var actionsList = monthlyActions?.ToList() ?? new List<MonthlyAction>();
        if (actionsList.Count != 12)
        {
            throw new ArgumentException("Plan must contain exactly 12 monthly actions", 
                nameof(monthlyActions));
        }

        PlanId = planId;
        FarmerId = farmerId;
        SoilData = soilData;
        Recommendations = recommendations ?? Enumerable.Empty<PlanRecommendation>();
        MonthlyActions = actionsList;
        CarbonEstimate = carbonEstimate ?? throw new ArgumentNullException(nameof(carbonEstimate));
        CreatedAt = createdAt;
        ValidUntil = validUntil ?? createdAt.AddYears(1);
        EstimatedCostSavings = estimatedCostSavings;
    }
}

/// <summary>
/// Domain model representing a plan recommendation
/// </summary>
public record PlanRecommendation
{
    public string Title { get; init; }
    public string Description { get; init; }
    public string Category { get; init; }
    public string Priority { get; init; } // "high", "medium", "low"
    public decimal EstimatedCost { get; init; }
    public string ExpectedBenefit { get; init; }
    public IEnumerable<string> ImplementationSteps { get; init; }

    public PlanRecommendation(
        string title,
        string description,
        string category,
        string priority,
        decimal estimatedCost,
        string expectedBenefit,
        IEnumerable<string>? implementationSteps = null)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Priority = priority ?? throw new ArgumentNullException(nameof(priority));
        EstimatedCost = estimatedCost;
        ExpectedBenefit = expectedBenefit ?? throw new ArgumentNullException(nameof(expectedBenefit));
        ImplementationSteps = implementationSteps ?? Enumerable.Empty<string>();
    }
}

/// <summary>
/// Domain model representing monthly action items
/// </summary>
public record MonthlyAction
{
    public int Month { get; init; }
    public string MonthName { get; init; }
    public IEnumerable<string> Practices { get; init; }
    public string Rationale { get; init; }
    public IEnumerable<string> ExpectedOutcomes { get; init; }

    public MonthlyAction(
        int month,
        string monthName,
        IEnumerable<string> practices,
        string rationale,
        IEnumerable<string> expectedOutcomes)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), 
                "Month must be between 1 and 12");
        }

        if (string.IsNullOrWhiteSpace(monthName))
        {
            throw new ArgumentException("Month name cannot be null or empty", nameof(monthName));
        }

        if (string.IsNullOrWhiteSpace(rationale))
        {
            throw new ArgumentException("Rationale cannot be null or empty", nameof(rationale));
        }

        Month = month;
        MonthName = monthName;
        Practices = practices ?? Enumerable.Empty<string>();
        Rationale = rationale;
        ExpectedOutcomes = expectedOutcomes ?? Enumerable.Empty<string>();
    }
}

/// <summary>
/// Domain model representing carbon sequestration estimate
/// </summary>
public record CarbonSequestrationEstimate
{
    public float TotalCarbonTonnesPerYear { get; init; }
    public float MonthlyAverageTonnes { get; init; }
    public IEnumerable<MonthlyCarbon> MonthlyBreakdown { get; init; }

    public CarbonSequestrationEstimate(
        float totalCarbonTonnesPerYear,
        float monthlyAverageTonnes,
        IEnumerable<MonthlyCarbon> monthlyBreakdown)
    {
        if (totalCarbonTonnesPerYear < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCarbonTonnesPerYear), 
                "Total carbon tonnes cannot be negative");
        }

        if (monthlyAverageTonnes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyAverageTonnes), 
                "Monthly average tonnes cannot be negative");
        }

        var breakdownList = monthlyBreakdown?.ToList() ?? new List<MonthlyCarbon>();
        if (breakdownList.Count != 12)
        {
            throw new ArgumentException("Monthly breakdown must contain exactly 12 entries", 
                nameof(monthlyBreakdown));
        }

        TotalCarbonTonnesPerYear = totalCarbonTonnesPerYear;
        MonthlyAverageTonnes = monthlyAverageTonnes;
        MonthlyBreakdown = breakdownList;
    }
}

/// <summary>
/// Domain model representing monthly carbon sequestration
/// </summary>
public record MonthlyCarbon
{
    public int Month { get; init; }
    public float EstimatedTonnes { get; init; }
    public string PrimaryPractice { get; init; }

    public MonthlyCarbon(
        int month,
        float estimatedTonnes,
        string primaryPractice)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), 
                "Month must be between 1 and 12");
        }

        if (estimatedTonnes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedTonnes), 
                "Estimated tonnes cannot be negative");
        }

        if (string.IsNullOrWhiteSpace(primaryPractice))
        {
            throw new ArgumentException("Primary practice cannot be null or empty", 
                nameof(primaryPractice));
        }

        Month = month;
        EstimatedTonnes = estimatedTonnes;
        PrimaryPractice = primaryPractice;
    }
}
