using Xunit;
using Philiprehberger.CircuitBreaker;

namespace Philiprehberger.CircuitBreaker.Tests;

public class FallbackTests
{
    [Fact]
    public void ExecuteWithFallbackReturnsFallbackWhenOpen()
    {
        var breaker = new CircuitBreaker(failureThreshold: 1);
        breaker.Trip();

        var result = breaker.Execute(() => 42, () => -1);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void ExecuteWithFallbackReturnsResultWhenClosed()
    {
        var breaker = new CircuitBreaker();
        var result = breaker.Execute(() => 42, () => -1);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsyncWithFallbackReturnsFallbackWhenOpen()
    {
        var breaker = new CircuitBreaker(failureThreshold: 1);
        breaker.Trip();

        var result = await breaker.ExecuteAsync(() => Task.FromResult(42), () => Task.FromResult(-1));
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task ExecuteAsyncWithFallbackReturnsResultWhenClosed()
    {
        var breaker = new CircuitBreaker();
        var result = await breaker.ExecuteAsync(() => Task.FromResult(42), () => Task.FromResult(-1));
        Assert.Equal(42, result);
    }

    [Fact]
    public void ExecuteWithoutFallbackThrowsWhenOpen()
    {
        var breaker = new CircuitBreaker(failureThreshold: 1);
        breaker.Trip();

        Assert.Throws<CircuitBrokenException>(() => breaker.Execute(() => 42));
    }
}
