using SteadyFlow.Resilience.Retry;
using SteadyFlow.Resilience.Tests.Helpers;

namespace SteadyFlow.Resilience.Tests
{
    public class RetryPolicyTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldSucceedWithoutRetry()
        {
            var policy = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, observer: null);

            var result = await policy.ExecuteAsync(() => Task.FromResult("ok"));

            Assert.Equal("ok", result);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRetryOnFailure_AndEventuallySucceed()
        {
            var attempts = 0;
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 10, observer: null);

            var result = await policy.ExecuteAsync(() =>
            {
                attempts++;
                if (attempts < 2) throw new Exception("fail");
                return Task.FromResult("success");
            });

            Assert.Equal("success", result);
            Assert.Equal(2, attempts);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldThrowAfterMaxRetries()
        {
            var attempts = 0;
            var policy = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, observer: null);

            await Assert.ThrowsAsync<Exception>(() =>
                policy.ExecuteAsync<string>(() =>
                {
                    attempts++;
                    throw new Exception("fail");
                }));

            Assert.Equal(3, attempts); // initial + 2 retries
        }

        [Fact]
        public async Task ExecuteAsync_Should_Report_RetryAttempts_To_Observer()
        {
            var observer = new FakeObserver();
            var attempts = 0;
            var policy = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, observer: observer);

            await Assert.ThrowsAsync<Exception>(() =>
                policy.ExecuteAsync<string>(() =>
                {
                    attempts++;
                    throw new Exception("fail");
                }));

            Assert.Contains(observer.Events, e => e.StartsWith("RetryAttempt:"));
            Assert.True(attempts > 1);
        }

        [Fact]
        public async Task ExecuteAsync_NonGeneric_ShouldWork()
        {
            var called = false;
            var policy = new RetryPolicy(maxRetries: 1, initialDelayMs: 10, observer: null);

            await policy.ExecuteAsync(() =>
            {
                called = true;
                return Task.CompletedTask;
            });

            Assert.True(called);
        }
    }
}
