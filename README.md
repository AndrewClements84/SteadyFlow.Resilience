
<p align="center">
  <img src="assets/steadyflow_logo_banner.png" alt="SteadyFlow Logo" width="700"/>
</p>

# SteadyFlow.Resilience

✨ Lightweight resilience toolkit for .NET  
Retry policies · Circuit Breaker · Rate limiting (Token Bucket & Sliding Window) · Batch processing · ASP.NET Core Middleware · Metrics & Observability

[![.NET Build, Test & Publish](https://github.com/AndrewClements84/SteadyFlow.Resilience/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/AndrewClements84/SteadyFlow.Resilience/actions/workflows/dotnet.yml) 
[![codecov](https://codecov.io/gh/AndrewClements84/SteadyFlow.Resilience/branch/master/graph/badge.svg)](https://codecov.io/gh/AndrewClements84/SteadyFlow.Resilience)
[![NuGet Version](https://img.shields.io/nuget/v/SteadyFlow.Resilience.svg?logo=nuget)](https://www.nuget.org/packages/SteadyFlow.Resilience) 
[![NuGet Downloads](https://img.shields.io/nuget/dt/SteadyFlow.Resilience)](https://www.nuget.org/packages/SteadyFlow.Resilience) 
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![GitHub Repo stars](https://img.shields.io/github/stars/AndrewClements84/SteadyFlow.Resilience?style=flat&color=2bbc8a)

---

## ✨ Features

- **Retry Policy** – automatic retries with exponential backoff  
- **Circuit Breaker** – stop cascading failures after repeated errors  
- **Rate Limiting**  
  - Token Bucket algorithm with async wait support  
  - Sliding Window algorithm for API-style quotas  
- **Batch Processing** – collect items into timed or size-based batches  
- **ASP.NET Core Middleware** – plug resilience directly into your web app pipeline  
- **Metrics & Observability Hooks** – pluggable observers for retries, breaker state, rate limiting, and batch flushes (`IMetricsObserver`, `LoggingMetricsObserver`)  
- **Fluent Chaining** – build pipelines with `.WithTokenBucketAsync().WithSlidingWindowAsync().WithRetryAsync().WithCircuitBreakerAsync()`  
- **Async-first** – designed for modern .NET apps  
- **Lightweight** – zero external dependencies  
- **100% Test Coverage** – verified with xUnit + Codecov  

---

## 📦 Installation

```bash
dotnet add package SteadyFlow.Resilience
```

---

## 🚀 Usage Examples

### 🔁 Retry with Exponential Backoff

```csharp
using SteadyFlow.Resilience.Retry;

var retry = new RetryPolicy(maxRetries: 5, initialDelayMs: 200);

Func<Task<string>> unreliableAction = async () =>
{
    if (new Random().Next(2) == 0)
        throw new Exception("Transient failure");
    return "Success!";
};

var pipeline = unreliableAction.WithRetryAsync(retry);
var result = await pipeline();

Console.WriteLine(result);
```

---

### ⚡ Circuit Breaker

```csharp
using SteadyFlow.Resilience.Policies;

var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromSeconds(10));

Func<Task> riskyAction = async () =>
{
    if (new Random().Next(3) == 0)
        throw new Exception("Boom!");
    await Task.CompletedTask;
};

var pipeline = riskyAction.WithCircuitBreakerAsync(breaker);
await pipeline(); // executes under breaker control
```

---

### ⏳ Token Bucket Rate Limiting

```csharp
using SteadyFlow.Resilience.RateLimiting;

var limiter = new TokenBucketRateLimiter(capacity: 5, refillRatePerSecond: 2);

for (int i = 0; i < 10; i++)
{
    await limiter.WaitForAvailabilityAsync();
    Console.WriteLine($"Request {i} at {DateTime.Now:HH:mm:ss.fff}");
}
```

---

### 📊 Sliding Window Rate Limiter

```csharp
using SteadyFlow.Resilience.RateLimiting;

var limiter = new SlidingWindowRateLimiter(maxRequests: 3, window: TimeSpan.FromSeconds(10));

for (int i = 0; i < 6; i++)
{
    await limiter.WaitForAvailabilityAsync();
    Console.WriteLine($"Request {i} at {DateTime.UtcNow:HH:mm:ss.fff}");
}
```

---

### 🔗 Fluent Integration Example (TokenBucket + Retry + CircuitBreaker)

```csharp
using SteadyFlow.Resilience.Extensions;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;

var limiter = new TokenBucketRateLimiter(capacity: 2, refillRatePerSecond: 1);
var retry = new RetryPolicy(maxRetries: 3, initialDelayMs: 100);
var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromSeconds(10));

Func<Task> action = async () =>
{
    if (new Random().Next(2) == 0)
        throw new Exception("Simulated transient failure");
    await Task.CompletedTask;
};

var pipeline = action
    .WithTokenBucketAsync(limiter)
    .WithRetryAsync(retry)
    .WithCircuitBreakerAsync(breaker);

await pipeline();
```

---

### 📈 Metrics & Observability (optional)

```csharp
using Microsoft.Extensions.Logging;
using SteadyFlow.Resilience.Metrics;
using SteadyFlow.Resilience.Retry;

ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var logger = loggerFactory.CreateLogger("resilience");
var observer = new LoggingMetricsObserver(logger);

// Pass observer when constructing policies
var retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 50, observer: observer);
```

Observer hooks:
- `OnRetry(int attempt, Exception ex)`
- `OnCircuitOpened()` / `OnCircuitClosed()` / `OnCircuitHalfOpen()`
- `OnRateLimited(string limiterType)`
- `OnBatchProcessed(int itemCount)`
- `OnEvent(string policy, string message)`

---

### 🌐 ASP.NET Core Middleware

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseResiliencePipeline(options =>
    {
        // Pass policies; you may construct them with an observer if you like
        options.Retry = new RetryPolicy(maxRetries: 3);
        options.CircuitBreaker = new CircuitBreakerPolicy(failureThreshold: 5, openDuration: TimeSpan.FromSeconds(30));
        options.SlidingWindowLimiter = new SlidingWindowRateLimiter(maxRequests: 100, window: TimeSpan.FromMinutes(1));
        // or: options.TokenBucketLimiter = new TokenBucketRateLimiter(capacity: 5, refillRatePerSecond: 2);
    });

    app.UseRouting();
    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
}
```

---

## 🧪 Tests

The project includes a full **xUnit test suite**:

- ✅ Unit tests for RetryPolicy, CircuitBreaker, TokenBucket, SlidingWindow, and BatchProcessor  
- ✅ Unit tests for ResiliencePipeline and Middleware  
- ✅ Integration tests combining multiple policies  
- ✅ Middleware integration tests (Retry, CircuitBreaker, TokenBucket, SlidingWindow)  
- ✅ **Metrics hooks** covered end-to-end with `IMetricsObserver` and `LoggingMetricsObserver`  
- ✅ CI/CD runs all tests on every commit (GitHub Actions + Codecov)  
- ✅ **100% code coverage**  

Run locally:

```bash
dotnet test
```

---

## 📈 Roadmap

- [x] Add Circuit Breaker policy  
- [x] Support sliding window rate limiter  
- [x] ASP.NET middleware integration  
- [x] Metrics & observability hooks  
- [ ] Configurable backoff strategies (jitter, linear, Fibonacci)  

---

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

1. Fork the repo  
2. Create a feature branch  
3. Add tests & code  
4. Open a PR 🎉  

---

## ⭐ Support

If you find **SteadyFlow.Resilience** useful, please consider giving it a star on GitHub — it helps others discover the project and shows your support.

---

## 📄 License

Licensed under the **MIT License** – see [LICENSE](LICENSE) for details.
