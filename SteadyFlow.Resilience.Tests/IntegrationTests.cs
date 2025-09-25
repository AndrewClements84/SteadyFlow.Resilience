using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task FullIntegrationTest_RetryRateLimitBatching_Reliable()
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
    }
}
