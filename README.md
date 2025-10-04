<p align="center">
  <img src="assets/steadyflow_logo_banner.png" alt="SteadyFlow Logo" width="700"/>
</p>

# SteadyFlow.Resilience

✨ Lightweight resilience toolkit for .NET  
Retry policies · Circuit Breaker · Rate limiting (Token Bucket & Sliding Window) · Batch processing · ASP.NET Core Middleware · Metrics & Observability · Configurable Backoff Strategies

[![.NET Build, Test & Publish](https://github.com/AndrewClements84/SteadyFlow.Resilience/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/AndrewClements84/SteadyFlow.Resilience/actions/workflows/dotnet.yml) 
[![codecov](https://codecov.io/gh/AndrewClements84/SteadyFlow.Resilience/branch/master/graph/badge.svg)](https://codecov.io/gh/AndrewClements84/SteadyFlow.Resilience)
[![NuGet Version](https://img.shields.io/nuget/v/SteadyFlow.Resilience.svg?logo=nuget)](https://www.nuget.org/packages/SteadyFlow.Resilience) 
[![NuGet Downloads](https://img.shields.io/nuget/dt/SteadyFlow.Resilience)](https://www.nuget.org/packages/SteadyFlow.Resilience) 
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![GitHub Repo stars](https://img.shields.io/github/stars/AndrewClements84/SteadyFlow.Resilience?style=flat&color=2bbc8a)

---

## ✨ Features

- **Retry Policy** – automatic retries with configurable backoff (Exponential, Linear, Fibonacci, Jitter)  
- **Circuit Breaker** – prevent cascading failures and enable recovery  
- **Rate Limiting**  
  - Token Bucket algorithm with async wait support  
  - Sliding Window algorithm for API-style quotas  
- **Batch Processing** – collect and flush batches by size or interval  
- **ASP.NET Core Middleware** – plug directly into your web pipeline with `UseResiliencePipeline()`  
- **Metrics & Observability Hooks** – `IMetricsObserver` / `LoggingMetricsObserver` for complete visibility  
- **Fluent Chaining** – build pipelines with `.WithRetryAsync().WithCircuitBreakerAsync().WithSlidingWindowAsync().WithTokenBucketAsync()`  
- **Async-first** – designed for modern .NET apps  
- **Lightweight** – zero external dependencies  
- **100% Test Coverage** – verified via xUnit + Codecov  

---

## 📦 Installation

```bash
dotnet add package SteadyFlow.Resilience
```

---

## 🚀 Usage Examples

### 🔁 Retry with Custom Backoff

```csharp
using SteadyFlow.Resilience.Retry;
using SteadyFlow.Resilience.Backoff;

var strategy = new JitterBackoffStrategy();
var retry = new RetryPolicy(maxRetries: 5, initialDelayMs: 200, strategy: strategy);

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
await pipeline();
```

---

### 📊 Rate Limiting

#### Token Bucket

```csharp
using SteadyFlow.Resilience.RateLimiting;

var limiter = new TokenBucketRateLimiter(capacity: 5, refillRatePerSecond: 2);
for (int i = 0; i < 10; i++)
{
    await limiter.WaitForAvailabilityAsync();
    Console.WriteLine($"Request {i} at {DateTime.Now:HH:mm:ss.fff}");
}
```

#### Sliding Window

```csharp
var limiter = new SlidingWindowRateLimiter(maxRequests: 3, window: TimeSpan.FromSeconds(10));
for (int i = 0; i < 6; i++)
{
    await limiter.WaitForAvailabilityAsync();
    Console.WriteLine($"Request {i} at {DateTime.UtcNow:HH:mm:ss.fff}");
}
```

---

### 🔗 Fluent Integration Example

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

### 📈 Metrics & Observability

```csharp
using Microsoft.Extensions.Logging;
using SteadyFlow.Resilience.Metrics;

ILoggerFactory factory = LoggerFactory.Create(b => b.AddConsole());
var observer = new LoggingMetricsObserver(factory.CreateLogger("resilience"));

observer.OnRetry(1, new Exception("Transient error"));
observer.OnCircuitOpened();
observer.OnRateLimited("TokenBucket");
```

---

### 🌐 ASP.NET Core Middleware

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseResiliencePipeline(options =>
    {
        options.Retry = new RetryPolicy(maxRetries: 3);
        options.CircuitBreaker = new CircuitBreakerPolicy(failureThreshold: 5, openDuration: TimeSpan.FromSeconds(30));
        options.SlidingWindowLimiter = new SlidingWindowRateLimiter(maxRequests: 100, window: TimeSpan.FromMinutes(1));
    });

    app.UseRouting();
    app.UseEndpoints(endpoints => endpoints.MapControllers());
}
```

---

## 🧪 Tests

- ✅ Unit tests for all policies, rate limiters, and middleware  
- ✅ Integration and metrics tests with `IMetricsObserver`  
- ✅ 100% code coverage via GitHub Actions + Codecov  

Run locally:

```bash
dotnet test
```

---

## 📈 Roadmap

- [x] Circuit Breaker policy  
- [x] Sliding Window rate limiter  
- [x] ASP.NET middleware integration  
- [x] Metrics & observability hooks  
- [x] Configurable backoff strategies (Jitter, Linear, Fibonacci)  
- [ ] Advanced policy composition (PolicyRegistry / named pipelines)  
- [ ] Built-in Prometheus metrics adapter  

---

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

1. Fork the repo  
2. Create a feature branch  
3. Add tests & code  
4. Open a PR 🎉  

---

## ⭐ Support

If you find **SteadyFlow.Resilience** useful, please give it a ⭐ on GitHub — it helps others discover the project!

---

## 📄 License

Licensed under the **MIT License** – see [LICENSE](LICENSE) for details.
