using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.PlantingAdvisory;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.PlantingAdvisory;

/// <summary>
/// Calculates confidence scores for planting recommendations based on forecast reliability
/// </summary>
public class ConfidenceScorer : IConfidenceScorer
{
    private readonly ILogger<ConfidenceScorer> _logger;

    public ConfidenceScorer(ILogger<ConfidenceScorer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<float> CalculateConfidenceAsync(
        PlantingWindow window,
        WeatherForecast forecast,
        CancellationToken cancellationToken = default)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));
        if (forecast == null)
            throw new ArgumentNullException(nameof(forecast));

        _logger.LogInformation("Calculating confidence score for planting window {Start} to {End}", 
            window.StartDate, window.EndDate);

        // Calculate confidence based on multiple factors
        var baseConfidence = 100f;

        // Factor 1: Time horizon - confidence decreases for longer-term predictions
        var daysUntilStart = (window.StartDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).Days;
        var timeHorizonPenalty = CalculateTimeHorizonPenalty(daysUntilStart);

        // Factor 2: Weather variability - high variability reduces confidence
        var variabilityPenalty = CalculateWeatherVariabilityPenalty(forecast, window);

        // Factor 3: Risk factors - each risk factor reduces confidence
        var riskPenalty = window.RiskFactors.Count() * 5f;

        // Factor 4: Forecast data completeness
        var completenessPenalty = CalculateCompletenessPenalty(forecast, window);

        var finalConfidence = baseConfidence 
            - timeHorizonPenalty 
            - variabilityPenalty 
            - riskPenalty 
            - completenessPenalty;

        // Ensure confidence is between 0 and 100
        finalConfidence = Math.Max(0f, Math.Min(100f, finalConfidence));

        _logger.LogInformation("Confidence score calculated: {Score} (Time: -{TimePenalty}, " +
            "Variability: -{VarPenalty}, Risk: -{RiskPenalty}, Completeness: -{CompPenalty})",
            finalConfidence, timeHorizonPenalty, variabilityPenalty, riskPenalty, completenessPenalty);

        return Task.FromResult(finalConfidence);
    }

    private static float CalculateTimeHorizonPenalty(int daysUntilStart)
    {
        // Confidence decreases for predictions further in the future
        // 0-7 days: 0% penalty
        // 8-30 days: 5-15% penalty
        // 31-60 days: 15-25% penalty
        // 61-90 days: 25-35% penalty

        if (daysUntilStart <= 7)
            return 0f;
        else if (daysUntilStart <= 30)
            return 5f + ((daysUntilStart - 7) / 23f) * 10f;
        else if (daysUntilStart <= 60)
            return 15f + ((daysUntilStart - 30) / 30f) * 10f;
        else
            return 25f + ((daysUntilStart - 60) / 30f) * 10f;
    }

    private static float CalculateWeatherVariabilityPenalty(WeatherForecast forecast, PlantingWindow window)
    {
        // High variability in temperature or rainfall reduces confidence
        var relevantDays = forecast.DailyForecasts
            .Where(d => d.Date >= window.StartDate && d.Date <= window.EndDate)
            .ToList();

        if (relevantDays.Count == 0)
            return 20f; // No data for window = high penalty

        // Calculate temperature range variability
        var tempRanges = relevantDays.Select(d => d.MaxTemperature - d.MinTemperature).ToList();
        var avgTempRange = tempRanges.Average();
        var tempVariability = tempRanges.Select(r => Math.Abs(r - avgTempRange)).Average();

        // Calculate rainfall variability
        var rainfalls = relevantDays.Select(d => d.Rainfall).ToList();
        var avgRainfall = rainfalls.Average();
        var rainfallStdDev = rainfalls.Count > 1 
            ? (float)Math.Sqrt(rainfalls.Select(r => Math.Pow(r - avgRainfall, 2)).Average())
            : 0f;

        // High variability = higher penalty (max 15%)
        var tempPenalty = Math.Min(tempVariability * 0.5f, 7.5f);
        var rainfallPenalty = Math.Min(rainfallStdDev * 0.3f, 7.5f);

        return tempPenalty + rainfallPenalty;
    }

    private static float CalculateCompletenessPenalty(WeatherForecast forecast, PlantingWindow window)
    {
        // Penalty if forecast doesn't cover the entire planting window
        var windowDays = (window.EndDate.ToDateTime(TimeOnly.MinValue) - 
            window.StartDate.ToDateTime(TimeOnly.MinValue)).Days;

        var forecastDays = forecast.DailyForecasts
            .Count(d => d.Date >= window.StartDate && d.Date <= window.EndDate);

        if (forecastDays == 0)
            return 30f; // No coverage = high penalty

        var coverageRatio = (float)forecastDays / windowDays;

        if (coverageRatio >= 0.9f)
            return 0f; // Good coverage
        else if (coverageRatio >= 0.7f)
            return 5f; // Moderate coverage
        else if (coverageRatio >= 0.5f)
            return 10f; // Poor coverage
        else
            return 20f; // Very poor coverage
    }
}
