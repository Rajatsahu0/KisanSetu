# Resilience Infrastructure

This module provides circuit breaker pattern implementation for protecting against cascading failures when calling external services.

## Components

### ICircuitBreaker
Interface for circuit breaker pattern implementation.

**Methods:**
- `ExecuteAsync<T>(operation, cancellationToken)` - Execute with protection
- `ExecuteAsync<T>(operation, fallback, cancellationToken)` - Execute with fallback
- `GetState()` - Get current circuit state

### CircuitBreaker
Implementation of circuit breaker pattern with three states:
- **Closed** - Normal operation, requests pass through
- **Open** - Circuit is open, requests fail fast
- **HalfOpen** - Testing if service recovered

**Configuration:**
- `failureThreshold` - Number of failures before opening (default: 5)
- `openDuration` - How long circuit stays open (default: 1 minute)
- `halfOpenTimeout` - Timeout for test requests in half-open state (default: 30 seconds)

### CircuitBreakerFactory
Factory for creating and managing circuit breakers per service.

**Pre-configured Circuit Breakers:**
- `GetMandiPriceCircuitBreaker()` - For Mandi Price API
- `GetWeatherApiCircuitBreaker()` - For Weather API
- `GetGovernmentApiCircuitBreaker()` - For Government Integration

## Circuit Breaker States

```
Closed (Normal) --[5 failures]--> Open (Failing Fast)
                                    |
                                    | [1 minute]
                                    v
                                  HalfOpen (Testing)
                                    |
                    [Success]       |       [Failure]
                      |             |             |
                      v             |             v
                    Closed <--------+----------> Open
```

## Configuration

Register in `Program.cs`:
```csharp
services.AddCircuitBreakers();
```

## Usage

### Basic Usage
```csharp
public class MandiPriceService
{
    private readonly ICircuitBreaker _circuitBreaker;

    public MandiPriceService(CircuitBreakerFactory factory)
    {
        _circuitBreaker = factory.GetMandiPriceCircuitBreaker();
    }

    public async Task<MandiPrice> GetPriceAsync(string commodity, string location)
    {
        return await _circuitBreaker.ExecuteAsync(async ct =>
        {
            // Call external Mandi API
            return await _mandiApi.GetPriceAsync(commodity, location, ct);
        });
    }
}
```

### With Fallback
```csharp
public async Task<MandiPrice> GetPriceAsync(string commodity, string location)
{
    return await _circuitBreaker.ExecuteAsync(
        operation: async ct =>
        {
            // Call external Mandi API
            return await _mandiApi.GetPriceAsync(commodity, location, ct);
        },
        fallback: async () =>
        {
            // Return cached price when circuit is open
            return await _cache.GetCachedPriceAsync(commodity, location);
        }
    );
}
```

### Check Circuit State
```csharp
var state = _circuitBreaker.GetState();
if (state == CircuitState.Open)
{
    // Circuit is open, service is unavailable
    return cachedData;
}
```

## Behavior

### Closed State
- All requests pass through normally
- Failures are counted
- When failure count reaches threshold, circuit opens

### Open State
- All requests fail immediately (fail fast)
- No requests are sent to the failing service
- After open duration, circuit transitions to half-open
- Fallback is used if provided

### HalfOpen State
- One test request is allowed
- If successful, circuit closes
- If fails, circuit reopens
- Test request has timeout (30 seconds)

## Logging

Circuit breaker logs all state transitions:
- Opening circuit after threshold failures
- Transitioning to half-open for testing
- Closing circuit after successful test
- Reopening circuit after failed test
- Using fallback responses

## Requirements

Validates Requirements:
- 6.2: Retry policies and error handling
- 6.3: Fallback responses for service failures
