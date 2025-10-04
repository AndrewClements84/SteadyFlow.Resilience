using SteadyFlow.Resilience.Extensions;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task Integration_Retry_RateLimit_Batching_Baseline()
        {
            var observer = new FakeObserver();
            var limiter = new TokenBucketRateLimiter(capacity: 3, refillRatePerSecond: 2, observer: observer);
            var retry = new RetryPolicy(maxRetries: 3, initialDelayMs: 50, observer: observer);
            var processedItems = new List<int>();

            var batcher = new BatchProcessor<int>(
                batchSize: 2,
                interval: TimeSpan.FromMilliseconds(200),
                async batch =>
                {
                    processedItems.AddRange(batch);
                    await Task.CompletedTask;
                },
                observer: observer);

            var results = new List<string>();
            var tasks = new List<Task>();
            var attemptMap = new Dictionary<int, int>();

            for (int i = 1; i <= 5; i++)
            {
                var index = i;
                attemptMap[index] = 0;

                tasks.Add(Task.Run(async () =>
                {
                    Func<Task> action = async () =>
                    {
                        await limiter.WaitForAvailabilityAsync();
                        attemptMap[index]++;

                        if (index % 2 == 0 && attemptMap[index] == 1)
                            throw new Exception("Simulated transient failure");

                        batcher.Add(index);
                        results.Add($"Processed {index}");
                    };

                    var pipeline = action.WithRetryAsync(retry);
                    await pipeline();
                }));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(300);

            Assert.Equal(5, processedItems.Count);
            foreach (var i in new[] { 1, 2, 3, 4, 5 })
                Assert.Contains(i, processedItems);

            foreach (var r in results)
                Assert.StartsWith("Processed", r);
        }

        [Fact]
        public async Task Integration_Retry_CircuitBreaker_RateLimit_WorkTogether()
        {
            var observer = new FakeObserver();
            var retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 50, observer: observer);
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(500), observer: observer);
            var limiter = new TokenBucketRateLimiter(capacity: 2, refillRatePerSecond: 1, observer: observer);

            var processed = new List<int>();
            var batcher = new BatchProcessor<int>(
                batchSize: 2,
                interval: TimeSpan.FromMilliseconds(200),
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                },
                observer: observer);

            var tasks = new List<Task>();
            var attemptMap = new Dictionary<int, int>();

            for (int i = 1; i <= 4; i++)
            {
                int value = i;
                attemptMap[value] = 0;

                tasks.Add(Task.Run(async () =>
                {
                    Func<Task> action = async () =>
                    {
                        await limiter.WaitForAvailabilityAsync();
                        attemptMap[value]++;

                        if (value % 2 == 0 && attemptMap[value] == 1)
                            throw new Exception("Simulated transient failure");

                        batcher.Add(value);
                    };

                    var pipeline = action
                        .WithRetryAsync(retry)
                        .WithCircuitBreakerAsync(breaker);

                    await pipeline();
                }));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(400);

            Assert.Equal(4, processed.Count);
            foreach (var v in new[] { 1, 2, 3, 4 })
                Assert.Contains(v, processed);

            Assert.Equal(CircuitState.Closed, breaker.State);
        }

        [Fact]
        public async Task Integration_CircuitBreaker_Should_Open_On_TooManyFailures()
        {
            var observer = new FakeObserver();
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(200), observer: observer);
            int attempts = 0;

            Func<Task> alwaysFailingAction = () =>
            {
                attempts++;
                throw new Exception("Always fails");
            };

            var pipeline = alwaysFailingAction.WithCircuitBreakerAsync(breaker);

            await Assert.ThrowsAsync<Exception>(async () => await pipeline());
            await Assert.ThrowsAsync<Exception>(async () => await pipeline());
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () => await pipeline());

            Assert.Equal(CircuitState.Open, breaker.State);
            Assert.Equal(2, attempts);
        }

        [Fact]
        public async Task Integration_SlidingWindow_Retry_CircuitBreaker()
        {
            var observer = new FakeObserver();
            var limiter = new SlidingWindowRateLimiter(maxRequests: 2, window: TimeSpan.FromMilliseconds(300), observer: observer);
            var retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 50, observer: observer);
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(500), observer: observer);

            var processed = new List<int>();
            var attemptMap = new Dictionary<int, int>();
            var tasks = new List<Task>();

            for (int i = 1; i <= 4; i++)
            {
                int value = i;
                attemptMap[value] = 0;

                tasks.Add(Task.Run(async () =>
                {
                    Func<Task> action = async () =>
                    {
                        await limiter.WaitForAvailabilityAsync();
                        attemptMap[value]++;

                        if (value % 2 == 0 && attemptMap[value] == 1)
                            throw new Exception("Simulated transient failure");

                        processed.Add(value);
                    };

                    var pipeline = action
                        .WithRetryAsync(retry)
                        .WithCircuitBreakerAsync(breaker);

                    await pipeline();
                }));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(4, processed.Count);
            foreach (var v in new[] { 1, 2, 3, 4 })
                Assert.Contains(v, processed);

            Assert.Equal(CircuitState.Closed, breaker.State);
        }

        [Fact]
        public async Task Integration_Retry_Should_Report_Observer_Events()
        {
            var observer = new FakeObserver();
            var retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, observer: observer);
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(200), observer);
            var limiter = new TokenBucketRateLimiter(capacity: 1, refillRatePerSecond: 1, observer);

            int attempts = 0;

            Func<Task> alwaysFailing = async () =>
            {
                await limiter.WaitForAvailabilityAsync();
                attempts++;
                throw new Exception("Always fails");
            };

            // Cause enough failures to trip circuit breaker
            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync(() => retry.ExecuteAsync(alwaysFailing)));
            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync(() => retry.ExecuteAsync(alwaysFailing)));

            // The breaker should now be open
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
                breaker.ExecuteAsync(() => retry.ExecuteAsync(alwaysFailing)));

            Assert.Equal(CircuitState.Open, breaker.State);

            Assert.Contains(observer.ObservedEvents, e => e.StartsWith("Retry:"));
            Assert.Contains(observer.ObservedEvents, e => e.Contains("RateLimited:TokenBucket"));
            Assert.Contains(observer.ObservedEvents, e => e.Contains("CircuitOpened"));
        }
    }
}
