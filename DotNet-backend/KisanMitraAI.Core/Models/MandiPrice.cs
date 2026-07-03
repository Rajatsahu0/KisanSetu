namespace KisanMitraAI.Core.Models;

/// <summary>
/// Domain model representing Mandi (market) price information
/// </summary>
public record MandiPrice
{
    public string Commodity { get; init; }
    public string Location { get; init; }
    public string MandiName { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public decimal ModalPrice { get; init; }
    public DateTimeOffset PriceDate { get; init; }
    public string Unit { get; init; }
    public string? State { get; init; }
    public string? District { get; init; }
    public string? Variety { get; init; }
    public string? Grade { get; init; }

    public MandiPrice(
        string commodity,
        string location,
        string mandiName,
        decimal minPrice,
        decimal maxPrice,
        decimal modalPrice,
        DateTimeOffset priceDate,
        string unit,
        string? state = null,
        string? district = null,
        string? variety = null,
        string? grade = null)
    {
        if (string.IsNullOrWhiteSpace(commodity))
            throw new ArgumentException("Commodity cannot be null or empty", nameof(commodity));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location cannot be null or empty", nameof(location));

        if (minPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(minPrice), "Price cannot be negative");

        // Allow maxPrice >= minPrice (some API records have equal min/max)
        if (maxPrice < minPrice)
            maxPrice = minPrice;

        // Clamp modal price between min and max
        if (modalPrice < minPrice) modalPrice = minPrice;
        if (modalPrice > maxPrice) modalPrice = maxPrice;

        Commodity = commodity;
        Location = location;
        MandiName = mandiName ?? string.Empty;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        ModalPrice = modalPrice;
        PriceDate = priceDate;
        Unit = unit ?? "Quintal";
        State = state;
        District = district;
        Variety = variety;
        Grade = grade;
    }
}
