namespace Philiprehberger.CircuitBreaker;

/// <summary>
/// Configuration options for a <see cref="CircuitBreaker"/> instance.
/// </summary>
/// <param name="FailureThreshold">Number of consecutive failures before the circuit opens. Defaults to 5.</param>
/// <param name="OpenDuration">How long the circuit stays open before transitioning to half-open. Defaults to 30 seconds.</param>
/// <param name="HalfOpenTimeout">Maximum time allowed for a probe request in the half-open state. Defaults to null (no timeout).</param>
/// <param name="SlidingWindowSize">Number of results to track in the sliding window. Defaults to 100. Set to 0 to disable sliding window and use consecutive failure count only.</param>
/// <param name="FailureRateThreshold">Failure rate (0.0-1.0) that triggers the circuit to open when using the sliding window. Defaults to 0.5.</param>
/// <param name="SuccessThresholdInHalfOpen">Number of consecutive successes required in half-open state before closing the circuit. Defaults to 1.</param>
/// <param name="JitterRatio">Random jitter applied to the open duration (0.0-1.0). The actual duration is openDuration * (1 + random(-jitter, +jitter)). Defaults to 0.0 (no jitter).</param>
public record CircuitBreakerOptions(
    int FailureThreshold = 5,
    TimeSpan? OpenDuration = null,
    TimeSpan? HalfOpenTimeout = null,
    int SlidingWindowSize = 100,
    double FailureRateThreshold = 0.5,
    int SuccessThresholdInHalfOpen = 1,
    double JitterRatio = 0.0);
