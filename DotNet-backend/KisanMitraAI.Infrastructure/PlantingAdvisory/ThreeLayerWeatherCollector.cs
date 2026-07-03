using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.PlantingAdvisory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.PlantingAdvisory;

/// <summary>
/// 3-Layer Weather System:
///   Layer 1: Open-Meteo 16-day forecast (real, free, no API key)
///   Layer 2: NASA POWER 5-year monthly averages (historical, free, no API key)
///   Layer 3: Intelligent merge with confidence degradation
///
/// Replaces OpenWeatherMap (limited, needs API key).
/// </summary>
public class ThreeLayerWeatherCollector : IWeatherDataCollector
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ThreeLayerWeatherCollector> _logger;

    public ThreeLayerWeatherCollector(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<ThreeLayerWeatherCollector> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WeatherForecast> GetForecastAsync(
        string location, int daysAhead, CancellationToken cancellationToken = default)
    {
        daysAhead = Math.Clamp(daysAhead, 1, 90);

        var cacheKey = $"weather3l_{location}_{daysAhead}";
        if (_cache.TryGetValue<WeatherForecast>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogInformation("Weather cache HIT for {Location}", location);
            return cached;
        }

        _logger.LogInformation("3-Layer weather fetch for {Location}, {Days} days", location, daysAhead);

        // Step 1: Geocode location → lat/lon
        var (lat, lon, resolvedName) = await GeocodeLocationAsync(location, cancellationToken);
        _logger.LogInformation("Geocoded {Location} → {Lat}, {Lon} ({Resolved})", location, lat, lon, resolvedName);

        // Step 2: Parallel fetch — Open-Meteo forecast + NASA POWER historical
        var forecastTask = FetchOpenMeteoForecastAsync(lat, lon, cancellationToken);
        var historicalTask = FetchNasaPowerHistoricalAsync(lat, lon, cancellationToken);

        await Task.WhenAll(forecastTask, historicalTask);

        var forecastDays = await forecastTask;
        var monthlyAverages = await historicalTask;

        _logger.LogInformation("Layer 1: {ForecastCount} forecast days, Layer 2: {HistCount} monthly averages",
            forecastDays.Count, monthlyAverages.Count);

        // Step 3: Merge into unified 90-day projection
        var mergedDays = MergeLayers(forecastDays, monthlyAverages, daysAhead);

        var result = new WeatherForecast(
            resolvedName ?? location,
            mergedDays,
            DateTimeOffset.UtcNow,
            lat, lon,
            realForecastDays: forecastDays.Count,
            historicalProjectionDays: Math.Max(0, daysAhead - forecastDays.Count));

        _cache.Set(cacheKey, result, TimeSpan.FromHours(6));
        _logger.LogInformation("3-Layer weather complete: {Total} days ({Real} real + {Hist} historical)",
            mergedDays.Count, forecastDays.Count, Math.Max(0, mergedDays.Count - forecastDays.Count));

        return result;
    }

    // ─── Layer 0: Geocoding (Open-Meteo, free, no key) ───

    private async Task<(double lat, double lon, string? name)> GeocodeLocationAsync(
        string location, CancellationToken ct)
    {
        var geocodeCacheKey = $"geocode_{location.ToLowerInvariant()}";
        if (_cache.TryGetValue<(double, double, string?)>(geocodeCacheKey, out var cachedGeo))
            return cachedGeo;

        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1&language=en&format=json";
            var json = await _httpClient.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var first = results[0];
                var lat = first.GetProperty("latitude").GetDouble();
                var lon = first.GetProperty("longitude").GetDouble();
                var name = first.TryGetProperty("name", out var n) ? n.GetString() : location;

                var result = (lat, lon, name);
                _cache.Set(geocodeCacheKey, result, TimeSpan.FromDays(30));
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geocoding failed for {Location}, using Delhi fallback", location);
        }

        // Fallback: Delhi
        return (28.61, 77.23, location);
    }

    // ─── Layer 1: Open-Meteo 16-day Forecast ───

    private async Task<List<DailyForecast>> FetchOpenMeteoForecastAsync(
        double lat, double lon, CancellationToken ct)
    {
        try
        {
            var url = $"https://api.open-meteo.com/v1/forecast" +
                $"?latitude={lat}&longitude={lon}" +
                $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,relative_humidity_2m_mean" +
                $"&forecast_days=16&timezone=Asia%2FKolkata";

            var json = await _httpClient.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var daily = doc.RootElement.GetProperty("daily");

            var dates = daily.GetProperty("time").EnumerateArray().ToList();
            var maxTemps = daily.GetProperty("temperature_2m_max").EnumerateArray().ToList();
            var minTemps = daily.GetProperty("temperature_2m_min").EnumerateArray().ToList();
            var precip = daily.GetProperty("precipitation_sum").EnumerateArray().ToList();
            var humidity = daily.GetProperty("relative_humidity_2m_mean").EnumerateArray().ToList();

            var forecasts = new List<DailyForecast>();
            for (int i = 0; i < dates.Count; i++)
            {
                var dayIndex = i + 1;
                var confidence = dayIndex <= 3 ? 0.90f :
                                 dayIndex <= 7 ? 0.75f :
                                 dayIndex <= 12 ? 0.60f : 0.45f;

                var rain = GetFloat(precip, i);
                var hum = GetFloat(humidity, i);
                var soilMoisture = EstimateSoilMoisture(hum, rain);

                forecasts.Add(new DailyForecast(
                    DateOnly.Parse(dates[i].GetString()!),
                    GetFloat(minTemps, i),
                    GetFloat(maxTemps, i),
                    rain,
                    hum,
                    soilMoisture,
                    confidence,
                    "forecast"));
            }

            _logger.LogInformation("Open-Meteo: {Count} days fetched", forecasts.Count);
            return forecasts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open-Meteo forecast failed");
            return new List<DailyForecast>();
        }
    }

    // ─── Layer 2: NASA POWER 5-year Monthly Averages ───

    private async Task<Dictionary<int, MonthlyAverage>> FetchNasaPowerHistoricalAsync(
        double lat, double lon, CancellationToken ct)
    {
        var nasaCacheKey = $"nasa_{lat:F1}_{lon:F1}";
        if (_cache.TryGetValue<Dictionary<int, MonthlyAverage>>(nasaCacheKey, out var cachedNasa) && cachedNasa != null)
            return cachedNasa;

        try
        {
            var endYear = DateTime.UtcNow.Year - 1;
            var startYear = endYear - 4;

            var url = $"https://power.larc.nasa.gov/api/temporal/monthly/point" +
                $"?parameters=T2M_MAX,T2M_MIN,PRECTOTCORR,RH2M" +
                $"&community=AG&longitude={lon:F2}&latitude={lat:F2}" +
                $"&start={startYear}&end={endYear}&format=JSON";

            var json = await _httpClient.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var parameters = doc.RootElement.GetProperty("properties").GetProperty("parameter");

            var tMax = parameters.GetProperty("T2M_MAX");
            var tMin = parameters.GetProperty("T2M_MIN");
            var precip = parameters.GetProperty("PRECTOTCORR");
            var rh = parameters.GetProperty("RH2M");

            // Average by month across 5 years
            // NOTE: NASA T2M_MAX/T2M_MIN are monthly EXTREMES (hottest/coldest moment),
            // not daily averages. We need to dampen them for realistic daily projections.
            var monthlyData = new Dictionary<int, List<(float tmax, float tmin, float rain, float hum)>>();
            for (int m = 1; m <= 12; m++)
                monthlyData[m] = new List<(float, float, float, float)>();

            foreach (var prop in tMax.EnumerateObject())
            {
                var key = prop.Name; // "202007" = July 2020
                if (key.Length == 6 && int.TryParse(key[4..], out var month) && month >= 1 && month <= 12)
                {
                    var tmaxVal = GetJsonFloat(tMax, key);
                    var tminVal = GetJsonFloat(tMin, key);
                    var rainVal = GetJsonFloat(precip, key);
                    var humVal = GetJsonFloat(rh, key);

                    if (tmaxVal > -900 && tminVal > -900) // NASA uses -999 for missing
                        monthlyData[month].Add((tmaxVal, tminVal, rainVal, humVal));
                }
            }

            var averages = new Dictionary<int, MonthlyAverage>();
            foreach (var (month, data) in monthlyData)
            {
                if (data.Count == 0) continue;
                // NASA T2M_MAX is the monthly peak extreme, not daily average max.
                // Dampen by ~15% to get realistic daily average max/min.
                // Example: NASA May peak 48.5°C → daily avg max ≈ 41°C (which matches IMD data)
                var avgTMax = data.Average(d => d.tmax) * 0.85f;
                var avgTMin = data.Average(d => d.tmin) * 1.10f; // Min is monthly coldest, raise slightly
                averages[month] = new MonthlyAverage(
                    avgTMax,
                    avgTMin,
                    data.Average(d => d.rain),  // Already mm/day, correct
                    data.Average(d => d.hum));
            }

            _cache.Set(nasaCacheKey, averages, TimeSpan.FromDays(7));
            _logger.LogInformation("NASA POWER: {Count} months of historical data", averages.Count);
            return averages;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NASA POWER fetch failed, using Indian seasonal defaults");
            return GetDefaultSeasonalAverages();
        }
    }

    // ─── Layer 3: Intelligent Merge ───

    private List<DailyForecast> MergeLayers(
        List<DailyForecast> forecastDays,
        Dictionary<int, MonthlyAverage> monthlyAverages,
        int totalDays)
    {
        var result = new List<DailyForecast>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var random = new Random(today.DayNumber);

        for (int day = 0; day < totalDays; day++)
        {
            var date = today.AddDays(day);
            var month = date.Month;

            if (day < forecastDays.Count)
            {
                // Layer 1: Real forecast
                result.Add(forecastDays[day]);
            }
            else if (day < forecastDays.Count + 4 && forecastDays.Count > 0)
            {
                // Blending zone: weighted mix of last forecast day + historical
                var lastForecast = forecastDays[^1];
                var historical = monthlyAverages.GetValueOrDefault(month, GetDefaultMonth(month));
                var forecastWeight = (float)(forecastDays.Count + 4 - day) / 4f;
                var histWeight = 1f - forecastWeight;

                result.Add(new DailyForecast(
                    date,
                    lastForecast.MinTemperature * forecastWeight + (float)historical.TMin * histWeight,
                    lastForecast.MaxTemperature * forecastWeight + (float)historical.TMax * histWeight,
                    lastForecast.Rainfall * forecastWeight + (float)historical.RainPerDay * histWeight,
                    lastForecast.Humidity * forecastWeight + (float)historical.Humidity * histWeight,
                    EstimateSoilMoisture((float)historical.Humidity, (float)historical.RainPerDay),
                    0.40f - (day - forecastDays.Count) * 0.02f,
                    "blended"));
            }
            else
            {
                // Layer 2: Historical seasonal average with daily variance
                var historical = monthlyAverages.GetValueOrDefault(month, GetDefaultMonth(month));
                var variance = (float)(random.NextDouble() * 0.1 - 0.05); // ±5%
                // RainPerDay is mm/day average. Randomize: some days rain, some don't.
                var hasRain = random.NextDouble() < (historical.RainPerDay > 2 ? 0.4 : 0.15);
                var actualRain = hasRain ? (float)historical.RainPerDay * (2f + (float)random.NextDouble() * 3f) : 0f;

                var daysSinceReal = day - forecastDays.Count;
                var confidence = Math.Max(0.20f, 0.40f - daysSinceReal * 0.003f);

                result.Add(new DailyForecast(
                    date,
                    (float)historical.TMin * (1 + variance),
                    (float)historical.TMax * (1 + variance),
                    actualRain,
                    (float)historical.Humidity * (1 + variance * 0.5f),
                    EstimateSoilMoisture((float)historical.Humidity, actualRain),
                    confidence,
                    "historical_average"));
            }
        }

        return result;
    }

    // ─── Helpers ───

    /// <summary>
    /// Estimates soil moisture as a fraction (0.0-1.0) from humidity and rainfall.
    /// Open-Meteo doesn't provide soil moisture for Indian locations.
    /// This is a simplified agronomic model:
    ///   - Base moisture from relative humidity (humid air = less evaporation = more soil moisture)
    ///   - Rainfall adds moisture directly
    ///   - Dry conditions (humidity < 20%, no rain) = very low moisture (~10-15%)
    ///   - Monsoon conditions (humidity > 70%, rain > 5mm) = high moisture (~60-80%)
    /// </summary>
    private static float EstimateSoilMoisture(float humidity, float rainfallMmPerDay)
    {
        // Base: humidity contributes ~30-50% of soil moisture retention
        var humidityContribution = humidity * 0.006f; // 0-60% humidity → 0-0.36
        // Rain: each mm/day adds significant moisture
        var rainContribution = Math.Min(rainfallMmPerDay * 0.04f, 0.40f); // cap at 0.40
        // Combined
        var moisture = humidityContribution + rainContribution;
        return Math.Clamp(moisture, 0.08f, 0.90f);
    }

    private static float GetFloat(List<JsonElement> arr, int index)
    {
        if (index >= arr.Count) return 0;
        var el = arr[index];
        if (el.ValueKind == JsonValueKind.Null) return 0;
        return (float)el.GetDouble();
    }

    private static float GetJsonFloat(JsonElement parent, string key)
    {
        if (parent.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.Number)
            return (float)val.GetDouble();
        return -999;
    }

    private static int DaysInMonth(int month) => DateTime.DaysInMonth(DateTime.UtcNow.Year, month);

    private record MonthlyAverage(double TMax, double TMin, double RainPerDay, double Humidity);

    private static MonthlyAverage GetDefaultMonth(int month)
    {
        // Indian seasonal defaults (North India averages)
        return month switch
        {
            1 => new(20, 7, 0.5, 60),    // Jan - cold, dry
            2 => new(23, 10, 0.6, 55),   // Feb
            3 => new(30, 15, 0.4, 40),   // Mar - warming
            4 => new(36, 21, 0.3, 30),   // Apr - hot
            5 => new(40, 26, 0.5, 30),   // May - peak heat
            6 => new(38, 27, 3.5, 55),   // Jun - pre-monsoon
            7 => new(34, 26, 7.5, 80),   // Jul - monsoon peak
            8 => new(33, 25, 7.0, 82),   // Aug - monsoon
            9 => new(33, 24, 4.5, 75),   // Sep - retreating monsoon
            10 => new(33, 19, 0.8, 55),  // Oct - post-monsoon
            11 => new(28, 12, 0.2, 50),  // Nov - cooling
            12 => new(22, 8, 0.3, 58),   // Dec - cold
            _ => new(30, 20, 2.0, 60)
        };
    }

    private static Dictionary<int, MonthlyAverage> GetDefaultSeasonalAverages()
    {
        var defaults = new Dictionary<int, MonthlyAverage>();
        for (int m = 1; m <= 12; m++)
            defaults[m] = GetDefaultMonth(m);
        return defaults;
    }
}
