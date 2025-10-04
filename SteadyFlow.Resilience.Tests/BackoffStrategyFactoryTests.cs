using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class BackoffStrategyFactoryTests
    {
        [Theory]
        [InlineData(typeof(ExponentialBackoffStrategy))]
        [InlineData(typeof(LinearBackoffStrategy))]
        [InlineData(typeof(FibonacciBackoffStrategy))]
        [InlineData(typeof(JitterBackoffStrategy))]
        public void All_Strategies_Should_Return_Positive_Delay(Type strategyType)
        {
            var strategy = (IBackoffStrategy)Activator.CreateInstance(strategyType);
            var delay = strategy.GetDelay(3, TimeSpan.FromMilliseconds(100));
            Assert.True(delay.TotalMilliseconds > 0);
        }
    }
}
