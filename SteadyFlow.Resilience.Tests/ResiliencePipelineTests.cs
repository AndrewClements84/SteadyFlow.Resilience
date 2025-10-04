using SteadyFlow.Resilience.AspNetCore;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;

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
            Assert.Contains("RateLimited:SlidingWindow", observer.ObservedEvents);
        }

        [Fact]
        public async Task Build_Should_Apply_Retry_And_CircuitBreaker_When_Configured()
        {
            var observer = new FakeObserver();

            var options = new ResilienceOptions
            {
                Retry = new RetryPolicy(maxRetries: 1, initialDelayMs: 10, observer: observer),
                CircuitBreaker = new CircuitBreakerPolicy(
                    failureThreshold: 1,
                    openDuration: TimeSpan.FromMilliseconds(50),
                    observer: observer) 
            };

            var pipeline = new ResiliencePipeline(options);

            Func<Task> action = () => throw new Exception("Fail");
            var built = pipeline.Build(action);

            // First call fails -> Retry logs, breaker records failure and opens
            await Assert.ThrowsAsync<Exception>(built);

            // Second call while open -> throws CircuitBreakerOpenException
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(built);

            Assert.Contains(observer.ObservedEvents, e => e.StartsWith("Retry:"));
            Assert.Contains(observer.ObservedEvents, e => e.Contains("RetryPolicy:Failure"));
            Assert.Contains(observer.ObservedEvents, e => e.Contains("CircuitOpened"));
        }

        [Fact]
        public async Task Build_Should_Pass_Observer_To_RetryPolicy()
        {
            // Arrange
            var observer = new FakeObserver();
            var options = new ResilienceOptions
            {
                Retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, observer: observer)
            };

            var pipeline = new ResiliencePipeline(options);

            int attempt = 0;
            Func<Task> action = async () =>
            {
                attempt++;
                throw new Exception("Fail");
            };

            // Act
            var resilient = pipeline.Build(action);
            await Assert.ThrowsAsync<Exception>(() => resilient());

            // Assert
            Assert.True(observer.ObservedEvents.Exists(e => e.StartsWith("Retry:")));
        }
    }
}
