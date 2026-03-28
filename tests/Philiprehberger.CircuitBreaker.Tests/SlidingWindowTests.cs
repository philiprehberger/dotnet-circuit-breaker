using Xunit;
using Philiprehberger.CircuitBreaker;

namespace Philiprehberger.CircuitBreaker.Tests;

public class SlidingWindowTests
{
    [Fact]
    public void OpensCircuitWhenFailureRateExceedsThreshold()
    {
        // High failureThreshold so only sliding window triggers
        var options = new CircuitBreakerOptions(
            FailureThreshold: 1000,
            SlidingWindowSize: 4,
            FailureRateThreshold: 0.5
        );
        var breaker = new CircuitBreaker(options);

        // 1 success, then 2 failures = 2/3 = 67% > 50%
        breaker.Execute(() => 1);

        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public void StaysClosedWhenFailureRateBelowThreshold()
    {
        var options = new CircuitBreakerOptions(
            FailureThreshold: 1000,
            SlidingWindowSize: 10,
            FailureRateThreshold: 0.8
        );
        var breaker = new CircuitBreaker(options);

        // 3 successes, 1 failure = 1/4 = 25% < 80%
        for (var i = 0; i < 3; i++)
            breaker.Execute(() => 1);

        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public void DisabledWhenSlidingWindowSizeIsZero()
    {
        var options = new CircuitBreakerOptions(
            FailureThreshold: 5,
            SlidingWindowSize: 0,
            FailureRateThreshold: 0.1
        );
        var breaker = new CircuitBreaker(options);

        // 4 failures should not open (below failureThreshold of 5, no sliding window)
        for (var i = 0; i < 4; i++)
            Assert.Throws<InvalidOperationException>(() =>
                breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public void ConsecutiveFailureThresholdStillWorks()
    {
        // Disable sliding window, use only consecutive failure threshold
        var options = new CircuitBreakerOptions(
            FailureThreshold: 2,
            SlidingWindowSize: 0
        );
        var breaker = new CircuitBreaker(options);

        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));
        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.Equal(CircuitState.Open, breaker.State);
    }
}
