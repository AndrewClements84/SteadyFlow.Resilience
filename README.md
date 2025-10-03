# SteadyFlow.Resilience

✨ Lightweight resilience toolkit for .NET  
Retry policies · Rate limiting · Batch processing  

[![.NET Build, Test & Publish](https://github.com/AndrewClements84/SteadyFlow.Resilience/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/AndrewClements84/SteadyFlow.Resilience/actions/workflows/dotnet.yml) 
[![NuGet Version](https://img.shields.io/nuget/v/SteadyFlow.Resilience.svg?logo=nuget)](https://www.nuget.org/packages/SteadyFlow.Resilience) 
[![NuGet Downloads](https://img.shields.io/nuget/dt/SteadyFlow.Resilience)](https://www.nuget.org/packages/SteadyFlow.Resilience) 
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![GitHub Repo stars](https://img.shields.io/github/stars/AndrewClements84/SteadyFlow.Resilience?style=flat&color=2bbc8a)

---

## ✨ Features

- **Retry Policy** – automatic retries with exponential backoff  
- **Rate Limiting** – token bucket algorithm with async wait support  
- **Batch Processing** – collect items into timed or size-based batches  
- **Async-first** – designed for modern .NET apps  
- **Lightweight** – no external dependencies  

---

## 📦 Installation

Once published to NuGet:

```bash
dotnet add package SteadyFlow.Resilience
```

---

## 🚀 Usage Examples

### 🔁 Retry with Exponential Backoff

```csharp
using SteadyFlow.Retry;

var policy = new RetryPolicy(maxRetries: 5, initialDelayMs: 200);

var result = await policy.ExecuteAsync(async () =>
{
    // Simulate an unreliable call
    if (new Random().Next(2) == 0)
        throw new Exception("Transient failure");

    return "Success!";
});

Console.WriteLine(result);
```

---

### ⏳ Rate Limiting with Token Bucket

```csharp
using SteadyFlow.RateLimiting;

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
using SteadyFlow.Policies;

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
    await Task.Delay(500); // simulate incoming data
}
```

---

### 🔗 Integration Example (Retry + Rate Limit + Batch)

```csharp
using SteadyFlow.Retry;
using SteadyFlow.RateLimiting;
using SteadyFlow.Policies;

var limiter = new TokenBucketRateLimiter(capacity: 3, refillRatePerSecond: 2);
var retry = new RetryPolicy(maxRetries: 3, initialDelayMs: 100);

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
    var index = i;
    await retry.ExecuteAsync(async () =>
    {
        await limiter.WaitForAvailabilityAsync();

        // Fail first time for even numbers, succeed on retry
        if (index % 2 == 0 && new Random().Next(2) == 0)
            throw new Exception("Simulated transient failure");

        batcher.Add(index);
    });
}
```

---

## 🧪 Tests

The project includes a full **xUnit test suite**:

- ✅ Unit tests for RetryPolicy, TokenBucketRateLimiter, and BatchProcessor  
- ✅ Integration tests combining all features  
- ✅ CI/CD workflow runs all tests on every commit (via GitHub Actions)  

Run locally:

```bash
dotnet test
```

---

## 📈 Roadmap

- [ ] Add Circuit Breaker policy  
- [ ] Support sliding window rate limiter  
- [ ] ASP.NET middleware integration  
- [ ] Metrics & observability hooks  
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

If you find **SteadyFlow.Resilience** useful, please consider giving it a star on
GitHub --- it helps others discover the project and shows your support.

---

## 📄 License

Licensed under the **MIT License** – see [LICENSE](LICENSE) for details.

