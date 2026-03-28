using Xunit;
using Philiprehberger.CircuitBreaker;

namespace Philiprehberger.CircuitBreaker.Tests;

public class HalfOpenSuccessThresholdTests
{
    [Fact]
    public void RequiresMultipleSuccessesBeforeClosing()
    {
        var options = new CircuitBreakerOptions(
            FailureThreshold: 1,
            OpenDuration: TimeSpan.FromMilliseconds(1),
            SuccessThresholdInHalfOpen: 3
        );
        var breaker = new CircuitBreaker(options);

        // Trip the circuit
        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.Equal(CircuitState.Open, breaker.State);

        // Wait for half-open
        Thread.Sleep(10);
        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        // First success: still half-open
        breaker.Execute(() => 1);
        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        // Second success: still half-open
        breaker.Execute(() => 1);
        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        // Third success: closes
        breaker.Execute(() => 1);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public void FailureInHalfOpenReopensCircuit()
    {
        var options = new CircuitBreakerOptions(
            FailureThreshold: 1,
            OpenDuration: TimeSpan.FromMilliseconds(1),
            SuccessThresholdInHalfOpen: 3
        );
        var breaker = new CircuitBreaker(options);

        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));

        Thread.Sleep(10);
        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        // One success, then a failure
        breaker.Execute(() => 1);
        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public void DefaultThresholdOfOneClosesImmediately()
    {
        var options = new CircuitBreakerOptions(
            FailureThreshold: 1,
            OpenDuration: TimeSpan.FromMilliseconds(1),
            SuccessThresholdInHalfOpen: 1
        );
        var breaker = new CircuitBreaker(options);

        Assert.Throws<InvalidOperationException>(() =>
            breaker.Execute<int>(() => throw new InvalidOperationException()));

        Thread.Sleep(10);
        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        breaker.Execute(() => 1);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }
}
