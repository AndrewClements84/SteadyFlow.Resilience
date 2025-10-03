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
            // Arrange
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

            var results = new List<string>();
            var tasks = new List<Task>();

            // Track attempts per item to simulate transient failures
            var attemptMap = new Dictionary<int, int>();

            // Act
            for (int i = 1; i <= 5; i++)
            {
                var index = i;
                attemptMap[index] = 0;

                tasks.Add(retry.ExecuteAsync(async () =>
                {
                    await limiter.WaitForAvailabilityAsync();

                    attemptMap[index]++;
                    // Fail only once for even numbers to simulate transient failure
                    if (index % 2 == 0 && attemptMap[index] == 1)
                        throw new Exception("Simulated transient failure");

                    batcher.Add(index);
                    results.Add($"Processed {index}");
                }));
            }

            await Task.WhenAll(tasks);

            // Wait for batcher to flush
            await Task.Delay(300);

            // Assert
            Assert.Equal(5, processedItems.Count); // all items should be processed
            foreach (var i in new[] { 1, 2, 3, 4, 5 })
            {
                Assert.Contains(i, processedItems);
            }

            foreach (var r in results)
            {
                Assert.StartsWith("Processed", r);
            }
        }

        [Fact]
        public async Task Integration_Retry_CircuitBreaker_RateLimit_WorkTogether()
        {
            var retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 50);
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(500));
            var limiter = new TokenBucketRateLimiter(capacity: 2, refillRatePerSecond: 1);

            var processed = new List<int>();
            var batcher = new BatchProcessor<int>(
                batchSize: 2,
                interval: TimeSpan.FromMilliseconds(200),
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                });

            var tasks = new List<Task>();
            var attemptMap = new Dictionary<int, int>();

            for (int i = 1; i <= 4; i++)
            {
                int value = i;
                attemptMap[value] = 0;

                tasks.Add(Task.Run(async () =>
                {
                    await limiter.WaitForAvailabilityAsync();

                    // core action
                    Func<Task> coreAction = async () =>
                    {
                        attemptMap[value]++;
                        if (value % 2 == 0 && attemptMap[value] == 1)
                            throw new Exception("Simulated transient failure");

                        batcher.Add(value);
                        await Task.CompletedTask;
                    };

                    // Wrap retry first, then breaker
                    Func<Task> resilientAction = () => coreAction.WithRetryAsync(retry);
                    await resilientAction.WithCircuitBreakerAsync(breaker);
                }));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(400); // let batcher flush

            Assert.Equal(4, processed.Count);
            foreach (var v in new[] { 1, 2, 3, 4 })
                Assert.Contains(v, processed);

            Assert.Equal(CircuitState.Closed, breaker.State);
        }

        [Fact]
        public async Task Integration_CircuitBreaker_Should_Open_On_TooManyFailures()
        {
            // Arrange
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(200));
            int attempts = 0;

            Func<Task> alwaysFailingAction = () =>
            {
                attempts++;
                throw new Exception("Always fails");
            };

            // Act: Trip the breaker directly (not via retry)
            await Assert.ThrowsAsync<Exception>(() => alwaysFailingAction.WithCircuitBreakerAsync(breaker));
            await Assert.ThrowsAsync<Exception>(() => alwaysFailingAction.WithCircuitBreakerAsync(breaker));

            // Now breaker should be open
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => alwaysFailingAction.WithCircuitBreakerAsync(breaker));

            // Assert
            Assert.Equal(CircuitState.Open, breaker.State);
            Assert.Equal(2, attempts); // breaker tripped after 2 failures
        }
    }
}
