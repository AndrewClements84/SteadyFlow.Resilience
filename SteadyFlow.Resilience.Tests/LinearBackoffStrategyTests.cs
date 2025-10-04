using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class LinearBackoffStrategyTests
    {
        [Fact]
        public void GetDelay_Should_Increase_Linearly()
        {
            var strategy = new LinearBackoffStrategy();
            var baseDelay = TimeSpan.FromMilliseconds(100);

            Assert.Equal(100, strategy.GetDelay(1, baseDelay).TotalMilliseconds);
            Assert.Equal(200, strategy.GetDelay(2, baseDelay).TotalMilliseconds);
            Assert.Equal(300, strategy.GetDelay(3, baseDelay).TotalMilliseconds);
        }

        [Fact]
        public void GetDelay_Should_Return_Zero_For_Zero_Attempt()
        {
            var strategy = new LinearBackoffStrategy();
            var delay = strategy.GetDelay(0, TimeSpan.FromMilliseconds(100));
            Assert.Equal(0, delay.TotalMilliseconds);
        }
    }
}

