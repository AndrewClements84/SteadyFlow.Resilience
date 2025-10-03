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
            var limiter = new TokenBucketRateLimiter(capacity: 3, refillRatePerSecond: 2);
            var retry = new RetryPolicy(maxRetries: 3, initialDelayMs: 50);
            var processedItems = new List<int>();

            var batcher = new BatchProcessor<int>(
                batchSize: 2,
                interval: TimeSpan.FromMilliseconds(200),
                async batch =>
                {
                    processedItems.AddRange(batch);
                    await Task.CompletedTask;
                });

            var attemptMap = new Dictionary<int, int>();
            var tasks = new List<Task>();

            for (int i = 1; i <= 5; i++)
            {
                int index = i;
                attemptMap[index] = 0;

                Func<Task> action = async () =>
                {
                    await limiter.WaitForAvailabilityAsync();
                    attemptMap[index]++;
                    if (index % 2 == 0 && attemptMap[index] == 1)
                        throw new Exception("Transient failure");
                    batcher.Add(index);
                };

                var pipeline = action.WithRetryAsync(retry);
                tasks.Add(pipeline());
            }

            await Task.WhenAll(tasks);
            await Task.Delay(300);

            Assert.Equal(5, processedItems.Count);
        }

        [Fact]
        public async Task Integration_CircuitBreaker_Should_Open_On_TooManyFailures()
        {
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(200));
            int attempts = 0;

            Func<Task> alwaysFailingAction = () =>
            {
                attempts++;
                throw new Exception("Always fails");
            };

            var pipeline = alwaysFailingAction.WithCircuitBreakerAsync(breaker);

            await Assert.ThrowsAsync<Exception>(() => pipeline());
            await Assert.ThrowsAsync<Exception>(() => pipeline());
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => pipeline());

            Assert.Equal(CircuitState.Open, breaker.State);
            Assert.Equal(2, attempts);
        }

        [Fact]
        public async Task Integration_TokenBucket_Retry_CircuitBreaker_Fluent()
        {
            var limiter = new TokenBucketRateLimiter(capacity: 2, refillRatePerSecond: 1);
            var retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 50);
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(500));

            var processed = new List<int>();
            var attemptMap = new Dictionary<int, int>();
            var tasks = new List<Task>();

            for (int i = 1; i <= 4; i++)
            {
                int value = i;
                attemptMap[value] = 0;

                Func<Task> action = async () =>
                {
                    attemptMap[value]++;

                    // Fail once for even numbers
                    if (value % 2 == 0 && attemptMap[value] == 1)
                        throw new Exception("Simulated transient failure");

                    processed.Add(value);
                    await Task.CompletedTask;
                };

                // Build fluent pipeline
                var pipeline = action
                    .WithTokenBucketAsync(limiter)
                    .WithRetryAsync(retry)
                    .WithCircuitBreakerAsync(breaker);

                tasks.Add(pipeline());
            }

            await Task.WhenAll(tasks);

            // Assert all values processed
            Assert.Equal(4, processed.Count);
            foreach (var v in new[] { 1, 2, 3, 4 })
                Assert.Contains(v, processed);

            // Breaker should still be closed
            Assert.Equal(CircuitState.Closed, breaker.State);
        }

        [Fact]
        public async Task Integration_SlidingWindow_Retry_CircuitBreaker_Fluent()
        {
            var limiter = new SlidingWindowRateLimiter(maxRequests: 2, window: TimeSpan.FromMilliseconds(300));
            var retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 50);
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(500));

            var processed = new List<int>();
            var attemptMap = new Dictionary<int, int>();
            var tasks = new List<Task>();

            for (int i = 1; i <= 4; i++)
            {
                int value = i;
                attemptMap[value] = 0;

                Func<Task> action = async () =>
                {
                    attemptMap[value]++;
                    if (value % 2 == 0 && attemptMap[value] == 1)
                        throw new Exception("Transient failure");
                    processed.Add(value);
                };

                var pipeline = action
                    .WithSlidingWindowAsync(limiter)
                    .WithRetryAsync(retry)
                    .WithCircuitBreakerAsync(breaker);

                tasks.Add(pipeline());
            }

            await Task.WhenAll(tasks);

            Assert.Equal(4, processed.Count);
            Assert.Equal(CircuitState.Closed, breaker.State);
        }
    }
}
