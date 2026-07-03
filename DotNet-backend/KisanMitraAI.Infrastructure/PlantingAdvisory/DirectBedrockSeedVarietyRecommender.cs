using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.PlantingAdvisory;
using KisanMitraAI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.PlantingAdvisory;

/// <summary>
/// AI-powered seed variety recommender using Vertex AI Gemini.
/// </summary>
public class DirectBedrockSeedVarietyRecommender : ISeedVarietyRecommender
{
    private readonly GeminiService _geminiService;
    private readonly GeminiModelConfig _modelConfig;
    private readonly ILogger<DirectBedrockSeedVarietyRecommender> _logger;

    public DirectBedrockSeedVarietyRecommender(
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        ILogger<DirectBedrockSeedVarietyRecommender> logger)
    {
        _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        _modelConfig = modelConfig ?? throw new ArgumentNullException(nameof(modelConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<SeedRecommendation>> RecommendVarietiesAsync(
        PlantingWindow window, SoilHealthData soilData, string cropType,
        CancellationToken cancellationToken = default, string? language = null)
    {
        if (window == null) throw new ArgumentNullException(nameof(window));
        if (soilData == null) throw new ArgumentNullException(nameof(soilData));
        if (string.IsNullOrWhiteSpace(cropType)) throw new ArgumentException("Crop type required", nameof(cropType));

        _logger.LogInformation("Recommending seed varieties for {CropType} in {Location}", cropType, soilData.Location);

        try
        {
            var prompt = $@"You are an expert agricultural advisor for Indian farming.
Recommend 3-5 seed varieties for {cropType}.

PLANTING: {window.StartDate:yyyy-MM-dd} to {window.EndDate:yyyy-MM-dd}
LOCATION: {soilData.Location}
SOIL: pH={soilData.pH:F1}, N={soilData.Nitrogen:F1} kg/ha, P={soilData.Phosphorus:F1} kg/ha, K={soilData.Potassium:F1} kg/ha, OC={soilData.OrganicCarbon:F2}%

Return ONLY a valid JSON array:
[{{""varietyName"": """", ""seedCompany"": """", ""maturityDays"": 90, ""suitabilityReason"": """", ""yieldPotential"": 4.5, ""keyCharacteristics"": [""""]}}]";

            var response = await _geminiService.GenerateContentAsync(
                _modelConfig.SeedRecommendationModel, prompt, 0.7f, 2048, cancellationToken);

            return ParseRecommendations(response, cropType, soilData.Location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI recommendations for {CropType}", cropType);
            return GenerateFallbackRecommendations(cropType, soilData.Location);
        }
    }

    private IEnumerable<SeedRecommendation> ParseRecommendations(string response, string cropType, string location)
    {
        try
        {
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']') + 1;
            if (jsonStart < 0 || jsonEnd <= jsonStart) return GenerateFallbackRecommendations(cropType, location);

            var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dtos = JsonSerializer.Deserialize<List<SeedDto>>(jsonText, options);
            if (dtos == null || dtos.Count == 0) return GenerateFallbackRecommendations(cropType, location);

            return dtos.Select(d => new SeedRecommendation(
                d.VarietyName ?? "Unknown", d.SeedCompany ?? "Unknown", d.MaturityDays,
                d.SuitabilityReason ?? "Suitable for local conditions", d.YieldPotential,
                d.KeyCharacteristics ?? new List<string>()));
        }
        catch { return GenerateFallbackRecommendations(cropType, location); }
    }

    private static IEnumerable<SeedRecommendation> GenerateFallbackRecommendations(string cropType, string location)
    {
        return new[]
        {
            new SeedRecommendation($"Local {cropType} Variety 1", "ICAR", 120,
                $"Traditional variety for {location}", 4.0f, new List<string> { "Locally adapted", "Disease resistant" }),
            new SeedRecommendation($"Improved {cropType} Variety 2", "IARI", 130,
                $"Improved variety for {location}", 4.5f, new List<string> { "High yield", "Good quality" }),
            new SeedRecommendation($"Hybrid {cropType} Variety 3", "Private", 110,
                $"Hybrid variety for {location}", 5.0f, new List<string> { "Early maturing", "Pest resistant" }),
        };
    }

    private class SeedDto
    {
        public string? VarietyName { get; set; }
        public string? SeedCompany { get; set; }
        public int MaturityDays { get; set; }
        public string? SuitabilityReason { get; set; }
        public float YieldPotential { get; set; }
        public List<string>? KeyCharacteristics { get; set; }
    }
}
