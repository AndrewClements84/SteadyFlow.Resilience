# SteadyFlow.Resilience

✨ Lightweight resilience toolkit for .NET  
Retry policies · Circuit breaker · Rate limiting · Batch processing  

[![.NET Build, Test & Publish](https://github.com/AndrewClements84/SteadyFlow.Resilience/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/AndrewClements84/SteadyFlow.Resilience/actions/workflows/dotnet.yml) 
[![codecov](https://codecov.io/gh/AndrewClements84/SteadyFlow.Resilience/branch/master/graph/badge.svg)](https://codecov.io/gh/AndrewClements84/SteadyFlow.Resilience)
[![NuGet Version](https://img.shields.io/nuget/v/SteadyFlow.Resilience.svg?logo=nuget)](https://www.nuget.org/packages/SteadyFlow.Resilience) 
[![NuGet Downloads](https://img.shields.io/nuget/dt/SteadyFlow.Resilience)](https://www.nuget.org/packages/SteadyFlow.Resilience) 
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![GitHub Repo stars](https://img.shields.io/github/stars/AndrewClements84/SteadyFlow.Resilience?style=flat&color=2bbc8a)

---

## ✨ Features

- **Retry Policy** – automatic retries with exponential backoff  
- **Circuit Breaker** – prevent cascading failures with open/half-open states  
- **Rate Limiting** – token bucket algorithm with async wait support  
- **Batch Processing** – collect items into timed or size-based batches  
- **Async-first** – designed for modern .NET apps  
- **Lightweight** – no external dependencies  

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

var policy = new RetryPolicy(maxRetries: 5, initialDelayMs: 200);

var result = await policy.ExecuteAsync(async () =>
{
    if (new Random().Next(2) == 0)
        throw new Exception("Transient failure");

    return "Success!";
});
```

---

### ⛔ Circuit Breaker

```csharp
using SteadyFlow.Resilience.Policies;

var breaker = new CircuitBreakerPolicy(failureThreshold: 3, openDuration: TimeSpan.FromSeconds(10));

try
{
    await breaker.ExecuteAsync(async () =>
    {
        // risky call
        throw new Exception("Failure");
    });
}
catch (CircuitBreakerOpenException)
{
    Console.WriteLine("Circuit is open, fast-failing!");
}
```

You can also use the extension methods for a fluent style:

```csharp
Func<Task> action = async () => { /* risky work */ await Task.CompletedTask; };
await action.WithCircuitBreakerAsync(breaker);
```

---

### ⏳ Rate Limiting with Token Bucket

```csharp
using SteadyFlow.Resilience.RateLimiting;

var limiter = new TokenBucketRateLimiter(capacity: 5, refillRatePerSecond: 2);

for (int i = 0; i < 10; i++)
{
    await limiter.WaitForAvailabilityAsync();
    Console.WriteLine($"Request {i} sent at {DateTime.Now:HH:mm:ss.fff}");
}
```

---

### 📦 Batch Processing

```csharp
using SteadyFlow.Resilience.Policies;

var batcher = new BatchProcessor<int>(
    batchSize: 3,
    interval: TimeSpan.FromSeconds(5),
    async batch =>
    {
        Console.WriteLine($"Processing batch: {string.Join(", ", batch)}");
        await Task.CompletedTask;
    });

for (int i = 0; i < 10; i++)
{
    batcher.Add(i);
    await Task.Delay(500);
}
```

---

### 🔗 Integration Example (Retry + Circuit Breaker + Rate Limit + Batch)

```csharp
using SteadyFlow.Resilience.Extensions;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;

var retry = new RetryPolicy(maxRetries: 3, initialDelayMs: 100);
var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromSeconds(5));
var limiter = new TokenBucketRateLimiter(capacity: 2, refillRatePerSecond: 1);

var batcher = new BatchProcessor<int>(
    batchSize: 2,
    interval: TimeSpan.FromSeconds(2),
    async batch =>
    {
        Console.WriteLine($"Processed batch: {string.Join(", ", batch)}");
        await Task.CompletedTask;
    });

for (int i = 1; i <= 5; i++)
{
    int value = i;

    Func<Task> action = async () =>
    {
        await limiter.WaitForAvailabilityAsync();

        if (value % 2 == 0 && new Random().Next(2) == 0)
            throw new Exception("Simulated transient failure");

        batcher.Add(value);
    };

    // Retry wrapped inside Circuit Breaker
    await (() => action.WithRetryAsync(retry)).WithCircuitBreakerAsync(breaker);
}
```

---

## 🧪 Tests

- ✅ Unit tests for RetryPolicy, CircuitBreakerPolicy, TokenBucketRateLimiter, and BatchProcessor  
- ✅ TaskExtensions tested for Retry and CircuitBreaker  
- ✅ Integration tests combining multiple resilience patterns  
- ✅ 100% code coverage (enforced via Codecov + CI)  

Run locally:

```bash
dotnet test
```

---

## 📈 Roadmap

- [x] Add Retry policy  
- [x] Add Rate Limiting (Token Bucket)  
- [x] Add Batch Processing  
- [x] Add Circuit Breaker policy  
- [ ] Support sliding window rate limiter  
- [ ] ASP.NET middleware integration  
- [ ] Metrics & observability hooks  
- [ ] Configurable backoff strategies (jitter, linear, Fibonacci)  

---

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md).

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
