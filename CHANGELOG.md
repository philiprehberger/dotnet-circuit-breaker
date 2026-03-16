# Changelog

## 0.1.0 (2026-03-15)

- Initial release
- Circuit breaker state machine with Closed, Open, and HalfOpen states
- Synchronous and asynchronous execution via `Execute<T>` and `ExecuteAsync<T>`
- Configurable failure threshold and open duration
- Event callbacks: OnOpen, OnClose, OnHalfOpen, OnFailure, OnSuccess
- Manual Trip() and Reset() controls
- Thread-safe implementation
- CircuitBrokenException with state details and remaining duration
- CircuitBreakerOptions configuration record
