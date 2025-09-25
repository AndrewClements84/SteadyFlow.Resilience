using SteadyFlow.Resilience.RateLimiting;

namespace SteadyFlow.Resilience.Tests
{
    public class TokenBucketRateLimiterTests
    {
        [Fact]
        public void Should_Consume_Token_When_Available()
        {
            var limiter = new TokenBucketRateLimiter(capacity: 2, refillRatePerSecond: 1);

            Assert.True(limiter.TryConsume());
            Assert.True(limiter.TryConsume());
            Assert.False(limiter.TryConsume());
        }

        [Fact]
        public async Task Should_Refill_Tokens_Over_Time()
        {
            var limiter = new TokenBucketRateLimiter(capacity: 1, refillRatePerSecond: 1);

            Assert.True(limiter.TryConsume());
            Assert.False(limiter.TryConsume());

            await Task.Delay(1100);
            Assert.True(limiter.TryConsume());
        }
    }
}
