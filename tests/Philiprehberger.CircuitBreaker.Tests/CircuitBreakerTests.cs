using Xunit;
using Philiprehberger.CircuitBreaker;

namespace Philiprehberger.CircuitBreaker.Tests;

public class CircuitBreakerTests
{
    [Fact]
    public void StartsInClosedState()
    {
        var breaker = new CircuitBreaker();
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public void OpensAfterFailureThreshold()
    {
        var breaker = new CircuitBreaker(failureThreshold: 3);

        for (var i = 0; i < 3; i++)
            Assert.Throws<InvalidOperationException>(() => breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public void RejectsRequestsWhenOpen()
    {
        var breaker = new CircuitBreaker(failureThreshold: 1);
        Assert.Throws<InvalidOperationException>(() => breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.Throws<CircuitBrokenException>(() => breaker.Execute(() => 42));
    }

    [Fact]
    public void TripManuallyOpensCircuit()
    {
        var breaker = new CircuitBreaker();
        breaker.Trip();
        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public void ResetClosesCircuit()
    {
        var breaker = new CircuitBreaker();
        breaker.Trip();
        breaker.Reset();
        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.Equal(0, breaker.FailureCount);
    }

    [Fact]
    public async Task ExecuteAsyncReturnsResult()
    {
        var breaker = new CircuitBreaker();
        var result = await breaker.ExecuteAsync(() => Task.FromResult(42));
        Assert.Equal(42, result);
    }

    [Fact]
    public void ExecuteReturnsResult()
    {
        var breaker = new CircuitBreaker();
        var result = breaker.Execute(() => 42);
        Assert.Equal(42, result);
    }

    [Fact]
    public void InvokesEventCallbacks()
    {
        var openCalled = false;
        var failureCalled = false;
        var breaker = new CircuitBreaker(failureThreshold: 1);
        breaker.OnOpen = () => openCalled = true;
        breaker.OnFailure = () => failureCalled = true;

        Assert.Throws<InvalidOperationException>(() => breaker.Execute<int>(() => throw new InvalidOperationException()));

        Assert.True(failureCalled);
        Assert.True(openCalled);
    }
}
