namespace Philiprehberger.CircuitBreaker;

/// <summary>
/// Configuration options for a <see cref="CircuitBreaker"/> instance.
/// </summary>
/// <param name="FailureThreshold">Number of consecutive failures before the circuit opens. Defaults to 5.</param>
/// <param name="OpenDuration">How long the circuit stays open before transitioning to half-open. Defaults to 30 seconds.</param>
/// <param name="HalfOpenTimeout">Maximum time allowed for a probe request in the half-open state. Defaults to null (no timeout).</param>
public record CircuitBreakerOptions(
    int FailureThreshold = 5,
    TimeSpan? OpenDuration = null,
    TimeSpan? HalfOpenTimeout = null);
