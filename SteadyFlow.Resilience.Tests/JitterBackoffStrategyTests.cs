using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class JitterBackoffStrategyTests
    {
        [Fact]
        public void GetDelay_Should_Include_Random_Jitter()
        {
            var strategy = new JitterBackoffStrategy();
            var baseDelay = TimeSpan.FromMilliseconds(100);

            var delay = strategy.GetDelay(2, baseDelay);
            Assert.InRange(delay.TotalMilliseconds, 200, 400); // base*2 + random(0..100)
        }

        [Fact]
        public void GetDelay_Should_Always_Be_Positive()
        {
            var strategy = new JitterBackoffStrategy();
            var delay = strategy.GetDelay(1, TimeSpan.FromMilliseconds(100));
            Assert.True(delay.TotalMilliseconds > 0);
        }
    }
}

