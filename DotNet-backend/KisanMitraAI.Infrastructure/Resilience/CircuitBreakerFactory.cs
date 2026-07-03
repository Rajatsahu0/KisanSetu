using KisanMitraAI.Core.Resilience;
using KisanMitraAI.Core.Logging;
using System.Collections.Concurrent;

namespace KisanMitraAI.Infrastructure.Resilience;

/// <summary>
/// Factory for creating and managing circuit breakers
/// </summary>
public class CircuitBreakerFactory
{
    private readonly ConcurrentDictionary<string, ICircuitBreaker> _circuitBreakers = new();
    private readonly IStructuredLogger _logger;

    public CircuitBreakerFactory(IStructuredLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get or create a circuit breaker for a service
    /// </summary>
    public ICircuitBreaker GetCircuitBreaker(
        string serviceName,
        int failureThreshold = 5,
        TimeSpan? openDuration = null,
        TimeSpan? halfOpenTimeout = null)
    {
        return _circuitBreakers.GetOrAdd(serviceName, name =>
            new CircuitBreaker(
                name,
                failureThreshold,
                openDuration ?? TimeSpan.FromMinutes(1),
                halfOpenTimeout ?? TimeSpan.FromSeconds(30),
                _logger));
    }

    /// <summary>
    /// Get circuit breaker for Mandi Price API
    /// </summary>
    public ICircuitBreaker GetMandiPriceCircuitBreaker()
    {
        return GetCircuitBreaker("MandiPriceAPI", failureThreshold: 5);
    }

    /// <summary>
    /// Get circuit breaker for Weather API
    /// </summary>
    public ICircuitBreaker GetWeatherApiCircuitBreaker()
    {
        return GetCircuitBreaker("WeatherAPI", failureThreshold: 5);
    }

    /// <summary>
    /// Get circuit breaker for Government Integration
    /// </summary>
    public ICircuitBreaker GetGovernmentApiCircuitBreaker()
    {
        return GetCircuitBreaker("GovernmentAPI", failureThreshold: 3, openDuration: TimeSpan.FromMinutes(5));
    }
}
