using SteadyFlow.Resilience.RateLimiting;

namespace SteadyFlow.Resilience.Tests
{
    public  class SlidingWindowRateLimiterTests
    {
        [Fact]
        public async Task Allows_Requests_Under_Limit()
        {
            var limiter = new SlidingWindowRateLimiter(maxRequests: 3, window: TimeSpan.FromSeconds(1));
            var allowed = 0;

            for (int i = 0; i < 3; i++)
            {
                await limiter.WaitForAvailabilityAsync();
                allowed++;
            }

            Assert.Equal(3, allowed);
        }

        [Fact]
        public async Task Blocks_When_Over_Limit()
        {
            var limiter = new SlidingWindowRateLimiter(maxRequests: 2, window: TimeSpan.FromMilliseconds(500));

            // First two should succeed immediately
            await limiter.WaitForAvailabilityAsync();
            await limiter.WaitForAvailabilityAsync();

            var start = DateTime.UtcNow;

            // Third should be blocked until window slides
            await limiter.WaitForAvailabilityAsync();

            var elapsed = DateTime.UtcNow - start;
            Assert.True(elapsed >= TimeSpan.FromMilliseconds(400));
        }

        [Fact]
        public async Task Allows_Again_After_Window_Passes()
        {
            var limiter = new SlidingWindowRateLimiter(maxRequests: 2, window: TimeSpan.FromMilliseconds(200));

            await limiter.WaitForAvailabilityAsync();
            await limiter.WaitForAvailabilityAsync();

            // This will block until old timestamps expire
            await Task.Delay(250);

            var start = DateTime.UtcNow;
            await limiter.WaitForAvailabilityAsync();
            var elapsed = DateTime.UtcNow - start;

            Assert.True(elapsed < TimeSpan.FromMilliseconds(50)); // should not block
        }

        [Fact]
        public void Constructor_ShouldThrow_On_Invalid_MaxRequests()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SlidingWindowRateLimiter(maxRequests: 0, window: TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public void Constructor_ShouldThrow_On_Invalid_Window()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SlidingWindowRateLimiter(maxRequests: 1, window: TimeSpan.Zero));
        }
    }
}
