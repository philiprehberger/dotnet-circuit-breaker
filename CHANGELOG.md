# Changelog

## 0.2.0 (2026-03-27)

- Add sliding window failure rate monitoring with configurable window size
- Add fallback support for graceful degradation when circuit is open
- Add configurable success threshold for half-open to closed transition
- Add jitter on open duration to prevent thundering herd recovery

## 0.1.6 (2026-03-23)

- Sync .csproj description with README

## 0.1.5 (2026-03-22)

- Add dates to changelog entries

## 0.1.4 (2026-03-17)

- Rename Install section to Installation in README per package guide

## 0.1.3 (2026-03-16)

- Add Development section to README
- Add GenerateDocumentationFile, RepositoryType, PackageReadmeFile to .csproj

## 0.1.2 (2026-03-16)

## 0.1.1 (2026-03-16)

- Fix: add NuGet publishing secret

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
