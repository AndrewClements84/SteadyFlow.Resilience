using Microsoft.AspNetCore.Http;
using SteadyFlow.Resilience.AspNetCore;

namespace SteadyFlow.Resilience.Tests
{
    public class ResilienceMiddlewareTests
    {
        [Fact]
        public void Constructor_Should_Throw_When_Next_IsNull()
        {
            var pipeline = new ResiliencePipeline(new ResilienceOptions());

            Assert.Throws<ArgumentNullException>(() =>
                new ResilienceMiddleware(null!, pipeline));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Pipeline_IsNull()
        {
            RequestDelegate next = ctx => Task.CompletedTask;

            Assert.Throws<ArgumentNullException>(() =>
                new ResilienceMiddleware(next, null!));
        }

        [Fact]
        public async Task InvokeAsync_Should_Call_Next()
        {
            var context = new DefaultHttpContext();
            var called = false;

            RequestDelegate next = ctx =>
            {
                called = true;
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            };

            var pipeline = new ResiliencePipeline(new ResilienceOptions());
            var middleware = new ResilienceMiddleware(next, pipeline);

            await middleware.InvokeAsync(context);

            Assert.True(called);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_Should_Propagate_Exception_From_Next()
        {
            var context = new DefaultHttpContext();

            RequestDelegate next = ctx =>
            {
                throw new InvalidOperationException("Boom!");
            };

            var pipeline = new ResiliencePipeline(new ResilienceOptions());
            var middleware = new ResilienceMiddleware(next, pipeline);

            await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        }
    }
}
