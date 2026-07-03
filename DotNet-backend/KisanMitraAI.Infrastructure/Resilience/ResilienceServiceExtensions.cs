using KisanMitraAI.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace KisanMitraAI.Infrastructure.Resilience;

/// <summary>
/// Extension methods for configuring resilience services
/// </summary>
public static class ResilienceServiceExtensions
{
    public static IServiceCollection AddCircuitBreakers(this IServiceCollection services)
    {
        services.AddSingleton<CircuitBreakerFactory>(sp =>
        {
            var logger = sp.GetRequiredService<IStructuredLogger>();
            return new CircuitBreakerFactory(logger);
        });

        return services;
    }
}
