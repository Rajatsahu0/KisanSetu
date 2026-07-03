using System.Security.Claims;

namespace KisanMitraAI.Core.Authorization;

/// <summary>
/// Extension methods for extracting farmer-specific claims from ClaimsPrincipal.
/// </summary>
public static class FarmerIdClaimsPrincipalExtensions
{
    /// <summary>
    /// The claim type used to store the farmer ID in JWT tokens.
    /// </summary>
    public const string FarmerIdClaimType = "farmer_id";

    /// <summary>
    /// Extracts the farmer ID from the ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal containing the user's claims.</param>
    /// <returns>The farmer ID if found; otherwise, null.</returns>
    public static string? GetFarmerId(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return null;
        }

        // Try custom farmer_id claim first
        var farmerIdClaim = principal.FindFirst(FarmerIdClaimType);
        if (farmerIdClaim != null)
        {
            return farmerIdClaim.Value;
        }

        // Fallback to sub claim (subject identifier from Cognito)
        var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier) 
                       ?? principal.FindFirst("sub");
        
        return subClaim?.Value;
    }

    /// <summary>
    /// Extracts the preferred language from Cognito custom:preferred_language claim.
    /// Falls back to "Hindi" if not set.
    /// </summary>
    public static string GetPreferredLanguage(this ClaimsPrincipal principal)
    {
        var lang = principal?.FindFirst("custom:preferred_language")?.Value
                ?? principal?.FindFirst("preferred_language")?.Value;
        return string.IsNullOrEmpty(lang) ? "Hindi" : lang;
    }

    /// <summary>
    /// Checks if the ClaimsPrincipal has a farmer ID claim.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal to check.</param>
    /// <returns>True if the farmer ID claim exists; otherwise, false.</returns>
    public static bool HasFarmerId(this ClaimsPrincipal principal)
    {
        return !string.IsNullOrEmpty(GetFarmerId(principal));
    }

    /// <summary>
    /// Checks if the ClaimsPrincipal has the Farmer role.
    /// For Cognito users without explicit role claims, all authenticated users are considered farmers by default.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal to check.</param>
    /// <returns>True if the user has the Farmer role or is authenticated; otherwise, false.</returns>
    public static bool IsFarmer(this ClaimsPrincipal principal)
    {
        if (principal == null) return false;
        
        // Check for explicit Farmer role
        if (principal.IsInRole("Farmer")) return true;
        
        // For Cognito users, check if they have a valid sub claim (authenticated)
        // and don't have an Admin role (admins are not farmers)
        if (principal.HasFarmerId() && !principal.IsInRole("Admin"))
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Checks if the ClaimsPrincipal has the Admin role.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal to check.</param>
    /// <returns>True if the user has the Admin role; otherwise, false.</returns>
    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal?.IsInRole("Admin") ?? false;
    }
}
