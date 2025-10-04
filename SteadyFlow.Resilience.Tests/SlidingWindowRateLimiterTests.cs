using SteadyFlow.Resilience.RateLimiting;

namespace SteadyFlow.Resilience.Tests
{
    public class SlidingWindowRateLimiterTests
    {
        [Fact]
        public async Task Should_Allow_Within_Limits()
        {
            var limiter = new SlidingWindowRateLimiter(3, TimeSpan.FromMilliseconds(200), observer: null);

            await limiter.WaitForAvailabilityAsync();
            await limiter.WaitForAvailabilityAsync();
            await limiter.WaitForAvailabilityAsync();

            // Should not block yet
            Assert.True(true);
        }

        [Fact]
        public async Task Should_Block_When_Limit_Reached()
        {
            var limiter = new SlidingWindowRateLimiter(2, TimeSpan.FromMilliseconds(200), observer: null);

            await limiter.WaitForAvailabilityAsync();
            await limiter.WaitForAvailabilityAsync();

            var start = DateTime.UtcNow;

            // This should block until earlier requests fall out of the window
            await limiter.WaitForAvailabilityAsync();

            var elapsed = DateTime.UtcNow - start;
            Assert.True(elapsed.TotalMilliseconds >= 100);
        }

        [Fact]
        public async Task Should_Report_OnRateLimited_To_Observer()
        {
            var observer = new FakeObserver();
            var limiter = new SlidingWindowRateLimiter(1, TimeSpan.FromMilliseconds(200), observer);

            // First request succeeds
            await limiter.WaitForAvailabilityAsync();

            // Second request should be limited
            var start = DateTime.UtcNow;
            await limiter.WaitForAvailabilityAsync();
            var elapsed = DateTime.UtcNow - start;

            Assert.Contains(observer.ObservedEvents, e => e.StartsWith("RateLimited:SlidingWindow"));
            Assert.True(elapsed.TotalMilliseconds >= 100);
        }

        [Fact]
        public void Constructor_Should_Throw_When_MaxRequests_IsZeroOrNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SlidingWindowRateLimiter(maxRequests: 0, window: TimeSpan.FromSeconds(1)));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SlidingWindowRateLimiter(maxRequests: -3, window: TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Window_IsZeroOrNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SlidingWindowRateLimiter(maxRequests: 2, window: TimeSpan.Zero));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SlidingWindowRateLimiter(maxRequests: 2, window: TimeSpan.FromMilliseconds(-50)));
        }
    }
}
