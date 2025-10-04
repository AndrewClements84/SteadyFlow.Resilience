using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class FibonacciBackoffStrategyTests
    {
        [Fact]
        public void GetDelay_Should_Follow_Fibonacci_Pattern()
        {
            var strategy = new FibonacciBackoffStrategy();
            var baseDelay = TimeSpan.FromMilliseconds(100);

            var d1 = strategy.GetDelay(1, baseDelay); // fib(1)=1
            var d2 = strategy.GetDelay(2, baseDelay); // fib(2)=1
            var d3 = strategy.GetDelay(3, baseDelay); // fib(3)=2
            var d4 = strategy.GetDelay(4, baseDelay); // fib(4)=3

            Assert.Equal(100, d1.TotalMilliseconds);
            Assert.Equal(100, d2.TotalMilliseconds);
            Assert.Equal(200, d3.TotalMilliseconds);
            Assert.Equal(300, d4.TotalMilliseconds);
        }

        [Fact]
        public void GetDelay_Should_Handle_Large_Attempt()
        {
            var strategy = new FibonacciBackoffStrategy();
            var delay = strategy.GetDelay(6, TimeSpan.FromMilliseconds(50)); // fib(6)=8
            Assert.Equal(400, delay.TotalMilliseconds);
        }
    }
}

