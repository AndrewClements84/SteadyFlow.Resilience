using Microsoft.AspNetCore.Http;
using SteadyFlow.Resilience.AspNetCore;
using SteadyFlow.Resilience.Retry;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;

namespace SteadyFlow.Resilience.Tests
{
    public class ResilienceMiddlewareObserverTests
    {
        [Fact]
        public async Task Middleware_Should_Notify_Observer_On_Retry_And_Success()
        {
            // Arrange
            var observer = new FakeObserver();
            var options = new ResilienceOptions
            {
                Retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, observer: observer)
            };
            var pipeline = new ResiliencePipeline(options);

            var middleware = new ResilienceMiddleware(
                async (ctx) =>
                {
                    if (!ctx.Items.ContainsKey("retried"))
                    {
                        ctx.Items["retried"] = true;
                        throw new Exception("Transient fail");
                    }

                    await ctx.Response.WriteAsync("OK");
                },
                pipeline
            );

            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Contains(observer.ObservedEvents, e => e.StartsWith("Retry:"));
            Assert.Contains(observer.ObservedEvents, e => e.Contains("RetryPolicy:Success"));
        }

        [Fact]
        public async Task Middleware_Should_Notify_Observer_On_CircuitBreaker_Open()
        {
            var observer = new FakeObserver();
            var options = new ResilienceOptions
            {
                CircuitBreaker = new CircuitBreakerPolicy(failureThreshold: 1, openDuration: TimeSpan.FromMilliseconds(200), observer: observer)
            };

            var context = new DefaultHttpContext();

            RequestDelegate next = ctx =>
            {
                throw new Exception("Always fails");
            };

            var pipeline = new ResiliencePipeline(options);
            var middleware = new ResilienceMiddleware(next, pipeline);

            // First failure
            await Assert.ThrowsAsync<Exception>(() => middleware.InvokeAsync(context));
            // Second call should trip breaker
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => middleware.InvokeAsync(context));

            Assert.Contains("CircuitOpened", observer.ObservedEvents);
        }

        [Fact]
        public async Task Middleware_Should_Notify_Observer_On_TokenBucket_Throttle()
        {
            var observer = new FakeObserver();
            var options = new ResilienceOptions
            {
                TokenBucketLimiter = new TokenBucketRateLimiter(capacity: 1, refillRatePerSecond: 1, observer: observer)
            };

            var context1 = new DefaultHttpContext();
            var context2 = new DefaultHttpContext();

            RequestDelegate next = async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("OK");
            };

            var pipeline = new ResiliencePipeline(options);
            var middleware = new ResilienceMiddleware(next, pipeline);

            // First request should succeed
            await middleware.InvokeAsync(context1);

            // Second request immediately should trigger limiter wait
            var throttled = false;
            var throttledTask = middleware.InvokeAsync(context2);
            if (!throttledTask.Wait(50))
                throttled = true;

            Assert.True(throttled);
            Assert.Contains("RateLimited:TokenBucket", observer.ObservedEvents);
        }

        [Fact]
        public async Task Middleware_Should_Notify_Observer_On_BatchProcessing()
        {
            var observer = new FakeObserver();
            var processed = new List<int>();

            var batcher = new BatchProcessor<int>(
                batchSize: 2,
                interval: TimeSpan.FromMilliseconds(100),
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                },
                observer: observer);

            var options = new ResilienceOptions(); // No retry/breaker here

            var context = new DefaultHttpContext();
            RequestDelegate next = ctx =>
            {
                batcher.Add(1);
                batcher.Add(2);
                return Task.CompletedTask;
            };

            var pipeline = new ResiliencePipeline(options);
            var middleware = new ResilienceMiddleware(next, pipeline);

            await middleware.InvokeAsync(context);
            await Task.Delay(200); // let batch flush

            Assert.Equal(2, processed.Count);
            Assert.Contains("BatchProcessed:2", observer.ObservedEvents);
        }
    }
}
