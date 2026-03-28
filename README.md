# Philiprehberger.CircuitBreaker

[![CI](https://github.com/philiprehberger/dotnet-circuit-breaker/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-circuit-breaker/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.CircuitBreaker.svg)](https://www.nuget.org/packages/Philiprehberger.CircuitBreaker)
[![GitHub release](https://img.shields.io/github/v/release/philiprehberger/dotnet-circuit-breaker)](https://github.com/philiprehberger/dotnet-circuit-breaker/releases)
[![Last updated](https://img.shields.io/github/last-commit/philiprehberger/dotnet-circuit-breaker)](https://github.com/philiprehberger/dotnet-circuit-breaker/commits/main)
[![License](https://img.shields.io/github/license/philiprehberger/dotnet-circuit-breaker)](LICENSE)
[![Bug Reports](https://img.shields.io/github/issues/philiprehberger/dotnet-circuit-breaker/bug)](https://github.com/philiprehberger/dotnet-circuit-breaker/issues?q=is%3Aissue+is%3Aopen+label%3Abug)
[![Feature Requests](https://img.shields.io/github/issues/philiprehberger/dotnet-circuit-breaker/enhancement)](https://github.com/philiprehberger/dotnet-circuit-breaker/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement)
[![Sponsor](https://img.shields.io/badge/sponsor-GitHub%20Sponsors-ec6cb9)](https://github.com/sponsors/philiprehberger)

Standalone circuit breaker with sliding window failure rate, fallback support, half-open probing, and event callbacks.

## Installation

```bash
dotnet add package Philiprehberger.CircuitBreaker
```

## Usage

```csharp
using Philiprehberger.CircuitBreaker;

var breaker = new CircuitBreaker(failureThreshold: 3, openDuration: TimeSpan.FromSeconds(10));

breaker.OnOpen = () => Console.WriteLine("Circuit opened!");
breaker.OnClose = () => Console.WriteLine("Circuit closed.");
breaker.OnHalfOpen = () => Console.WriteLine("Circuit half-open, probing...");

var result = breaker.Execute(() => ComputeValue());
var data = await breaker.ExecuteAsync(() => httpClient.GetStringAsync("/api/health"));
```

### Sliding Window Failure Rate

Track the last N results and open the circuit when the failure rate exceeds a threshold, rather than requiring consecutive failures.

```csharp
var options = new CircuitBreakerOptions(
    FailureThreshold: 50,
    SlidingWindowSize: 100,
    FailureRateThreshold: 0.5
);

var breaker = new CircuitBreaker(options);
```

### Fallback Support

Provide a fallback that runs when the circuit is open instead of throwing `CircuitBrokenException`.

```csharp
var result = breaker.Execute(
    () => CallRemoteService(),
    () => GetCachedValue()
);

var data = await breaker.ExecuteAsync(
    () => httpClient.GetStringAsync("/api/data"),
    () => Task.FromResult("{\"fallback\": true}")
);
```

### Success Threshold in Half-Open

Require multiple consecutive successes in half-open state before closing the circuit.

```csharp
var options = new CircuitBreakerOptions(
    FailureThreshold: 5,
    OpenDuration: TimeSpan.FromSeconds(30),
    SuccessThresholdInHalfOpen: 3
);

var breaker = new CircuitBreaker(options);
```

### Jitter on Open Duration

Add random jitter to the open duration to prevent thundering herd when multiple circuit breakers recover simultaneously.

```csharp
var options = new CircuitBreakerOptions(
    FailureThreshold: 5,
    OpenDuration: TimeSpan.FromSeconds(30),
    JitterRatio: 0.2
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
| `Execute<T>(Func<T>, Func<T>)` | Executes with a fallback when the circuit is open |
| `ExecuteAsync<T>(Func<Task<T>>)` | Executes an asynchronous operation through the breaker |
| `ExecuteAsync<T>(Func<Task<T>>, Func<Task<T>>)` | Executes async with a fallback when the circuit is open |
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

### `CircuitBreakerOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `FailureThreshold` | `int` | `5` | Consecutive failures before opening |
| `OpenDuration` | `TimeSpan?` | `null` (30s) | Duration circuit stays open |
| `HalfOpenTimeout` | `TimeSpan?` | `null` | Timeout for half-open probe |
| `SlidingWindowSize` | `int` | `100` | Number of results to track (0 to disable) |
| `FailureRateThreshold` | `double` | `0.5` | Failure rate (0.0-1.0) that triggers open |
| `SuccessThresholdInHalfOpen` | `int` | `1` | Consecutive successes to close from half-open |
| `JitterRatio` | `double` | `0.0` | Random jitter on open duration (0.0-1.0) |

### `CircuitState`

| Value | Description |
|-------|-------------|
| `Closed` | Requests flow through normally |
| `Open` | Requests are rejected immediately |
| `HalfOpen` | Probe requests test whether the service has recovered |

### `CircuitBrokenException`

| Property | Description |
|----------|-------------|
| `State` | Circuit state when the exception was thrown |
| `OpenedAt` | When the circuit was opened |
| `RemainingDuration` | Time until the circuit transitions to half-open |

## Development

```bash
dotnet build src/Philiprehberger.CircuitBreaker.csproj --configuration Release
```

## Support

If you find this package useful, consider giving it a star on GitHub — it helps motivate continued maintenance and development.

[![LinkedIn](https://img.shields.io/badge/Philip%20Rehberger-LinkedIn-0A66C2?logo=linkedin)](https://www.linkedin.com/in/philiprehberger)
[![More packages](https://img.shields.io/badge/more-open%20source%20packages-blue)](https://philiprehberger.com/open-source-packages)

## License

[MIT](LICENSE)
