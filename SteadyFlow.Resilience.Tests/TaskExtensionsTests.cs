using SteadyFlow.Resilience.Extensions;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class TaskExtensionsTests
    {
        [Fact]
        public async Task WithRetryAsync_FuncTaskT_Success()
        {
            var policy = new RetryPolicy(maxRetries: 2);
            int attempts = 0;

            Func<Task<int>> action = async () =>
            {
                attempts++;
                return 99;
            };

            var pipeline = action.WithRetryAsync(policy);
            var result = await pipeline();

            Assert.Equal(99, result);
            Assert.Equal(1, attempts);
        }

        [Fact]
        public async Task WithRetryAsync_FuncTaskT_FailsThenRetries()
        {
            var policy = new RetryPolicy(maxRetries: 2);
            int attempts = 0;

            Func<Task<int>> action = async () =>
            {
                attempts++;
                if (attempts < 2) throw new Exception("fail");
                return 42;
            };

            var pipeline = action.WithRetryAsync(policy);
            var result = await pipeline();

            Assert.Equal(42, result);
            Assert.Equal(2, attempts);
        }

        [Fact]
        public async Task WithCircuitBreakerAsync_FuncTask_Success()
        {
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(200));
            bool executed = false;

            Func<Task> action = async () =>
            {
                executed = true;
                await Task.CompletedTask;
            };

            var pipeline = action.WithCircuitBreakerAsync(breaker);
            await pipeline();

            Assert.True(executed);
            Assert.Equal(CircuitState.Closed, breaker.State);
        }

        [Fact]
        public async Task WithCircuitBreakerAsync_FuncTask_FailureOpensBreaker()
        {
            var breaker = new CircuitBreakerPolicy(failureThreshold: 1, openDuration: TimeSpan.FromMilliseconds(200));
            int attempts = 0;

            Func<Task> action = () =>
            {
                attempts++;
                throw new Exception("always fails");
            };

            var pipeline = action.WithCircuitBreakerAsync(breaker);

            await Assert.ThrowsAsync<Exception>(() => pipeline());
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => pipeline());

            Assert.Equal(CircuitState.Open, breaker.State);
            Assert.Equal(1, attempts);
        }

        [Fact]
        public async Task WithSlidingWindowAsync_FuncTaskT_ExecutesSuccessfully()
        {
            var limiter = new SlidingWindowRateLimiter(maxRequests: 2, window: TimeSpan.FromSeconds(1));

            Func<Task<int>> action = async () =>
            {
                await Task.Delay(5);
                return 77;
            };

            var pipeline = action.WithSlidingWindowAsync(limiter);
            var result = await pipeline();

            Assert.Equal(77, result);
        }

        [Fact]
        public async Task WithSlidingWindowAsync_FuncTask_ExecutesSuccessfully()
        {
            var limiter = new SlidingWindowRateLimiter(maxRequests: 1, window: TimeSpan.FromSeconds(1));
            bool executed = false;

            Func<Task> action = async () =>
            {
                await Task.Delay(5);
                executed = true;
            };

            var pipeline = action.WithSlidingWindowAsync(limiter);
            await pipeline();

            Assert.True(executed);
        }

        [Fact]
        public async Task WithSlidingWindowAsync_Should_Block_When_Limit_Exceeded()
        {
            var limiter = new SlidingWindowRateLimiter(maxRequests: 1, window: TimeSpan.FromMilliseconds(200));
            bool executed = false;

            Func<Task> action = async () =>
            {
                executed = true;
                await Task.CompletedTask;
            };

            var pipeline = action.WithSlidingWindowAsync(limiter);

            // First call succeeds
            await pipeline();

            // Second call should block until window passes
            var start = DateTime.UtcNow;
            await pipeline();
            var elapsed = DateTime.UtcNow - start;

            Assert.True(elapsed >= TimeSpan.FromMilliseconds(150));
            Assert.True(executed);
        }
    }
}
