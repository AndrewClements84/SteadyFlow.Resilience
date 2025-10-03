using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;
using System;
using System.Threading.Tasks;

namespace SteadyFlow.Resilience.Extensions
{
    public static class TaskExtensions
    {
        // --- Retry ---
        public static Func<Task<T>> WithRetryAsync<T>(this Func<Task<T>> action, RetryPolicy policy)
            => () => policy.ExecuteAsync(action);

        public static Func<Task> WithRetryAsync(this Func<Task> action, RetryPolicy policy)
            => () => policy.ExecuteAsync(action);

        // --- Circuit Breaker ---
        public static Func<Task<T>> WithCircuitBreakerAsync<T>(this Func<Task<T>> action, CircuitBreakerPolicy policy)
            => () => policy.ExecuteAsync(action);

        public static Func<Task> WithCircuitBreakerAsync(this Func<Task> action, CircuitBreakerPolicy policy)
            => () => policy.ExecuteAsync(action);

        // --- Sliding Window Rate Limiter ---
        public static Func<Task<T>> WithSlidingWindowAsync<T>(this Func<Task<T>> action, SlidingWindowRateLimiter limiter)
            => async () =>
            {
                await limiter.WaitForAvailabilityAsync();
                return await action();
            };

        public static Func<Task> WithSlidingWindowAsync(this Func<Task> action, SlidingWindowRateLimiter limiter)
            => async () =>
            {
                await limiter.WaitForAvailabilityAsync();
                await action();
            };
    }
}
