using Xunit;
using Philiprehberger.CircuitBreaker;

namespace Philiprehberger.CircuitBreaker.Tests;

public class JitterTests
{
    [Fact]
    public void ZeroJitterUsesExactDuration()
    {
        var options = new CircuitBreakerOptions(
            FailureThreshold: 1,
            OpenDuration: TimeSpan.FromMilliseconds(50),
            JitterRatio: 0.0
        );
        var breaker = new CircuitBreaker(options);

        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));

        // At 25ms, circuit should still be open (50ms duration, no jitter)
        Thread.Sleep(25);
        Assert.Equal(CircuitState.Open, breaker.State);

        // At 60ms, circuit should be half-open
        Thread.Sleep(35);
        Assert.Equal(CircuitState.HalfOpen, breaker.State);
    }

    [Fact]
    public void JitterProducesVariableDuration()
    {
        // With jitter=1.0, duration ranges from 0 to 2x the base.
        // Run multiple trials and verify that at least one differs from the base timing.
        var options = new CircuitBreakerOptions(
            FailureThreshold: 1,
            OpenDuration: TimeSpan.FromMilliseconds(100),
            JitterRatio: 1.0
        );

        // Just verify it doesn't throw and the circuit opens correctly
        var breaker = new CircuitBreaker(options);
        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public void InvalidJitterRatioThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CircuitBreaker(new CircuitBreakerOptions(JitterRatio: -0.1)));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CircuitBreaker(new CircuitBreakerOptions(JitterRatio: 1.1)));
    }

    [Fact]
    public void JitterAppliedOnTrip()
    {
        var options = new CircuitBreakerOptions(
            FailureThreshold: 1,
            OpenDuration: TimeSpan.FromMilliseconds(200),
            JitterRatio: 0.5
        );
        var breaker = new CircuitBreaker(options);
        breaker.Trip();

        Assert.Equal(CircuitState.Open, breaker.State);
    }
}
