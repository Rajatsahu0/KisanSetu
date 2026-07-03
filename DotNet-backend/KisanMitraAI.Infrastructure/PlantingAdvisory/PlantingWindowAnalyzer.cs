using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.PlantingAdvisory;
using KisanMitraAI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.PlantingAdvisory;

/// <summary>
/// Analyzes optimal planting windows using Vertex AI Gemini.
/// </summary>
public class PlantingWindowAnalyzer : IPlantingWindowAnalyzer
{
    private readonly GeminiService _geminiService;
    private readonly GeminiModelConfig _modelConfig;
    private readonly ILogger<PlantingWindowAnalyzer> _logger;

    public PlantingWindowAnalyzer(
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        ILogger<PlantingWindowAnalyzer> logger)
    {
        _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        _modelConfig = modelConfig ?? throw new ArgumentNullException(nameof(modelConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<PlantingWindow>> AnalyzePlantingWindowsAsync(
        WeatherForecast forecast, SoilHealthData soilData, string cropType,
        CancellationToken cancellationToken = default, string? language = null)
    {
        if (forecast == null) throw new ArgumentNullException(nameof(forecast));
        if (soilData == null) throw new ArgumentNullException(nameof(soilData));
        if (string.IsNullOrWhiteSpace(cropType)) throw new ArgumentException("Crop type required", nameof(cropType));

        _logger.LogInformation("Analyzing planting windows for {CropType} in {Location}", cropType, forecast.Location);

        try
        {
            var weatherSummary = new StringBuilder();
            weatherSummary.AppendLine("Weather Forecast:");
            foreach (var day in forecast.DailyForecasts.Take(30))
            {
                weatherSummary.AppendLine($"- {day.Date:yyyy-MM-dd}: Temp {day.MinTemperature:F1}-{day.MaxTemperature:F1}°C, Rain {day.Rainfall:F1}mm");
            }

            var prompt = $@"You are an agricultural expert analyzing optimal planting windows for {cropType}.

{weatherSummary}

Soil: pH={soilData.pH}, OC={soilData.OrganicCarbon}%, N={soilData.Nitrogen} kg/ha, Location={soilData.Location}

Identify 2-3 optimal planting windows. Return ONLY a valid JSON array:
[{{""startDate"": ""2026-07-18"", ""endDate"": ""2026-08-17"", ""rationale"": ""..."", ""confidenceScore"": 75, ""riskFactors"": [""...""]}}]";

            var response = await _geminiService.GenerateContentAsync(
                _modelConfig.PlantingWindowModel, prompt, 0.7f, 4000, cancellationToken);

            return ParsePlantingWindows(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze planting windows for {CropType}", cropType);
            throw;
        }
    }

    private static IEnumerable<PlantingWindow> ParsePlantingWindows(string text)
    {
        var jsonStart = text.IndexOf('[');
        var jsonEnd = text.LastIndexOf(']') + 1;
        if (jsonStart < 0 || jsonEnd <= jsonStart)
        {
            // Fallback: return a default window when Gemini doesn't produce JSON
            var now = DateOnly.FromDateTime(DateTime.UtcNow);
            return new[] { new PlantingWindow(
                now.AddDays(7), now.AddDays(37), 
                "Based on current season conditions", 60f, 
                new List<string> { "Weather variability" }) };
        }

        var jsonText = text.Substring(jsonStart, jsonEnd - jsonStart);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dtos = JsonSerializer.Deserialize<List<WindowDto>>(jsonText, options);
        if (dtos == null || dtos.Count == 0)
        {
            var now = DateOnly.FromDateTime(DateTime.UtcNow);
            return new[] { new PlantingWindow(
                now.AddDays(7), now.AddDays(37), 
                "Based on current season conditions", 60f, 
                new List<string> { "Weather variability" }) };
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var idx = 0;
        return dtos.Select(dto =>
        {
            DateOnly start, end;
            if (string.IsNullOrWhiteSpace(dto.StartDate) || string.IsNullOrWhiteSpace(dto.EndDate))
            {
                start = today.AddDays(idx * 45);
                end = start.AddDays(30);
            }
            else
            {
                start = DateOnly.Parse(dto.StartDate);
                end = DateOnly.Parse(dto.EndDate);
            }
            var rationale = string.IsNullOrWhiteSpace(dto.Rationale) ? $"Window {idx + 1}" : dto.Rationale;
            var confidence = dto.ConfidenceScore > 0 && dto.ConfidenceScore <= 100 ? dto.ConfidenceScore : 65f;
            idx++;
            return new PlantingWindow(start, end, rationale, confidence, dto.RiskFactors ?? new List<string>());
        });
    }

    private class WindowDto
    {
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public string Rationale { get; set; } = "";
        public float ConfidenceScore { get; set; }
        public List<string>? RiskFactors { get; set; }
    }
}
