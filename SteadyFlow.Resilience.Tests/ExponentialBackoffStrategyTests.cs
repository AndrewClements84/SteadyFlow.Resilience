using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class ExponentialBackoffStrategyTests
    {
        [Fact]
        public void GetDelay_Should_Double_Each_Attempt()
        {
            var strategy = new ExponentialBackoffStrategy();
            var baseDelay = TimeSpan.FromMilliseconds(100);

            var d1 = strategy.GetDelay(1, baseDelay);
            var d2 = strategy.GetDelay(2, baseDelay);
            var d3 = strategy.GetDelay(3, baseDelay);

            Assert.Equal(100, d1.TotalMilliseconds);
            Assert.Equal(200, d2.TotalMilliseconds);
            Assert.Equal(400, d3.TotalMilliseconds);
        }

        [Fact]
        public void GetDelay_Should_Handle_Single_Attempt()
        {
            var strategy = new ExponentialBackoffStrategy();
            var delay = strategy.GetDelay(1, TimeSpan.FromMilliseconds(250));
            Assert.Equal(250, delay.TotalMilliseconds);
        }
    }
}

