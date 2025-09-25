# SteadyFlow.Resilience Roadmap

This document outlines the planned features, improvements, and long-term goals for **SteadyFlow.Resilience**.  
The roadmap is not fixed and may evolve based on feedback, contributions, and user needs.

---

## ✅ Current (v0.1.0)

- [x] Retry policy with exponential backoff
- [x] Token bucket rate limiter
- [x] Batch processor (size-based & time-based)
- [x] Unit tests for all core features
- [x] Integration tests for combined usage
- [x] GitHub Actions CI for build & test
- [x] MIT license & contributing guide

---

## 🎯 Short-Term Goals (v0.2.x)

- [ ] Add **circuit breaker** policy
- [ ] Sliding window & leaky bucket rate limiters
- [ ] Configurable retry strategies (jitter, linear, Fibonacci backoff)
- [ ] Async cancellation support in all policies
- [ ] Improved logging hooks for retries, rate limiting, and batches

---

## 🚀 Medium-Term Goals (v0.3.x – v0.4.x)

- [ ] ASP.NET Core middleware for retry/rate limiting
- [ ] HttpClient integration wrapper (`HttpClient.WithResilience()`)
- [ ] Distributed rate limiter (Redis support)
- [ ] Metrics & observability integration (Prometheus, Application Insights)
- [ ] NuGet package auto-publish via GitHub Actions on release

---

## 🌐 Long-Term Vision (v1.0.0)

- A **comprehensive resilience toolkit** for .NET apps
- Feature parity with established libraries (e.g., Polly, Resilience4j) but lightweight and modular
- Plug-and-play policies for retry, rate limiting, batching, circuit breaking, and timeouts
- Production-ready documentation, samples, and API reference
- Community-driven contributions and regular releases

---

## 💡 Ideas / Stretch Goals

- [ ] UI dashboard for monitoring rate limits and retries
- [ ] Machine-learning assisted retry strategies
- [ ] Cross-language bindings (e.g., a Rust/Python port)

---

## 📢 Feedback & Contributions

The roadmap is open to discussion!  
If you’d like to propose changes or vote on priorities, please [open an issue](https://github.com/AndrewClements84/SteadyFlow.Resilience/issues).
