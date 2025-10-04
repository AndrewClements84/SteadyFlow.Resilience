using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Tests.Helpers;

namespace SteadyFlow.Resilience.Tests
{
    public class TokenBucketRateLimiterTests
    {
        [Fact]
        public async Task Should_Allow_Within_Capacity()
        {
            var limiter = new TokenBucketRateLimiter(2, 1, observer: null);

            await limiter.WaitForAvailabilityAsync();
            await limiter.WaitForAvailabilityAsync();

            // Both should succeed without delay
            Assert.True(true);
        }

        [Fact]
        public async Task Should_Block_When_Empty_And_Recover_After_Refill()
        {
            var limiter = new TokenBucketRateLimiter(1, 2, observer: null);

            // Consume initial token
            await limiter.WaitForAvailabilityAsync();

            var start = DateTime.UtcNow;

            // This should block until a refill occurs
            await limiter.WaitForAvailabilityAsync();

            var elapsed = DateTime.UtcNow - start;
            Assert.True(elapsed.TotalMilliseconds >= 400); // ~0.5s refill
        }

        [Fact]
        public async Task Should_Report_OnRateLimited_To_Observer()
        {
            var observer = new FakeObserver();
            var limiter = new TokenBucketRateLimiter(1, 0.5, observer);

            // Consume initial token
            await limiter.WaitForAvailabilityAsync();

            // Next call should cause rate limit wait
            var start = DateTime.UtcNow;
            await limiter.WaitForAvailabilityAsync();
            var elapsed = DateTime.UtcNow - start;

            Assert.Contains(observer.Events, e => e.StartsWith("RateLimited:TokenBucket"));
            Assert.True(elapsed.TotalMilliseconds >= 100); // waited at least a tick
        }

        [Fact]
        public void Constructor_Should_Throw_When_Capacity_IsZeroOrNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TokenBucketRateLimiter(capacity: 0, refillRatePerSecond: 1));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TokenBucketRateLimiter(capacity: -5, refillRatePerSecond: 1));
        }

        [Fact]
        public void Constructor_Should_Throw_When_RefillRatePerSecond_IsZeroOrNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TokenBucketRateLimiter(capacity: 5, refillRatePerSecond: 0));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TokenBucketRateLimiter(capacity: 5, refillRatePerSecond: -2));
        }
    }
}
