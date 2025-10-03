using SteadyFlow.Resilience.AspNetCore;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class ResiliencePipelineTests
    {
        [Fact]
        public async Task Build_NoOptions_RunsOriginalAction()
        {
            var options = new ResilienceOptions();
            var pipeline = new ResiliencePipeline(options);

            bool executed = false;
            Func<Task> action = () =>
            {
                executed = true;
                return Task.CompletedTask;
            };

            var result = pipeline.Build(action);
            await result();

            Assert.True(executed);
        }

        [Fact]
        public async Task Build_WithRetry_RetriesOnFailure()
        {
            var options = new ResilienceOptions
            {
                Retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 10)
            };
            var pipeline = new ResiliencePipeline(options);

            int attempts = 0;
            Func<Task> action = () =>
            {
                attempts++;
                if (attempts < 2) throw new Exception("fail once");
                return Task.CompletedTask;
            };

            var result = pipeline.Build(action);
            await result();

            Assert.Equal(2, attempts); // retried once
        }

        [Fact]
        public async Task Build_WithCircuitBreaker_TripsOnFailures()
        {
            var breaker = new CircuitBreakerPolicy(failureThreshold: 1, openDuration: TimeSpan.FromMilliseconds(100));
            var options = new ResilienceOptions { CircuitBreaker = breaker };
            var pipeline = new ResiliencePipeline(options);

            Func<Task> action = () => throw new Exception("fail");

            var result = pipeline.Build(action);

            // First call throws original exception
            await Assert.ThrowsAsync<Exception>(result);

            // Second call should throw CircuitBreakerOpenException
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(result);
        }

        [Fact]
        public async Task Build_WithTokenBucket_LimitsRequests()
        {
            var limiter = new TokenBucketRateLimiter(capacity: 1, refillRatePerSecond: 1);
            var options = new ResilienceOptions { TokenBucketLimiter = limiter };
            var pipeline = new ResiliencePipeline(options);

            int executed = 0;
            Func<Task> action = () =>
            {
                executed++;
                return Task.CompletedTask;
            };

            var result = pipeline.Build(action);

            // First executes immediately
            await result();

            // Second waits until token refills
            var start = DateTime.UtcNow;
            await result();
            var elapsed = DateTime.UtcNow - start;

            Assert.True(elapsed >= TimeSpan.FromMilliseconds(500));
            Assert.Equal(2, executed);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Options_IsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ResiliencePipeline(null));
        }

        [Fact]
        public void Build_Should_Throw_When_Action_IsNull()
        {
            var pipeline = new ResiliencePipeline(new ResilienceOptions());
            Assert.Throws<ArgumentNullException>(() => pipeline.Build(null));
        }
    }
}
