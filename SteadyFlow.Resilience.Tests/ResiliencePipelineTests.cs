using SteadyFlow.Resilience.AspNetCore;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;
using SteadyFlow.Resilience.Tests.Helpers;

namespace SteadyFlow.Resilience.Tests
{
    public class ResiliencePipelineTests
    {
        [Fact]
        public void Constructor_Should_Throw_When_Options_IsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ResiliencePipeline(null!));
        }

        [Fact]
        public void Build_Should_Throw_When_Action_IsNull()
        {
            var options = new ResilienceOptions();
            var pipeline = new ResiliencePipeline(options);

            Assert.Throws<ArgumentNullException>(() => pipeline.Build(null!));
        }

        [Fact]
        public async Task Build_Should_Run_Action_When_No_Policies_Configured()
        {
            var options = new ResilienceOptions();
            var pipeline = new ResiliencePipeline(options);

            var executed = false;
            var action = new Func<Task>(() =>
            {
                executed = true;
                return Task.CompletedTask;
            });

            var result = pipeline.Build(action);
            await result();

            Assert.True(executed);
        }

        [Fact]
        public async Task Build_Should_Apply_SlidingWindowLimiter_When_Configured()
        {
            var observer = new FakeObserver();
            var options = new ResilienceOptions
            {
                SlidingWindowLimiter = new SlidingWindowRateLimiter(maxRequests: 1, window: TimeSpan.FromMilliseconds(100), observer: observer)
            };
            var pipeline = new ResiliencePipeline(options);

            var calls = 0;
            Func<Task> action = () =>
            {
                calls++;
                return Task.CompletedTask;
            };

            var wrapped = pipeline.Build(action);

            await wrapped(); // first call executes
            await wrapped(); // second call should be rate limited, but still executes eventually

            Assert.Equal(2, calls);
            Assert.Contains("RateLimited:SlidingWindow", observer.Events);
        }

        [Fact]
        public async Task Build_Should_Apply_Retry_And_CircuitBreaker_When_Configured()
        {
            var observer = new FakeObserver();
            var options = new ResilienceOptions
            {
                Retry = new RetryPolicy(maxRetries: 1, initialDelayMs: 10, observer: observer),
                CircuitBreaker = new CircuitBreakerPolicy(failureThreshold: 1, openDuration: TimeSpan.FromMilliseconds(100), observer: observer)
            };

            var pipeline = new ResiliencePipeline(options);

            int attempts = 0;
            Func<Task> action = () =>
            {
                attempts++;
                throw new Exception("Fail!");
            };

            var wrapped = pipeline.Build(action);

            await Assert.ThrowsAsync<Exception>(async () => await wrapped());
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () => await wrapped());

            Assert.True(attempts >= 1);
            Assert.Contains("RetryAttempt:1", observer.Events);
            Assert.Contains("CircuitOpened", observer.Events);
        }
    }
}
