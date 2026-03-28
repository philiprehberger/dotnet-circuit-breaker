namespace Philiprehberger.CircuitBreaker;

/// <summary>
/// A thread-safe circuit breaker with sliding window failure rate, fallback support,
/// configurable half-open success threshold, jitter on open duration, and event callbacks.
/// </summary>
public class CircuitBreaker
{
    private readonly object _lock = new();
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly SlidingWindow? _slidingWindow;
    private readonly double _failureRateThreshold;
    private readonly int _successThresholdInHalfOpen;
    private readonly double _jitterRatio;
    private readonly Random _random = new();

    private CircuitState _state = CircuitState.Closed;
    private int _failureCount;
    private int _halfOpenSuccessCount;
    private DateTimeOffset? _lastFailure;
    private DateTimeOffset? _openedAt;
    private TimeSpan _currentOpenDuration;

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                EvaluateState();
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets the number of consecutive failures recorded.
    /// </summary>
    public int FailureCount
    {
        get { lock (_lock) return _failureCount; }
    }

    /// <summary>
    /// Gets the time of the most recent failure, or null if no failures have occurred.
    /// </summary>
    public DateTimeOffset? LastFailure
    {
        get { lock (_lock) return _lastFailure; }
    }

    /// <summary>
    /// Callback invoked when the circuit transitions to the <see cref="CircuitState.Open"/> state.
    /// </summary>
    public Action? OnOpen { get; set; }

    /// <summary>
    /// Callback invoked when the circuit transitions to the <see cref="CircuitState.Closed"/> state.
    /// </summary>
    public Action? OnClose { get; set; }

    /// <summary>
    /// Callback invoked when the circuit transitions to the <see cref="CircuitState.HalfOpen"/> state.
    /// </summary>
    public Action? OnHalfOpen { get; set; }

    /// <summary>
    /// Callback invoked when a failure is recorded.
    /// </summary>
    public Action? OnFailure { get; set; }

    /// <summary>
    /// Callback invoked when an operation succeeds.
    /// </summary>
    public Action? OnSuccess { get; set; }

    /// <summary>
    /// Creates a new <see cref="CircuitBreaker"/> instance.
    /// </summary>
    /// <param name="failureThreshold">Number of consecutive failures before the circuit opens. Defaults to 5.</param>
    /// <param name="openDuration">How long the circuit stays open before transitioning to half-open. Defaults to 30 seconds.</param>
    public CircuitBreaker(int failureThreshold = 5, TimeSpan? openDuration = null)
    {
        if (failureThreshold < 1)
            throw new ArgumentOutOfRangeException(nameof(failureThreshold), "Failure threshold must be at least 1.");

        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
        _currentOpenDuration = _openDuration;
        _failureRateThreshold = 0.5;
        _successThresholdInHalfOpen = 1;
        _jitterRatio = 0.0;
    }

    /// <summary>
    /// Creates a new <see cref="CircuitBreaker"/> from an options record.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    public CircuitBreaker(CircuitBreakerOptions options)
    {
        if (options.FailureThreshold < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "Failure threshold must be at least 1.");
        if (options.FailureRateThreshold is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(options), "Failure rate threshold must be between 0.0 and 1.0.");
        if (options.SuccessThresholdInHalfOpen < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "Success threshold in half-open must be at least 1.");
        if (options.JitterRatio is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(options), "Jitter ratio must be between 0.0 and 1.0.");

        _failureThreshold = options.FailureThreshold;
        _openDuration = options.OpenDuration ?? TimeSpan.FromSeconds(30);
        _currentOpenDuration = _openDuration;
        _failureRateThreshold = options.FailureRateThreshold;
        _successThresholdInHalfOpen = options.SuccessThresholdInHalfOpen;
        _jitterRatio = options.JitterRatio;

        if (options.SlidingWindowSize > 0)
            _slidingWindow = new SlidingWindow(options.SlidingWindowSize);
    }

    /// <summary>
    /// Executes an asynchronous operation through the circuit breaker.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="action">The asynchronous operation to execute.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="CircuitBrokenException">Thrown when the circuit is open and the request is rejected.</exception>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        EnsureAllowed();

        try
        {
            var result = await action().ConfigureAwait(false);
            RecordSuccess();
            return result;
        }
        catch (Exception)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Executes an asynchronous operation through the circuit breaker with a fallback.
    /// When the circuit is open, the fallback is invoked instead of throwing <see cref="CircuitBrokenException"/>.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="action">The asynchronous operation to execute.</param>
    /// <param name="fallback">The fallback operation to invoke when the circuit is open.</param>
    /// <returns>The result of the operation or the fallback.</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<Task<T>> fallback)
    {
        if (IsOpen())
            return await fallback().ConfigureAwait(false);

        try
        {
            EnsureAllowed();
            var result = await action().ConfigureAwait(false);
            RecordSuccess();
            return result;
        }
        catch (CircuitBrokenException)
        {
            return await fallback().ConfigureAwait(false);
        }
        catch (Exception)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Executes a synchronous operation through the circuit breaker.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="action">The operation to execute.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="CircuitBrokenException">Thrown when the circuit is open and the request is rejected.</exception>
    public T Execute<T>(Func<T> action)
    {
        EnsureAllowed();

        try
        {
            var result = action();
            RecordSuccess();
            return result;
        }
        catch (Exception)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Executes a synchronous operation through the circuit breaker with a fallback.
    /// When the circuit is open, the fallback is invoked instead of throwing <see cref="CircuitBrokenException"/>.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="action">The operation to execute.</param>
    /// <param name="fallback">The fallback operation to invoke when the circuit is open.</param>
    /// <returns>The result of the operation or the fallback.</returns>
    public T Execute<T>(Func<T> action, Func<T> fallback)
    {
        if (IsOpen())
            return fallback();

        try
        {
            EnsureAllowed();
            var result = action();
            RecordSuccess();
            return result;
        }
        catch (CircuitBrokenException)
        {
            return fallback();
        }
        catch (Exception)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Manually trips the circuit breaker into the open state.
    /// </summary>
    public void Trip()
    {
        lock (_lock)
        {
            _openedAt = DateTimeOffset.UtcNow;
            _currentOpenDuration = ComputeOpenDuration();
            TransitionTo(CircuitState.Open);
        }
    }

    /// <summary>
    /// Manually resets the circuit breaker to the closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _halfOpenSuccessCount = 0;
            _lastFailure = null;
            _openedAt = null;
            _slidingWindow?.Reset();
            TransitionTo(CircuitState.Closed);
        }
    }

    private bool IsOpen()
    {
        lock (_lock)
        {
            EvaluateState();
            return _state == CircuitState.Open;
        }
    }

    private void EnsureAllowed()
    {
        lock (_lock)
        {
            EvaluateState();

            if (_state == CircuitState.Open)
            {
                var remaining = _currentOpenDuration - (DateTimeOffset.UtcNow - _openedAt!.Value);
                if (remaining < TimeSpan.Zero)
                    remaining = TimeSpan.Zero;

                throw new CircuitBrokenException(_state, _openedAt.Value, remaining);
            }
        }
    }

    private void EvaluateState()
    {
        if (_state == CircuitState.Open && _openedAt.HasValue)
        {
            var elapsed = DateTimeOffset.UtcNow - _openedAt.Value;
            if (elapsed >= _currentOpenDuration)
            {
                _halfOpenSuccessCount = 0;
                TransitionTo(CircuitState.HalfOpen);
            }
        }
    }

    private void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _slidingWindow?.Record(isFailure: false);
            OnSuccess?.Invoke();

            if (_state == CircuitState.HalfOpen)
            {
                _halfOpenSuccessCount++;
                if (_halfOpenSuccessCount >= _successThresholdInHalfOpen)
                {
                    _openedAt = null;
                    _halfOpenSuccessCount = 0;
                    TransitionTo(CircuitState.Closed);
                }
            }
        }
    }

    private void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailure = DateTimeOffset.UtcNow;
            _slidingWindow?.Record(isFailure: true);
            OnFailure?.Invoke();

            var shouldOpen = false;

            if (_state == CircuitState.HalfOpen)
            {
                shouldOpen = true;
            }
            else if (_failureCount >= _failureThreshold)
            {
                shouldOpen = true;
            }
            else if (_slidingWindow != null &&
                     _slidingWindow.FailureRate > _failureRateThreshold)
            {
                shouldOpen = true;
            }

            if (shouldOpen)
            {
                _openedAt = DateTimeOffset.UtcNow;
                _currentOpenDuration = ComputeOpenDuration();
                _halfOpenSuccessCount = 0;
                TransitionTo(CircuitState.Open);
            }
        }
    }

    private TimeSpan ComputeOpenDuration()
    {
        if (_jitterRatio <= 0.0)
            return _openDuration;

        var jitter = (_random.NextDouble() * 2.0 - 1.0) * _jitterRatio;
        var multiplier = 1.0 + jitter;
        return TimeSpan.FromTicks((long)(_openDuration.Ticks * multiplier));
    }

    private void TransitionTo(CircuitState newState)
    {
        if (_state == newState) return;

        _state = newState;

        switch (newState)
        {
            case CircuitState.Open:
                OnOpen?.Invoke();
                break;
            case CircuitState.Closed:
                OnClose?.Invoke();
                break;
            case CircuitState.HalfOpen:
                OnHalfOpen?.Invoke();
                break;
        }
    }
}
