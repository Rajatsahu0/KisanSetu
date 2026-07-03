using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KisanMitraAI.Core.Authorization;

/// <summary>
/// Authorization handler that enforces farmer data isolation.
/// Ensures that farmers can only access their own data by comparing the farmer ID
/// from the JWT token with the farmer ID in the request.
/// </summary>
public class FarmerDataIsolationHandler : AuthorizationHandler<FarmerDataIsolationRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FarmerDataIsolationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FarmerDataIsolationRequirement requirement)
    {
        // Allow admin users to bypass data isolation if configured
        if (requirement.AllowAdminBypass && context.User.IsAdmin())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get the farmer ID from the JWT claims
        var claimsFarmerId = context.User.GetFarmerId();
        if (string.IsNullOrEmpty(claimsFarmerId))
        {
            // No farmer ID in claims - fail authorization
            context.Fail();
            return Task.CompletedTask;
        }

        // Get the farmer ID from the request (route, query, or body)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var requestFarmerId = GetFarmerIdFromRequest(httpContext);
        
        // If no farmer ID in request, allow (will be set from claims by controller)
        // If farmer ID in request, it must match the claims
        if (string.IsNullOrEmpty(requestFarmerId) || requestFarmerId == claimsFarmerId)
        {
            context.Succeed(requirement);
        }
        else
        {
            // Farmer ID mismatch - attempting to access another farmer's data
            context.Fail();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Extracts the farmer ID from the HTTP request.
    /// Checks route values, query parameters, and common header locations.
    /// </summary>
    private static string? GetFarmerIdFromRequest(HttpContext httpContext)
    {
        // Check route values first (e.g., /api/farmers/{farmerId}/...)
        var routeData = httpContext.GetRouteData();
        if (routeData?.Values.TryGetValue("farmerId", out var routeFarmerId) == true)
        {
            return routeFarmerId?.ToString();
        }

        // Check query parameters (e.g., ?farmerId=...)
        if (httpContext.Request.Query.TryGetValue("farmerId", out var queryFarmerId))
        {
            return queryFarmerId.ToString();
        }

        // Check custom header
        if (httpContext.Request.Headers.TryGetValue("X-Farmer-Id", out var headerFarmerId))
        {
            return headerFarmerId.ToString();
        }

        return null;
    }
}
