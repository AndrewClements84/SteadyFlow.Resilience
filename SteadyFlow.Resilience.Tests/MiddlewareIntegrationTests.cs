using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SteadyFlow.Resilience.AspNetCore;
using SteadyFlow.Resilience.Metrics;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;
using System.Net;

namespace SteadyFlow.Resilience.Tests
{
    public class MiddlewareIntegrationTests
    {
        private TestServer CreateServer(Action<ResilienceOptions> configure, RequestDelegate handler)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseResiliencePipeline(configure);
                    app.Run(handler);
                });

            return new TestServer(builder);
        }

        [Fact]
        public async Task Middleware_Should_Apply_Retry_And_Succeed()
        {
            var attempts = 0;

            var server = CreateServer(options =>
            {
                options.Retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 10);
            },
            async ctx =>
            {
                attempts++;
                if (attempts < 2)
                    throw new Exception("Transient failure");

                await ctx.Response.WriteAsync("Success");
            });

            var client = server.CreateClient();
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Success", content);
        }

        [Fact]
        public async Task Middleware_Should_Trip_CircuitBreaker()
        {
            var server = CreateServer(options =>
            {
                options.CircuitBreaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(500));
            },
            ctx =>
            {
                throw new Exception("Always fails");
            });

            var client = server.CreateClient();

            // First two calls fail with Exception
            await Assert.ThrowsAsync<Exception>(async () => await client.GetAsync("/"));
            await Assert.ThrowsAsync<Exception>(async () => await client.GetAsync("/"));

            // Third call should hit the breaker and return CircuitBreakerOpenException
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () => await client.GetAsync("/"));
        }

        [Fact]
        public async Task Middleware_Should_Respect_TokenBucketLimiter()
        {
            var server = CreateServer(options =>
            {
                options.TokenBucketLimiter = new TokenBucketRateLimiter(capacity: 1, refillRatePerSecond: 1);
            },
            async ctx =>
            {
                await ctx.Response.WriteAsync("OK");
            });

            var client = server.CreateClient();

            // First request succeeds
            var response1 = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            // Second request immediately should be throttled (because capacity = 1, refill = 1/s)
            var task2 = client.GetAsync("/");
            var completed = await Task.WhenAny(task2, Task.Delay(50));

            Assert.NotEqual(task2, completed); // means limiter throttled
        }

        [Fact]
        public async Task Middleware_Should_Report_RetryEvents()
        {
            // Arrange
            var observer = new FakeObserver();

            var services = new ServiceCollection();
            services.AddSingleton<IMetricsObserver>(observer);
            var provider = services.BuildServiceProvider();

            var app = new ApplicationBuilder(provider);
            app.UseResiliencePipeline(options =>
            {
                options.Retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, observer: observer);
            });

            app.Run(async context =>
            {
                throw new Exception("Always fail");
            });

            var pipeline = app.Build();

            var httpContext = new DefaultHttpContext();

            // Act
            await Assert.ThrowsAsync<Exception>(() => pipeline(httpContext));

            // Assert
            Assert.True(observer.ObservedEvents.Exists(e => e.StartsWith("Retry:")));
        }
    }
}
