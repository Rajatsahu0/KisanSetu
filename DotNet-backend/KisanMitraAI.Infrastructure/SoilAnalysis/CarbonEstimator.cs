using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.SoilAnalysis;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.SoilAnalysis;

public class CarbonEstimator : ICarbonEstimator
{
    private readonly ILogger<CarbonEstimator> _logger;

    // Research-based carbon sequestration rates (tonnes CO2e per hectare per year)
    private static readonly Dictionary<string, float> PracticeRates = new()
    {
        ["composting"] = 0.5f,
        ["cover cropping"] = 0.8f,
        ["cover crops"] = 0.8f,
        ["crop rotation"] = 0.4f,
        ["no-till"] = 0.6f,
        ["no-tillage"] = 0.6f,
        ["reduced tillage"] = 0.4f,
        ["mulching"] = 0.3f,
        ["agroforestry"] = 1.2f,
        ["green manure"] = 0.5f,
        ["biochar"] = 1.0f,
        ["organic amendments"] = 0.4f,
        ["manure application"] = 0.6f,
        ["legume integration"] = 0.5f,
        ["perennial crops"] = 0.9f
    };

    // Confidence intervals (as percentage of base rate)
    private const float ConfidenceLower = 0.7f; // -30%
    private const float ConfidenceUpper = 1.3f; // +30%

    public CarbonEstimator(ILogger<CarbonEstimator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CarbonSequestrationEstimate> EstimateCarbonSequestrationAsync(
        RegenerativePlan plan,
        SoilHealthData currentSoil,
        CancellationToken cancellationToken)
    {
        if (plan == null)
            throw new ArgumentNullException(nameof(plan));
        if (currentSoil == null)
            throw new ArgumentNullException(nameof(currentSoil));

        _logger.LogInformation(
            "Estimating carbon sequestration. PlanId: {PlanId}, FarmerId: {FarmerId}, CurrentOC: {OC}%",
            plan.PlanId, plan.FarmerId, currentSoil.OrganicCarbon);

        var monthlyBreakdown = new List<MonthlyCarbon>();
        float totalAnnualSequestration = 0f;

        foreach (var monthlyAction in plan.MonthlyActions)
        {
            // Calculate carbon sequestration for this month's practices
            float monthlySequestration = 0f;
            string primaryPractice = "General practices";
            float maxPracticeRate = 0f;

            foreach (var practice in monthlyAction.Practices)
            {
                var rate = GetPracticeRate(practice);
                monthlySequestration += rate / 12f; // Divide by 12 for monthly rate

                if (rate > maxPracticeRate)
                {
                    maxPracticeRate = rate;
                    primaryPractice = practice;
                }
            }

            // Apply soil condition multiplier
            var soilMultiplier = CalculateSoilConditionMultiplier(currentSoil);
            monthlySequestration *= soilMultiplier;

            monthlyBreakdown.Add(new MonthlyCarbon(
                monthlyAction.Month,
                monthlySequestration,
                primaryPractice));

            totalAnnualSequestration += monthlySequestration;
        }

        var monthlyAverage = totalAnnualSequestration / 12f;

        _logger.LogInformation(
            "Carbon sequestration estimated. PlanId: {PlanId}, TotalAnnual: {Total:F2} tonnes, MonthlyAvg: {Avg:F2} tonnes",
            plan.PlanId, totalAnnualSequestration, monthlyAverage);

        var estimate = new CarbonSequestrationEstimate(
            totalAnnualSequestration,
            monthlyAverage,
            monthlyBreakdown);

        return await Task.FromResult(estimate);
    }

    private float GetPracticeRate(string practice)
    {
        var practiceLower = practice.ToLowerInvariant();

        foreach (var kvp in PracticeRates)
        {
            if (practiceLower.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }

        // Default rate for unrecognized practices
        return 0.3f;
    }

    private float CalculateSoilConditionMultiplier(SoilHealthData soil)
    {
        float multiplier = 1.0f;

        // Low organic carbon increases sequestration potential
        if (soil.OrganicCarbon < 0.5f)
        {
            multiplier *= 1.3f; // 30% higher potential for degraded soils
        }
        else if (soil.OrganicCarbon < 1.0f)
        {
            multiplier *= 1.15f; // 15% higher potential
        }
        else if (soil.OrganicCarbon > 3.0f)
        {
            multiplier *= 0.8f; // 20% lower potential for already healthy soils
        }

        // pH affects carbon sequestration
        if (soil.pH < 5.5f || soil.pH > 8.0f)
        {
            multiplier *= 0.9f; // Suboptimal pH reduces effectiveness
        }

        // Nutrient availability affects plant growth and carbon input
        var avgNutrient = (soil.Nitrogen + soil.Phosphorus + soil.Potassium) / 3f;
        if (avgNutrient < 100f)
        {
            multiplier *= 0.85f; // Low nutrients reduce carbon sequestration
        }
        else if (avgNutrient > 300f)
        {
            multiplier *= 1.1f; // Good nutrients enhance carbon sequestration
        }

        return multiplier;
    }
}
