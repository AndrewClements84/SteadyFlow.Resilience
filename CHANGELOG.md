# 📘 Changelog

All notable changes to **SteadyFlow.Resilience** will be documented in this file.

---

## [v0.2.0] – 2025-10-04

### ✨ New Features
- Added **configurable backoff strategies** for `RetryPolicy`:
  - `ExponentialBackoffStrategy` (default)
  - `LinearBackoffStrategy`
  - `FibonacciBackoffStrategy`
  - `JitterBackoffStrategy`
- Added **Metrics & Observability Hooks** (`IMetricsObserver`, `LoggingMetricsObserver`)
  - Unified monitoring for retry, circuit breaker, rate limiting, and batching
  - Observer integration across pipeline and middleware
- Completed **ASP.NET Core Middleware** integration:
  - `app.UseResiliencePipeline()` for plug-and-play resilience in web apps
- Added **Fluent Task Extensions** with full chaining support:
  - `.WithRetryAsync()`, `.WithCircuitBreakerAsync()`, `.WithTokenBucketAsync()`, `.WithSlidingWindowAsync()`
- Introduced **ResiliencePipeline** to orchestrate multiple resilience layers
- Added comprehensive **integration and middleware tests**

### 🧪 Improvements
- Maintained **100% code coverage**
- Full observer coverage in unit and integration tests
- Expanded retry and circuit breaker validation logic
- Minor performance optimizations in rate limiter internals

### 🧹 Housekeeping
- Updated **README.md** with middleware, metrics, and observer examples
- Updated **ROADMAP.md** — all initial milestones completed 🎯
- Refined GitHub Actions & Codecov workflow

---

## [v0.1.3] – 2025-09-30
- Added `ResilienceMiddleware` and `ResiliencePipeline`
- Introduced ASP.NET Core integration
- Added middleware and integration tests

## [v0.1.2] – 2025-09-25
- Introduced Sliding Window rate limiter
- Added Fluent chaining for multiple resilience policies
- Enhanced integration test coverage

## [v0.1.1] – 2025-09-15
- Added Circuit Breaker implementation
- Improved retry behavior and test reliability

## [v0.1.0] – 2025-09-10
- Initial release 🎉  
  Includes core `RetryPolicy`, `TokenBucketRateLimiter`, and `BatchProcessor`.

---

📦 **Project:** [SteadyFlow.Resilience](https://github.com/AndrewClements84/SteadyFlow.Resilience)  
📝 Licensed under the MIT License
