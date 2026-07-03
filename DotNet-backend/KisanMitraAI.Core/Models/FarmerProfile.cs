using System.Text.RegularExpressions;

namespace KisanMitraAI.Core.Models;

/// <summary>
/// Domain model representing a farmer profile
/// </summary>
public record FarmerProfile
{
    private static readonly Regex IndianPhoneRegex = new(@"^(\+91)?[6-9]\d{9}$", RegexOptions.Compiled);

    public string FarmerId { get; init; }
    public string Name { get; init; }
    public string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public Language PreferredLanguage { get; init; }
    public Dialect? PreferredDialect { get; init; }
    public string Location { get; init; }
    public IEnumerable<FarmProfile> Farms { get; init; }
    public DateTimeOffset RegisteredAt { get; init; }

    public FarmerProfile(
        string farmerId,
        string name,
        string phoneNumber,
        Language preferredLanguage,
        Dialect? preferredDialect,
        string location,
        IEnumerable<FarmProfile> farms,
        DateTimeOffset registeredAt,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(farmerId))
        {
            throw new ArgumentException("Farmer ID cannot be null or empty", nameof(farmerId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        }

        if (!IsValidIndianPhoneNumber(phoneNumber))
        {
            throw new ArgumentException(
                "Phone number must be a valid Indian phone number (10 digits starting with 6-9, optionally prefixed with +91)", 
                nameof(phoneNumber));
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Location cannot be null or empty", nameof(location));
        }

        FarmerId = farmerId;
        Name = name;
        PhoneNumber = phoneNumber;
        Email = email;
        PreferredLanguage = preferredLanguage;
        PreferredDialect = preferredDialect;
        Location = location;
        Farms = farms ?? Enumerable.Empty<FarmProfile>();
        RegisteredAt = registeredAt;
    }

    private static bool IsValidIndianPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        return IndianPhoneRegex.IsMatch(phoneNumber);
    }
}
