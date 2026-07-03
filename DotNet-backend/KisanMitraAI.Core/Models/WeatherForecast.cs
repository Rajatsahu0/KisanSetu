namespace KisanMitraAI.Core.Models;

public record WeatherForecast
{
    public string Location { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public IEnumerable<DailyForecast> DailyForecasts { get; init; }
    public DateTimeOffset FetchedAt { get; init; }
    public int RealForecastDays { get; init; }
    public int HistoricalProjectionDays { get; init; }

    public WeatherForecast(
        string location,
        IEnumerable<DailyForecast> dailyForecasts,
        DateTimeOffset fetchedAt,
        double latitude = 0,
        double longitude = 0,
        int realForecastDays = 0,
        int historicalProjectionDays = 0)
    {
        Location = location ?? throw new ArgumentException("Location required", nameof(location));
        DailyForecasts = dailyForecasts ?? Enumerable.Empty<DailyForecast>();
        FetchedAt = fetchedAt;
        Latitude = latitude;
        Longitude = longitude;
        RealForecastDays = realForecastDays;
        HistoricalProjectionDays = historicalProjectionDays;
    }
}

public record DailyForecast
{
    public DateOnly Date { get; init; }
    public float MinTemperature { get; init; }
    public float MaxTemperature { get; init; }
    public float Rainfall { get; init; }
    public float Humidity { get; init; }
    public float SoilMoisture { get; init; }
    public float Confidence { get; init; }
    public string DataSource { get; init; }

    public DailyForecast(
        DateOnly date,
        float minTemperature,
        float maxTemperature,
        float rainfall,
        float humidity,
        float soilMoisture,
        float confidence = 0.9f,
        string dataSource = "forecast")
    {
        Date = date;
        MinTemperature = minTemperature;
        MaxTemperature = Math.Max(maxTemperature, minTemperature);
        Rainfall = Math.Max(rainfall, 0);
        Humidity = Math.Clamp(humidity, 0, 100);
        SoilMoisture = Math.Clamp(soilMoisture, 0, 100);
        Confidence = Math.Clamp(confidence, 0, 1);
        DataSource = dataSource ?? "forecast";
    }
}
