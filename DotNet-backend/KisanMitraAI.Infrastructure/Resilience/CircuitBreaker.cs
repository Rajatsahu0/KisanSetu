using KisanMitraAI.Core.Resilience;
using KisanMitraAI.Core.Logging;
using System.Collections.Concurrent;

namespace KisanMitraAI.Infrastructure.Resilience;

/// <summary>
/// Circuit breaker implementation for external service protection
/// </summary>
public class CircuitBreaker : ICircuitBreaker
{
    private readonly string _serviceName;
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly TimeSpan _halfOpenTimeout;
    private readonly IStructuredLogger _logger;

    private CircuitState _state = CircuitState.Closed;
    private int _failureCount = 0;
    private DateTimeOffset _lastFailureTime = DateTimeOffset.MinValue;
    private DateTimeOffset _circuitOpenedTime = DateTimeOffset.MinValue;
    private readonly object _lock = new object();

    public CircuitBreaker(
        string serviceName,
        int failureThreshold = 5,
        TimeSpan? openDuration = null,
        TimeSpan? halfOpenTimeout = null,
        IStructuredLogger? logger = null)
    {
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromMinutes(1);
        _halfOpenTimeout = halfOpenTimeout ?? TimeSpan.FromSeconds(30);
        _logger = logger ?? new NullStructuredLogger();
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(operation, null, cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<Task<T>>? fallback,
        CancellationToken cancellationToken = default)
    {
        // Check circuit state
        bool shouldUseFallback = false;
        lock (_lock)
        {
            if (_state == CircuitState.Open)
            {
                // Check if we should transition to half-open
                if (DateTimeOffset.UtcNow - _circuitOpenedTime >= _openDuration)
                {
                    _state = CircuitState.HalfOpen;
                    _logger.LogInformation($"Circuit breaker for {_serviceName} transitioning to HalfOpen", 
                        new LogContext(Module: "CircuitBreaker", Operation: "StateTransition"));
                }
                else
                {
                    // Circuit is still open, fail fast
                    _logger.LogWarning($"Circuit breaker for {_serviceName} is Open, failing fast",
                        new LogContext(Module: "CircuitBreaker", Operation: "FailFast"));
                    shouldUseFallback = true;
                }
            }
        }

        // Handle fallback outside of lock
        if (shouldUseFallback)
        {
            if (fallback != null)
            {
                return await fallback();
            }
            throw new CircuitBreakerOpenException($"Circuit breaker for {_serviceName} is open");
        }

        try
        {
            // Execute operation with timeout in half-open state
            Task<T> operationTask = operation(cancellationToken);
            
            if (_state == CircuitState.HalfOpen)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_halfOpenTimeout);
                operationTask = operation(cts.Token);
            }

            var result = await operationTask;

            // Success - reset failure count or close circuit
            lock (_lock)
            {
                if (_state == CircuitState.HalfOpen)
                {
                    _state = CircuitState.Closed;
                    _logger.LogInformation($"Circuit breaker for {_serviceName} closed after successful test",
                        new LogContext(Module: "CircuitBreaker", Operation: "StateTransition"));
                }
                _failureCount = 0;
            }

            return result;
        }
        catch (Exception ex)
        {
            // Record failure
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTimeOffset.UtcNow;

                if (_state == CircuitState.HalfOpen)
                {
                    // Failed in half-open state, reopen circuit
                    _state = CircuitState.Open;
                    _circuitOpenedTime = DateTimeOffset.UtcNow;
                    _logger.LogWarning($"Circuit breaker for {_serviceName} reopened after failed test",
                        new LogContext(Module: "CircuitBreaker", Operation: "StateTransition"), ex);
                }
                else if (_failureCount >= _failureThreshold)
                {
                    // Threshold exceeded, open circuit
                    _state = CircuitState.Open;
                    _circuitOpenedTime = DateTimeOffset.UtcNow;
                    _logger.LogError($"Circuit breaker for {_serviceName} opened after {_failureCount} failures",
                        new LogContext(Module: "CircuitBreaker", Operation: "StateTransition"), ex);
                }
            }

            // Use fallback if available
            if (fallback != null)
            {
                _logger.LogInformation($"Using fallback for {_serviceName}",
                    new LogContext(Module: "CircuitBreaker", Operation: "Fallback"));
                return await fallback();
            }

            throw;
        }
    }

    public CircuitState GetState()
    {
        lock (_lock)
        {
            return _state;
        }
    }
}

/// <summary>
/// Exception thrown when circuit breaker is open
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }
}

/// <summary>
/// Null logger for when no logger is provided
/// </summary>
internal class NullStructuredLogger : IStructuredLogger
{
    public void LogInformation(string message, LogContext context) { }
    public void LogWarning(string message, LogContext context, Exception? exception = null) { }
    public void LogError(string message, LogContext context, Exception exception) { }
}
