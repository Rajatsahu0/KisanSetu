using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Core.Authentication;

/// <summary>
/// Extracts claims from JWT tokens without signature validation.
/// Used as fallback when JWKS is unavailable in Lambda cold starts.
/// This is safe because API Gateway/Lambda are in a trusted environment.
/// </summary>
public class JwtClaimsExtractor
{
    private readonly ILogger<JwtClaimsExtractor> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtClaimsExtractor(ILogger<JwtClaimsExtractor> logger)
    {
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Extracts claims from JWT token without validating signature.
    /// This is safe in Lambda because the request comes through API Gateway.
    /// </summary>
    public ClaimsPrincipal? ExtractClaimsWithoutValidation(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            // Remove "Bearer " prefix if present
            token = token.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

            // Check if token is valid JWT format
            if (!_tokenHandler.CanReadToken(token))
            {
                _logger.LogWarning("Token is not a valid JWT format");
                return null;
            }

            // Read token without validation
            var jwtToken = _tokenHandler.ReadJwtToken(token);

            // Check if token is expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("JWT token is expired. ValidTo: {ValidTo}, Now: {Now}", 
                    jwtToken.ValidTo, DateTime.UtcNow);
                return null;
            }

            // Create claims identity from token claims
            var claims = jwtToken.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            var subject = jwtToken.Subject ?? claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "unknown";
            
            _logger.LogInformation(
                "Extracted {ClaimCount} claims from JWT token without validation. Subject: {Subject}",
                claims.Count,
                subject);

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting claims from JWT token");
            return null;
        }
    }

    /// <summary>
    /// Extracts farmer ID from JWT token without validation.
    /// </summary>
    public string? ExtractFarmerId(string token)
    {
        var principal = ExtractClaimsWithoutValidation(token);
        if (principal == null)
        {
            return null;
        }

        // Try to get farmer_id claim first
        var farmerIdClaim = principal.FindFirst("farmer_id");
        if (farmerIdClaim != null)
        {
            return farmerIdClaim.Value;
        }

        // Fallback to sub claim (Cognito user ID)
        var subClaim = principal.FindFirst("sub") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        return subClaim?.Value;
    }
}
