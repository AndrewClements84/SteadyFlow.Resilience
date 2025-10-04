using SteadyFlow.Resilience.Metrics;
using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class RetryPolicyTests
    {
        [Fact]
        public async Task ExecuteAsync_Should_Succeed_On_First_Attempt()
        {
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 10);
            int attempt = 0;

            var result = await policy.ExecuteAsync(async () =>
            {
                attempt++;
                return "OK";
            });

            Assert.Equal(1, attempt);
            Assert.Equal("OK", result);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Throw_After_MaxRetries()
        {
            int attempt = 0;
            var policy = new RetryPolicy(maxRetries: 2, initialDelayMs: 10);

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    attempt++;
                    throw new Exception("Always fail");
                });
            });

            Assert.Equal(2, attempt); // total = maxRetries
        }

        [Fact]
        public async Task ExecuteAsync_Should_Honor_Custom_Backoff_Strategy()
        {
            var mockStrategy = new MockBackoffStrategy();
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 10, strategy: mockStrategy);

            int attempts = 0;
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    attempts++;
                    throw new Exception("fail");
                });
            });

            Assert.Equal(3, attempts);
            Assert.Equal(2, mockStrategy.CallCount); // 2 backoffs (between 3 attempts)
        }

        [Fact]
        public async Task ExecuteAsync_Should_Use_Fibonacci_Backoff_When_Configured()
        {
            var strategy = new FibonacciBackoffStrategy();
            var policy = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, strategy: strategy);

            int attempts = 0;
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    attempts++;
                    throw new Exception("fail");
                });
            });

            Assert.Equal(2, attempts);
        }

        [Fact]
        public void ExecuteAsync_Should_Stop_When_MaxRetriesZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RetryPolicy(maxRetries: 0, initialDelayMs: 10));
        }

        [Fact]
        public async Task ExecuteAsync_Should_Throw_When_Action_IsNull()
        {
            var policy = new RetryPolicy();
            await Assert.ThrowsAsync<ArgumentNullException>(() => policy.ExecuteAsync((Func<Task>)null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => policy.ExecuteAsync<string>(null));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Invalid_Delay()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RetryPolicy(maxRetries: 3, initialDelayMs: 0));
        }

        [Fact]
        public async Task ExecuteAsync_Should_Notify_Observer_On_RetryAttempts()
        {
            var observer = new FakeObserver();
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 5, observer: observer);

            int attempt = 0;
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    attempt++;
                    throw new Exception($"Fail {attempt}");
                });
            });

            Assert.Equal(3, attempt); // total attempts = maxRetries
            var retryEvents = observer.ObservedEvents.FindAll(e => e.StartsWith("Retry:"));
            Assert.Equal(attempt, retryEvents.Count); // expect 3
        }


        // -------------------------------
        // Helpers & Fakes
        // -------------------------------

        private class MockBackoffStrategy : IBackoffStrategy
        {
            public int CallCount { get; private set; }
            public TimeSpan GetDelay(int attempt, TimeSpan baseDelay)
            {
                CallCount++;
                return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * (attempt + 1));
            }
        }

        private class FakeObserver : IMetricsObserver
        {
            public List<string> ObservedEvents { get; } = new List<string>();

            public void OnRetry(int attempt, Exception ex)
                => ObservedEvents.Add($"Retry:{attempt}:{ex.Message}");

            public void OnCircuitOpened() => ObservedEvents.Add("CircuitOpened");
            public void OnCircuitClosed() => ObservedEvents.Add("CircuitClosed");
            public void OnCircuitHalfOpen() => ObservedEvents.Add("CircuitHalfOpen");

            public void OnRateLimited(string limiterType) => ObservedEvents.Add($"RateLimited:{limiterType}");
            public void OnBatchProcessed(int itemCount) => ObservedEvents.Add($"BatchProcessed:{itemCount}");
            public void OnEvent(string policyName, string message) => ObservedEvents.Add($"{policyName}:{message}");
        }
    }
}
