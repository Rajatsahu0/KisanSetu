using KisanMitraAI.Core.PlantingAdvisory;
using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Infrastructure.Repositories.DynamoDB;
using KisanMitraAI.Infrastructure.Repositories.Timestream;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// Routes voice intents to real data sources. LLM formats — never invents.
/// 
/// Intent → Data Source mapping:
///   weather_query  → OpenWeatherMap API (via WeatherDataCollector)
///   soil_query     → DynamoDB soil history
///   price_query    → data.gov.in API + DynamoDB cache
///   profile_query  → DynamoDB user profile
///   general        → No grounded data (LLM with disclaimer)
/// </summary>
public class GroundedVoiceDataProvider : IVoiceDataProvider
{
    private readonly IWeatherDataCollector _weatherCollector;
    private readonly ISoilDataRepository _soilRepo;
    private readonly IUserProfileRepository _profileRepo;
    private readonly ILogger<GroundedVoiceDataProvider> _logger;

    public GroundedVoiceDataProvider(
        IWeatherDataCollector weatherCollector,
        ISoilDataRepository soilRepo,
        IUserProfileRepository profileRepo,
        ILogger<GroundedVoiceDataProvider> logger)
    {
        _weatherCollector = weatherCollector;
        _soilRepo = soilRepo;
        _profileRepo = profileRepo;
        _logger = logger;
    }

    public async Task<GroundedData> GetDataForIntentAsync(
        string intent, string query, string farmerId,
        string? location, string? commodity, CancellationToken cancellationToken)
    {
        try
        {
            return intent switch
            {
                "weather_query" => await GetWeatherDataAsync(farmerId, location, query, cancellationToken),
                "soil_query" => await GetSoilDataAsync(farmerId, cancellationToken),
                "profile_query" => await GetProfileDataAsync(farmerId, cancellationToken),
                _ => new GroundedData(intent, "{}", "none", false,
                    "यह जानकारी AI द्वारा उत्पन्न है और वास्तविक डेटा पर आधारित नहीं हो सकती। कृपया स्थानीय कृषि विभाग से सत्यापित करें। (This information is AI-generated and may not be based on real data. Please verify with your local agriculture department.)")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch grounded data for intent {Intent}, farmerId {FarmerId}", intent, farmerId);
            return new GroundedData(intent, "{}", "error", false,
                "डेटा प्राप्त करने में समस्या हुई। कृपया बाद में पुनः प्रयास करें। (There was a problem fetching data. Please try again later.)");
        }
    }

    private async Task<GroundedData> GetWeatherDataAsync(
        string farmerId, string? location, string query, CancellationToken ct)
    {
        var resolvedLocation = location;

        if (string.IsNullOrWhiteSpace(resolvedLocation))
        {
            resolvedLocation = await GetFarmerLocationAsync(farmerId, ct);
        }

        if (string.IsNullOrWhiteSpace(resolvedLocation))
        {
            return new GroundedData("weather_query", "{}", "none", false,
                "आपका स्थान नहीं मिला। कृपया अपना शहर या जिला बताएं। (Your location was not found. Please specify your city or district.)");
        }

        var daysAhead = ExtractDaysFromQuery(query);

        _logger.LogInformation("Fetching real weather for {Location}, {Days} days", resolvedLocation, daysAhead);

        var forecast = await _weatherCollector.GetForecastAsync(resolvedLocation, daysAhead, ct);

        var weatherJson = JsonSerializer.Serialize(new
        {
            location = resolvedLocation,
            fetchedAt = forecast.FetchedAt.ToString("o"),
            forecasts = forecast.DailyForecasts.Select(f => new
            {
                date = f.Date.ToString("yyyy-MM-dd"),
                minTemp = f.MinTemperature,
                maxTemp = f.MaxTemperature,
                rainfall = f.Rainfall,
                humidity = f.Humidity,
                dataSource = f.DataSource
            })
        });

        return new GroundedData("weather_query", weatherJson, $"OpenWeatherMap API for {resolvedLocation}", true, null);
    }

    private async Task<GroundedData> GetSoilDataAsync(string farmerId, CancellationToken ct)
    {
        var history = await _soilRepo.GetSoilHistoryAsync(
            farmerId, DateTimeOffset.UtcNow.AddYears(-1), DateTimeOffset.UtcNow, ct);

        var latest = history.OrderByDescending(s => s.TestDate).FirstOrDefault();

        if (latest == null)
        {
            return new GroundedData("soil_query", "{}", "none", false,
                "आपके खेत का मृदा डेटा नहीं मिला। कृपया पहले अपना मृदा स्वास्थ्य कार्ड अपलोड करें। (No soil data found. Please upload your Soil Health Card first.)");
        }

        var soilJson = JsonSerializer.Serialize(new
        {
            farmerId = latest.FarmerId,
            testDate = latest.TestDate.ToString("yyyy-MM-dd"),
            pH = latest.pH,
            nitrogen = latest.Nitrogen,
            phosphorus = latest.Phosphorus,
            potassium = latest.Potassium,
            organicCarbon = latest.OrganicCarbon,
            sulfur = latest.Sulfur,
            zinc = latest.Zinc,
            iron = latest.Iron,
            location = latest.Location
        });

        return new GroundedData("soil_query", soilJson, "DynamoDB soil history", true, null);
    }

    private async Task<GroundedData> GetProfileDataAsync(string farmerId, CancellationToken ct)
    {
        var profile = await _profileRepo.GetProfileAsync(farmerId, ct);

        if (profile == null)
        {
            return new GroundedData("profile_query", "{}", "none", false,
                "आपकी प्रोफ़ाइल नहीं मिली। (Your profile was not found.)");
        }

        var profileJson = JsonSerializer.Serialize(new
        {
            name = profile.Name,
            location = profile.Location,
            farms = profile.Farms.Select(f => new
            {
                areaInAcres = f.AreaInAcres,
                soilType = f.SoilType,
                currentCrops = f.CurrentCrops
            })
        });

        return new GroundedData("profile_query", profileJson, "DynamoDB user profile", true, null);
    }

    private async Task<string?> GetFarmerLocationAsync(string farmerId, CancellationToken ct)
    {
        try
        {
            var profile = await _profileRepo.GetProfileAsync(farmerId, ct);
            if (profile != null && !string.IsNullOrWhiteSpace(profile.Location))
            {
                _logger.LogInformation("Resolved location from farmer profile: {Location}", profile.Location);
                return profile.Location;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch farmer profile for location resolution");
        }
        return null;
    }

    private static int ExtractDaysFromQuery(string query)
    {
        var numberWords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["1"] = 1, ["2"] = 2, ["3"] = 3, ["4"] = 4, ["5"] = 5,
            ["6"] = 6, ["7"] = 7, ["8"] = 8, ["9"] = 9, ["10"] = 10,
            ["15"] = 15, ["एक"] = 1, ["दो"] = 2, ["तीन"] = 3,
            ["चार"] = 4, ["पांच"] = 5, ["पाँच"] = 5, ["छह"] = 6,
            ["सात"] = 7, ["आठ"] = 8, ["नौ"] = 9, ["दस"] = 10,
            ["पंद्रह"] = 15, ["हफ्ता"] = 7, ["हफ्ते"] = 7, ["week"] = 7,
        };

        foreach (var kvp in numberWords)
        {
            if (query.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return Math.Min(kvp.Value, 16); // OpenWeatherMap free tier max
        }

        return 5; // default
    }
}
