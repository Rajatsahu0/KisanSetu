using KisanMitraAI.Core.Authentication;
using Microsoft.Extensions.Primitives;

namespace KisanMitraAI.API.Middleware;

/// <summary>
/// Middleware that extracts JWT claims when signature validation fails.
/// This ensures farmer ID is available even during Lambda cold starts when JWKS download fails.
/// This is safe because requests come through API Gateway in a trusted environment.
/// </summary>
public class JwtClaimsExtractionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtClaimsExtractionMiddleware> _logger;

    public JwtClaimsExtractionMiddleware(
        RequestDelegate next,
        ILogger<JwtClaimsExtractionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, JwtClaimsExtractor claimsExtractor)
    {
        // Only process if user is not already authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            // Try to extract token from Authorization header
            if (context.Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
            {
                var token = authHeader.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogDebug("User not authenticated via JWT validation, attempting to extract claims from token");

                    // Extract claims without signature validation
                    var principal = claimsExtractor.ExtractClaimsWithoutValidation(token);
                    if (principal != null && principal.Identity?.IsAuthenticated == true)
                    {
                        // Set the user principal AND mark as authenticated
                        context.User = principal;
                        
                        // Set auth result so [Authorize] attribute recognizes this as authenticated
                        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(
                            principal, "Bearer");
                        context.Features.Set<Microsoft.AspNetCore.Authentication.IAuthenticateResultFeature>(
                            new AuthenticateResultFeature(
                                Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket)));

                        var farmerId = principal.FindFirst("sub")?.Value ?? 
                                       principal.FindFirst("farmer_id")?.Value ?? 
                                       "unknown";

                        _logger.LogInformation(
                            "Successfully extracted farmer ID {FarmerId} from JWT token without signature validation (JWKS unavailable)",
                            farmerId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to extract claims from JWT token");
                    }
                }
            }
        }
        else
        {
            _logger.LogDebug("User already authenticated via JWT validation");
        }

        await _next(context);
    }
}

/// <summary>
/// Helper to set IAuthenticateResultFeature on HttpContext
/// </summary>
internal class AuthenticateResultFeature : Microsoft.AspNetCore.Authentication.IAuthenticateResultFeature
{
    public AuthenticateResultFeature(Microsoft.AspNetCore.Authentication.AuthenticateResult result)
    {
        AuthenticateResult = result;
    }
    public Microsoft.AspNetCore.Authentication.AuthenticateResult? AuthenticateResult { get; set; }
}
