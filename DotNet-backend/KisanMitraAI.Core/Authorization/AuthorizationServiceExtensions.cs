using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace KisanMitraAI.Core.Authorization;

/// <summary>
/// Extension methods for configuring authorization services.
/// </summary>
public static class AuthorizationServiceExtensions
{
    /// <summary>
    /// Adds Kisan Mitra AI authorization services and policies to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKisanMitraAuthorization(this IServiceCollection services)
    {
        // Register authorization handlers
        services.AddSingleton<IAuthorizationHandler, RoleRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, FarmerDataIsolationHandler>();

        // Add HttpContextAccessor for data isolation handler
        services.AddHttpContextAccessor();

        // Configure authorization policies
        services.AddAuthorization(options =>
        {
            // RequiresFarmer policy: User must be authenticated and have Farmer role
            options.AddPolicy("RequiresFarmer", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new RoleRequirement("Farmer"));
                policy.Requirements.Add(new FarmerDataIsolationRequirement(allowAdminBypass: true));
            });

            // RequiresAdmin policy: User must be authenticated and have Admin role
            options.AddPolicy("RequiresAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new RoleRequirement("Admin"));
            });

            // FarmerDataIsolation policy: Enforce data isolation without role requirement
            options.AddPolicy("FarmerDataIsolation", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new FarmerDataIsolationRequirement(allowAdminBypass: true));
            });
        });

        return services;
    }
}
