using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using KisanMitraAI.Core.Authorization;
using KisanMitraAI.Infrastructure.Repositories.DynamoDB;
using KisanMitraAI.Core.Models;

namespace KisanMitraAI.API.Controllers;

/// <summary>
/// Profile controller for managing user profile information
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
//[Authorize]  // Auth handled via GetFarmerId() check inside methods
public class ProfileController : ControllerBase
{
    private readonly IUserProfileRepository _profileRepository;
    private readonly KisanMitraAI.Core.Authentication.ICognitoAuthService _authService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IUserProfileRepository profileRepository,
        KisanMitraAI.Core.Authentication.ICognitoAuthService authService,
        ILogger<ProfileController> logger)
    {
        _profileRepository = profileRepository;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get user profile
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetFarmerId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Profile request for user: {UserId}", userId);

        // Get real phone number from Cognito (access token doesn't have it, only ID token does)
        var phoneNumber = await GetPhoneFromCognitoAsync(cancellationToken);

        try
        {
            var profile = await _profileRepository.GetProfileAsync(userId, cancellationToken);

            if (profile == null)
            {
                var name = User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value;
                return Ok(new UserProfileResponse
                {
                    Name = name ?? "",
                    PhoneNumber = phoneNumber ?? "",
                    Email = "",
                    City = "",
                    State = "",
                    Pincode = ""
                });
            }

            var locationParts = profile.Location.Split(',', StringSplitOptions.TrimEntries);
            var city = locationParts.Length > 0 ? locationParts[0] : "";
            var state = locationParts.Length > 1 ? locationParts[1] : "";
            var pincode = locationParts.Length > 2 ? locationParts[2] : "";

            return Ok(new UserProfileResponse
            {
                Name = profile.Name,
                PhoneNumber = phoneNumber ?? profile.PhoneNumber,
                Email = profile.Email ?? "",
                City = city,
                State = state,
                Pincode = pincode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user: {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve profile" });
        }
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetFarmerId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Profile update request for user: {UserId}", userId);

        // Get real phone number from Cognito
        var phoneNumber = await GetPhoneFromCognitoAsync(cancellationToken);
        if (string.IsNullOrEmpty(phoneNumber))
        {
            // Fallback: check existing profile
            var existing = await _profileRepository.GetProfileAsync(userId, cancellationToken);
            phoneNumber = existing?.PhoneNumber ?? "+910000000000";
        }

        // Validate email if provided
        if (!string.IsNullOrEmpty(request.Email) && 
            !System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return BadRequest(new { error = "Invalid email address" });
        }

        // Validate pincode if provided
        if (!string.IsNullOrEmpty(request.Pincode) && !System.Text.RegularExpressions.Regex.IsMatch(request.Pincode, @"^\d{6}$"))
        {
            return BadRequest(new { error = "Pincode must be 6 digits" });
        }

        try
        {
            // Get existing profile or create new one
            var existingProfile = await _profileRepository.GetProfileAsync(userId, cancellationToken);

            // Build location string from city, state, pincode
            var locationParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.City)) locationParts.Add(request.City);
            if (!string.IsNullOrWhiteSpace(request.State)) locationParts.Add(request.State);
            if (!string.IsNullOrWhiteSpace(request.Pincode)) locationParts.Add(request.Pincode);
            var location = locationParts.Count > 0 ? string.Join(", ", locationParts) : "Unknown";

            // Create or update profile
            var profile = new FarmerProfile(
                farmerId: userId,
                name: request.Name,
                phoneNumber: phoneNumber,
                preferredLanguage: existingProfile?.PreferredLanguage ?? Language.Hindi,
                preferredDialect: existingProfile?.PreferredDialect,
                location: location,
                farms: existingProfile?.Farms ?? Enumerable.Empty<FarmProfile>(),
                registeredAt: existingProfile?.RegisteredAt ?? DateTimeOffset.UtcNow,
                email: request.Email
            );

            // Save to DynamoDB
            await _profileRepository.SaveProfileAsync(profile, cancellationToken);

            _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);

            // Return updated profile
            var response = new UserProfileResponse
            {
                Name = request.Name,
                PhoneNumber = phoneNumber,
                Email = request.Email ?? "",
                City = request.City ?? "",
                State = request.State ?? "",
                Pincode = request.Pincode ?? ""
            };

            // Sync name, email, location to Cognito (fire-and-forget, don't block response)
            _ = SyncToCognitoAsync(request.Name, request.Email, request.State, request.City);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user: {UserId}", userId);
            return StatusCode(500, new { error = "Failed to update profile" });
        }
    }

    private async Task<string?> GetPhoneFromCognitoAsync(CancellationToken cancellationToken)
    {
        // Firebase Auth: phone number is in the JWT claims, not fetched from provider
        try
        {
            var phone = User.FindFirst("phone_number")?.Value;
            return phone;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get phone from claims");
            return null;
        }
    }

    private Task SyncToCognitoAsync(string? name, string? email, string? state, string? city)
    {
        // Firebase Auth: user attributes stored in Firestore profile, not in auth provider
        // Profile is already saved to Firestore by the calling method
        _logger.LogDebug("Profile sync: stored in Firestore (Firebase Auth does not store custom attributes)");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Check if a string looks like a phone number (not a UUID or other identifier)
    /// </summary>
    private static bool LooksLikePhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Remove common phone number formatting characters
        var cleaned = System.Text.RegularExpressions.Regex.Replace(value, @"[\s\-\(\)]", "");

        // Check if it starts with + followed by digits, or is all digits
        // Phone numbers should be 10-15 digits
        if (cleaned.StartsWith("+"))
        {
            var digitsOnly = cleaned.Substring(1);
            return digitsOnly.Length >= 10 && digitsOnly.Length <= 15 && digitsOnly.All(char.IsDigit);
        }

        // Check if it's 10-15 digits
        return cleaned.Length >= 10 && cleaned.Length <= 15 && cleaned.All(char.IsDigit);
    }

    /// <summary>
    /// Normalize phone number to Indian format
    /// </summary>
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return phoneNumber;
        }

        // Remove all spaces, dashes, and parentheses
        phoneNumber = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");

        // If it starts with +91, keep it as is
        if (phoneNumber.StartsWith("+91"))
        {
            return phoneNumber;
        }

        // If it starts with 91 and is 12 digits, add +
        if (phoneNumber.StartsWith("91") && phoneNumber.Length == 12)
        {
            return "+" + phoneNumber;
        }

        // If it's 10 digits starting with 6-9, add +91
        if (phoneNumber.Length == 10 && phoneNumber[0] >= '6' && phoneNumber[0] <= '9')
        {
            return "+91" + phoneNumber;
        }

        // Return as is if we can't normalize
        return phoneNumber;
    }
}

/// <summary>
/// User profile response model
/// </summary>
public class UserProfileResponse
{
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
}

/// <summary>
/// Update profile request model
/// </summary>
public class UpdateProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
}
