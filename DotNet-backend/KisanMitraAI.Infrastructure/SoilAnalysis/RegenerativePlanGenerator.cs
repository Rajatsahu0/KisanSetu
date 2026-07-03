using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.PlantingAdvisory;
using KisanMitraAI.Core.SoilAnalysis;
using KisanMitraAI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.SoilAnalysis;

public class RegenerativePlanGenerator : IRegenerativePlanGenerator
{
    private readonly GeminiService _geminiService;
    private readonly GeminiModelConfig _modelConfig;
    private readonly IWeatherDataCollector _weatherCollector;
    private readonly ILogger<RegenerativePlanGenerator> _logger;
    private const int TimeoutSeconds = 60;

    public RegenerativePlanGenerator(
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        IWeatherDataCollector weatherCollector,
        ILogger<RegenerativePlanGenerator> logger)
    {
        _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        _modelConfig = modelConfig ?? throw new ArgumentNullException(nameof(modelConfig));
        _weatherCollector = weatherCollector ?? throw new ArgumentNullException(nameof(weatherCollector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RegenerativePlan> GeneratePlanAsync(
        SoilHealthData soilData, FarmProfile farmProfile,
        CancellationToken cancellationToken, string? language = null)
    {
        if (soilData == null) throw new ArgumentNullException(nameof(soilData));
        if (farmProfile == null) throw new ArgumentNullException(nameof(farmProfile));

        _logger.LogInformation("Generating regenerative plan. FarmerId: {FarmerId}", soilData.FarmerId);

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var weatherSummary = await FetchWeatherSummaryAsync(soilData.Location, linkedCts.Token);
            var prompt = BuildPlanGenerationPrompt(soilData, farmProfile, weatherSummary, language);

            var planText = await _geminiService.GenerateContentAsync(
                _modelConfig.PlanGenerationModel, prompt, 0.7f, 8000, linkedCts.Token);

            var plan = ParseGeneratedPlan(planText, soilData.FarmerId, soilData);
            _logger.LogInformation("Regenerative plan generated. PlanId: {PlanId}", plan.PlanId);
            return plan;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Plan generation timed out after {TimeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate regenerative plan. FarmerId: {FarmerId}", soilData.FarmerId);
            throw new InvalidOperationException("Failed to generate regenerative plan", ex);
        }
    }

    private async Task<string> FetchWeatherSummaryAsync(string location, CancellationToken ct)
    {
        try
        {
            var forecast = await _weatherCollector.GetForecastAsync(location, 90, ct);
            var days = forecast.DailyForecasts.ToList();
            if (days.Count == 0) return "Weather data unavailable.";

            var sb = new StringBuilder();
            sb.AppendLine($"Location: {forecast.Location}");
            var byMonth = days.GroupBy(d => d.Date.Month).Take(3);
            foreach (var monthGroup in byMonth)
            {
                var monthDays = monthGroup.ToList();
                var monthName = new DateTime(2024, monthGroup.Key, 1).ToString("MMMM");
                var avgMax = monthDays.Average(d => d.MaxTemperature);
                var avgMin = monthDays.Average(d => d.MinTemperature);
                var totalRain = monthDays.Sum(d => d.Rainfall);
                sb.AppendLine($"- {monthName}: Temp {avgMin:F0}-{avgMax:F0}°C, Rain {totalRain:F0}mm");
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch weather for {Location}", location);
            return "Weather data unavailable.";
        }
    }

    private string BuildPlanGenerationPrompt(SoilHealthData soilData, FarmProfile farmProfile, string weatherSummary, string? language)
    {
        var langInstruction = (language ?? "en").ToLowerInvariant() switch
        {
            "hi" or "hi-in" => "सरल हिंदी में उत्तर दें।",
            _ => "Respond in simple English suitable for Indian farmers."
        };

        return $@"You are an expert Indian agricultural advisor. Generate a 12-month regenerative farming plan.
CURRENT DATE: {DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(5.5)):dddd, dd MMMM yyyy} IST
LANGUAGE: {langInstruction}

FARM: {farmProfile.AreaInAcres} acres, Soil: {farmProfile.SoilType}, Irrigation: {farmProfile.IrrigationType}
SOIL: pH={soilData.pH}, OC={soilData.OrganicCarbon}%, N={soilData.Nitrogen} kg/ha, P={soilData.Phosphorus} kg/ha, K={soilData.Potassium} kg/ha
WEATHER: {weatherSummary}

Generate JSON with recommendations, months (12), and carbonEstimate. Return ONLY valid JSON.
{{
  ""recommendations"": [{{""title"": """", ""description"": """", ""category"": """", ""priority"": """", ""estimatedCost"": 0, ""expectedBenefit"": """", ""implementationSteps"": []}}],
  ""months"": [{{""month"": 1, ""monthName"": """", ""practices"": [], ""rationale"": """", ""expectedOutcomes"": []}}],
  ""carbonEstimate"": {{""totalTonnesPerYear"": 0, ""monthlyAverageTonnes"": 0, ""monthlyBreakdown"": [{{""month"": 1, ""estimatedTonnes"": 0, ""primaryPractice"": """"}}]}}
}}";
    }

    private RegenerativePlan ParseGeneratedPlan(string planText, string farmerId, SoilHealthData soilData)
    {
        try
        {
            var jsonStart = planText.IndexOf('{');
            var jsonEnd = planText.LastIndexOf('}') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = planText.Substring(jsonStart, jsonEnd - jsonStart);
                var planJson = JsonDocument.Parse(jsonText);
                var recommendations = new List<PlanRecommendation>();
                if (planJson.RootElement.TryGetProperty("recommendations", out var recsElement))
                {
                    foreach (var rec in recsElement.EnumerateArray())
                    {
                        var steps = new List<string>();
                        if (rec.TryGetProperty("implementationSteps", out var stepsEl))
                            foreach (var s in stepsEl.EnumerateArray()) steps.Add(s.GetString() ?? "");
                        recommendations.Add(new PlanRecommendation(
                            rec.GetProperty("title").GetString() ?? "",
                            rec.GetProperty("description").GetString() ?? "",
                            rec.GetProperty("category").GetString() ?? "General",
                            rec.GetProperty("priority").GetString() ?? "medium",
                            rec.TryGetProperty("estimatedCost", out var c) ? c.GetDecimal() : 0,
                            rec.GetProperty("expectedBenefit").GetString() ?? "",
                            steps));
                    }
                }
                var monthlyActions = new List<MonthlyAction>();
                if (planJson.RootElement.TryGetProperty("months", out var monthsEl))
                {
                    foreach (var m in monthsEl.EnumerateArray())
                    {
                        var practices = new List<string>();
                        foreach (var p in m.GetProperty("practices").EnumerateArray()) practices.Add(p.GetString() ?? "");
                        var outcomes = new List<string>();
                        foreach (var o in m.GetProperty("expectedOutcomes").EnumerateArray()) outcomes.Add(o.GetString() ?? "");
                        monthlyActions.Add(new MonthlyAction(
                            m.GetProperty("month").GetInt32(),
                            m.GetProperty("monthName").GetString() ?? "",
                            practices, m.GetProperty("rationale").GetString() ?? "", outcomes));
                    }
                }
                var carbonEl = planJson.RootElement.GetProperty("carbonEstimate");
                var monthlyBreakdown = new List<MonthlyCarbon>();
                foreach (var mc in carbonEl.GetProperty("monthlyBreakdown").EnumerateArray())
                    monthlyBreakdown.Add(new MonthlyCarbon(mc.GetProperty("month").GetInt32(),
                        (float)mc.GetProperty("estimatedTonnes").GetDouble(),
                        mc.GetProperty("primaryPractice").GetString() ?? ""));
                var carbonEstimate = new CarbonSequestrationEstimate(
                    (float)carbonEl.GetProperty("totalTonnesPerYear").GetDouble(),
                    (float)carbonEl.GetProperty("monthlyAverageTonnes").GetDouble(), monthlyBreakdown);
                var savings = recommendations.Sum(r => r.EstimatedCost) * 0.3m;
                return new RegenerativePlan(Guid.NewGuid().ToString(), farmerId, recommendations,
                    monthlyActions, carbonEstimate, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1), savings, soilData);
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to parse plan JSON"); }
        return CreateFallbackPlan(farmerId, soilData);
    }

    private static RegenerativePlan CreateFallbackPlan(string farmerId, SoilHealthData soilData)
    {
        var months = Enumerable.Range(1, 12).Select(i => new MonthlyAction(i,
            new DateTime(2024, i, 1).ToString("MMMM"),
            new List<string> { "Composting", "Cover cropping" },
            "Basic regenerative practices", new List<string> { "Improved soil" })).ToList();
        var carbon = new CarbonSequestrationEstimate(3.6f, 0.3f,
            Enumerable.Range(1, 12).Select(m => new MonthlyCarbon(m, 0.3f, "Composting")).ToList());
        var recs = new List<PlanRecommendation> { new("Start Composting", "Begin composting", "Soil Health", "high", 5000, "Improve OC",
            new[] { "Set up bins", "Collect waste" }) };
        return new RegenerativePlan(Guid.NewGuid().ToString(), farmerId, recs, months, carbon,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1), 5000, soilData);
    }
}
