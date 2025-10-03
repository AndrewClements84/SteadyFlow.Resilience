using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using SteadyFlow.Resilience.AspNetCore;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;
using System.Net;

namespace SteadyFlow.Resilience.Tests
{
    public class MiddlewareIntegrationTests
    {
        [Fact]
        public async Task Middleware_Should_Apply_RetryPolicy()
        {
            int attempts = 0;

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseResiliencePipeline(options =>
                    {
                        options.Retry = new RetryPolicy(maxRetries: 2, initialDelayMs: 10);
                    });

                    app.Run(ctx =>
                    {
                        attempts++;
                        if (attempts < 2)
                            throw new Exception("fail once");

                        return ctx.Response.WriteAsync("OK");
                    });
                });

            using var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("OK", content);
            Assert.Equal(2, attempts); // retried once
        }

        [Fact]
        public async Task Middleware_Should_Trip_CircuitBreaker()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseResiliencePipeline(options =>
                    {
                        // threshold=1 means: after the *first* failure, breaker opens
                        options.CircuitBreaker = new CircuitBreakerPolicy(
                            failureThreshold: 1,
                            openDuration: TimeSpan.FromMilliseconds(200));
                    });

                    app.Run(ctx =>
                    {
                        throw new Exception("Always fails");
                    });
                });

            using var server = new TestServer(builder);
            var client = server.CreateClient();

            // First call: original exception is rethrown from ExecuteAsync's catch
            var ex1 = await Assert.ThrowsAsync<Exception>(() => client.GetAsync("/"));
            Assert.Contains("Always fails", ex1.Message);

            // Second call: circuit is already Open, so fast-fail with CircuitBreakerOpenException
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => client.GetAsync("/"));
        }

        [Fact]
        public async Task Middleware_Should_Enforce_TokenBucketRateLimiter()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseResiliencePipeline(options =>
                    {
                        options.TokenBucketLimiter = new TokenBucketRateLimiter(
                            capacity: 1,
                            refillRatePerSecond: 1);
                    });

                    app.Run(ctx =>
                    {
                        return ctx.Response.WriteAsync("OK");
                    });
                });

            using var server = new TestServer(builder);
            var client = server.CreateClient();

            // First request succeeds immediately
            var response1 = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            // Second request should wait until token refills
            var start = DateTime.UtcNow;
            var response2 = await client.GetAsync("/");
            var elapsed = DateTime.UtcNow - start;

            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            Assert.True(elapsed >= TimeSpan.FromMilliseconds(500)); // waited for refill
        }

        [Fact]
        public async Task Middleware_Should_Enforce_SlidingWindowRateLimiter()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseResiliencePipeline(options =>
                    {
                        options.SlidingWindowLimiter = new SlidingWindowRateLimiter(
                            maxRequests: 1,
                            window: TimeSpan.FromMilliseconds(300));
                    });

                    app.Run(ctx =>
                    {
                        return ctx.Response.WriteAsync("OK");
                    });
                });

            using var server = new TestServer(builder);
            var client = server.CreateClient();

            // First request succeeds
            var response1 = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            // Second request should be delayed until window resets
            var start = DateTime.UtcNow;
            var response2 = await client.GetAsync("/");
            var elapsed = DateTime.UtcNow - start;

            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            Assert.True(elapsed >= TimeSpan.FromMilliseconds(200)); // at least some wait
        }
    }
}
