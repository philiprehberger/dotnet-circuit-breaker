# Philiprehberger.CircuitBreaker

[![CI](https://github.com/philiprehberger/dotnet-circuit-breaker/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-circuit-breaker/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.CircuitBreaker.svg)](https://www.nuget.org/packages/Philiprehberger.CircuitBreaker)
[![License](https://img.shields.io/github/license/philiprehberger/dotnet-circuit-breaker)](LICENSE)

Standalone circuit breaker with half-open probing, health tracking, and event callbacks.

## Install

```bash
dotnet add package Philiprehberger.CircuitBreaker
```

## Usage

```csharp
using Philiprehberger.CircuitBreaker;

var breaker = new CircuitBreaker(failureThreshold: 3, openDuration: TimeSpan.FromSeconds(10));

breaker.OnOpen += () => Console.WriteLine("Circuit opened!");
breaker.OnClose += () => Console.WriteLine("Circuit closed.");
breaker.OnHalfOpen += () => Console.WriteLine("Circuit half-open, probing...");

// Synchronous execution
var result = breaker.Execute(() => ComputeValue());

// Asynchronous execution
var data = await breaker.ExecuteAsync(() => httpClient.GetStringAsync("/api/health"));
```

### Using Options

```csharp
var options = new CircuitBreakerOptions(
    FailureThreshold: 5,
    OpenDuration: TimeSpan.FromSeconds(30)
);

var breaker = new CircuitBreaker(options);
```

### Manual Control

```csharp
breaker.Trip();   // Force the circuit open
breaker.Reset();  // Force the circuit closed
```

### Handling Rejected Requests

```csharp
try
{
    var result = breaker.Execute(() => CallService());
}
catch (CircuitBrokenException ex)
{
    Console.WriteLine($"State: {ex.State}, retry in {ex.RemainingDuration.TotalSeconds}s");
}
```

## API

### `CircuitBreaker`

| Member | Description |
|--------|-------------|
| `CircuitBreaker(int failureThreshold = 5, TimeSpan? openDuration = null)` | Creates a new circuit breaker |
| `CircuitBreaker(CircuitBreakerOptions options)` | Creates a circuit breaker from options |
| `Execute<T>(Func<T>)` | Executes a synchronous operation through the breaker |
| `ExecuteAsync<T>(Func<Task<T>>)` | Executes an asynchronous operation through the breaker |
| `Trip()` | Manually opens the circuit |
| `Reset()` | Manually closes the circuit and resets failure count |
| `State` | Current `CircuitState` (Closed, Open, HalfOpen) |
| `FailureCount` | Number of consecutive failures |
| `LastFailure` | Timestamp of the most recent failure |
| `OnOpen` | Callback when circuit opens |
| `OnClose` | Callback when circuit closes |
| `OnHalfOpen` | Callback when circuit enters half-open |
| `OnFailure` | Callback on each failure |
| `OnSuccess` | Callback on each success |

### `CircuitState`

| Value | Description |
|-------|-------------|
| `Closed` | Requests flow through normally |
| `Open` | Requests are rejected immediately |
| `HalfOpen` | A single probe request is allowed to test recovery |

### `CircuitBreakerOptions`

| Property | Default | Description |
|----------|---------|-------------|
| `FailureThreshold` | `5` | Failures before opening |
| `OpenDuration` | `null` (30s) | Duration circuit stays open |
| `HalfOpenTimeout` | `null` | Timeout for half-open probe |

### `CircuitBrokenException`

| Property | Description |
|----------|-------------|
| `State` | Circuit state when the exception was thrown |
| `OpenedAt` | When the circuit was opened |
| `RemainingDuration` | Time until the circuit transitions to half-open |

## License

MIT
