namespace Philiprehberger.CircuitBreaker;

/// <summary>
/// Exception thrown when a request is rejected because the circuit is open.
/// </summary>
public class CircuitBrokenException : InvalidOperationException
{
    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitState State { get; }

    /// <summary>
    /// Gets the time at which the circuit was opened.
    /// </summary>
    public DateTimeOffset OpenedAt { get; }

    /// <summary>
    /// Gets the remaining duration before the circuit transitions to half-open.
    /// </summary>
    public TimeSpan RemainingDuration { get; }

    /// <summary>
    /// Creates a new <see cref="CircuitBrokenException"/>.
    /// </summary>
    /// <param name="state">The current circuit state.</param>
    /// <param name="openedAt">When the circuit was opened.</param>
    /// <param name="remainingDuration">Time remaining before the circuit transitions to half-open.</param>
    public CircuitBrokenException(CircuitState state, DateTimeOffset openedAt, TimeSpan remainingDuration)
        : base($"Circuit is {state}. Retry after {remainingDuration.TotalSeconds:F1}s.")
    {
        State = state;
        OpenedAt = openedAt;
        RemainingDuration = remainingDuration;
    }
}
