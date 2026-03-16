namespace Philiprehberger.CircuitBreaker;

/// <summary>
/// A thread-safe circuit breaker with half-open probing, health tracking, and event callbacks.
/// </summary>
public class CircuitBreaker
{
    private readonly object _lock = new();
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;

    private CircuitState _state = CircuitState.Closed;
    private int _failureCount;
    private DateTimeOffset? _lastFailure;
    private DateTimeOffset? _openedAt;

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
    }

    /// <summary>
    /// Creates a new <see cref="CircuitBreaker"/> from an options record.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    public CircuitBreaker(CircuitBreakerOptions options)
        : this(options.FailureThreshold, options.OpenDuration)
    {
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
    /// Manually trips the circuit breaker into the open state.
    /// </summary>
    public void Trip()
    {
        lock (_lock)
        {
            TransitionTo(CircuitState.Open);
            _openedAt = DateTimeOffset.UtcNow;
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
            _lastFailure = null;
            _openedAt = null;
            TransitionTo(CircuitState.Closed);
        }
    }

    private void EnsureAllowed()
    {
        lock (_lock)
        {
            EvaluateState();

            if (_state == CircuitState.Open)
            {
                var remaining = _openDuration - (DateTimeOffset.UtcNow - _openedAt!.Value);
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
            if (elapsed >= _openDuration)
            {
                TransitionTo(CircuitState.HalfOpen);
            }
        }
    }

    private void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            OnSuccess?.Invoke();

            if (_state == CircuitState.HalfOpen)
            {
                _openedAt = null;
                TransitionTo(CircuitState.Closed);
            }
        }
    }

    private void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailure = DateTimeOffset.UtcNow;
            OnFailure?.Invoke();

            if (_failureCount >= _failureThreshold || _state == CircuitState.HalfOpen)
            {
                _openedAt = DateTimeOffset.UtcNow;
                TransitionTo(CircuitState.Open);
            }
        }
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
