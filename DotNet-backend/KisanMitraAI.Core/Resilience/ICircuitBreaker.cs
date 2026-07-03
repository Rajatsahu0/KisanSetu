namespace KisanMitraAI.Core.Resilience;

/// <summary>
/// Circuit breaker for protecting against cascading failures
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Execute an operation with circuit breaker protection
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an operation with circuit breaker protection and fallback
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<Task<T>> fallback,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current circuit state
    /// </summary>
    CircuitState GetState();
}

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitState
{
    Closed,    // Normal operation
    Open,      // Circuit is open, requests fail fast
    HalfOpen   // Testing if service recovered
}
