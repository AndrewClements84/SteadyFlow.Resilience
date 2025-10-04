using System;
using System.Threading.Tasks;
using SteadyFlow.Resilience.AspNetCore;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;
using SteadyFlow.Resilience.Tests.Helpers;
using Xunit;

namespace SteadyFlow.Resilience.Tests
{
    public class ResiliencePipelineTests
    {
        [Fact]
        public async Task Should_Run_Action_Without_Policies()
        {
            var options = new ResilienceOptions
            {
                Retry = null,
                CircuitBreaker = null,
                TokenBucketLimiter = null,
                SlidingWindowLimiter = null,
                Observer = null
            };

            var pipeline = new ResiliencePipeline(options);

            bool called = false;
            var action = new Func<Task>(() =>
            {
                called = true;
                return Task.CompletedTask;
            });

            var built = pipeline.Build(action);
            await built();

            Assert.True(called);
        }

        [Fact]
        public async Task Should_Apply_Retry_Policy()
        {
            int attempts = 0;
            var retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, observer: null);

            var options = new ResilienceOptions
            {
                Retry = retry,
                CircuitBreaker = null,
                TokenBucketLimiter = null,
                SlidingWindowLimiter = null,
                Observer = null
            };

            var pipeline = new ResiliencePipeline(options);

            var built = pipeline.Build(async () =>
            {
                attempts++;
                if (attempts < 2) throw new Exception("fail");
                await Task.CompletedTask;
            });

            await built();

            Assert.Equal(2, attempts);
        }

        [Fact]
        public async Task Should_Report_Events_To_Observer_From_Pipeline()
        {
            var observer = new FakeObserver();
            int attempts = 0;

            var retry = new RetryPolicy(maxRetries: 1, initialDelayMs: 10, observer: observer);

            var options = new ResilienceOptions
            {
                Retry = retry,
                CircuitBreaker = null,
                TokenBucketLimiter = null,
                SlidingWindowLimiter = null,
                Observer = observer
            };

            var pipeline = new ResiliencePipeline(options);

            var built = pipeline.Build(async () =>
            {
                attempts++;
                throw new Exception("always fail");
            });

            await Assert.ThrowsAsync<Exception>(built);

            Assert.Contains(observer.Events, e => e.StartsWith("RetryAttempt:"));
            Assert.True(attempts > 1);
        }
    }
}
