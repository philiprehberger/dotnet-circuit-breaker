namespace Philiprehberger.CircuitBreaker;

/// <summary>
/// Represents the possible states of a circuit breaker.
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// The circuit is closed and requests flow through normally.
    /// </summary>
    Closed,

    /// <summary>
    /// The circuit is open and requests are rejected immediately.
    /// </summary>
    Open,

    /// <summary>
    /// The circuit is testing whether the downstream service has recovered.
    /// </summary>
    HalfOpen
}
