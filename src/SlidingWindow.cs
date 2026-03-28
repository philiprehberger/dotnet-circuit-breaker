namespace Philiprehberger.CircuitBreaker;

/// <summary>
/// A fixed-size circular buffer that tracks success/failure results and computes the failure rate.
/// </summary>
internal sealed class SlidingWindow
{
    private readonly bool[] _buffer;
    private int _index;
    private int _count;
    private int _failureCount;

    /// <summary>
    /// Creates a new <see cref="SlidingWindow"/> with the specified capacity.
    /// </summary>
    /// <param name="size">The maximum number of results to track.</param>
    public SlidingWindow(int size)
    {
        if (size < 1)
            throw new ArgumentOutOfRangeException(nameof(size), "Sliding window size must be at least 1.");

        _buffer = new bool[size];
    }

    /// <summary>
    /// Gets the current failure rate as a value between 0.0 and 1.0.
    /// Returns 0.0 if no results have been recorded.
    /// </summary>
    public double FailureRate => _count == 0 ? 0.0 : (double)_failureCount / _count;

    /// <summary>
    /// Gets the number of results currently tracked.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Records a result in the sliding window.
    /// </summary>
    /// <param name="isFailure">True if the result is a failure, false if it is a success.</param>
    public void Record(bool isFailure)
    {
        if (_count == _buffer.Length)
        {
            // Overwriting the oldest entry
            if (_buffer[_index])
                _failureCount--;
        }
        else
        {
            _count++;
        }

        _buffer[_index] = isFailure;
        if (isFailure)
            _failureCount++;

        _index = (_index + 1) % _buffer.Length;
    }

    /// <summary>
    /// Resets the sliding window, clearing all recorded results.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _index = 0;
        _count = 0;
        _failureCount = 0;
    }
}
