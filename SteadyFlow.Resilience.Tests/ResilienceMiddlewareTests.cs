using Microsoft.AspNetCore.Http;
using SteadyFlow.Resilience.AspNetCore;

namespace SteadyFlow.Resilience.Tests
{
    public class ResilienceMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_CallsNextDelegate()
        {
            bool nextCalled = false;

            RequestDelegate next = ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var options = new ResilienceOptions(); // no policies
            var pipeline = new ResiliencePipeline(options);
            var middleware = new ResilienceMiddleware(next, pipeline);

            var context = new DefaultHttpContext();
            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_AppliesPipeline()
        {
            int attempts = 0;

            RequestDelegate next = ctx =>
            {
                attempts++;
                if (attempts < 2) throw new Exception("fail once");
                return Task.CompletedTask;
            };

            var options = new ResilienceOptions
            {
                Retry = new SteadyFlow.Resilience.Retry.RetryPolicy(maxRetries: 2, initialDelayMs: 10)
            };

            var pipeline = new ResiliencePipeline(options);
            var middleware = new ResilienceMiddleware(next, pipeline);

            var context = new DefaultHttpContext();
            await middleware.InvokeAsync(context);

            Assert.Equal(2, attempts); // retried once
        }
    }
}
