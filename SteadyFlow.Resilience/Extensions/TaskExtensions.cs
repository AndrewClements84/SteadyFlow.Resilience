using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.Retry;
using System;
using System.Threading.Tasks;

namespace SteadyFlow.Resilience.Extensions
{
    public static class TaskExtensions
    {
        public static Task<T> WithRetryAsync<T>(this Func<Task<T>> action, RetryPolicy policy)
        {
            return policy.ExecuteAsync(action);
        }

        public static Task WithRetryAsync(this Func<Task> action, RetryPolicy policy)
        {
            return policy.ExecuteAsync(action);
        }

        public static Task<T> WithCircuitBreakerAsync<T>(this Func<Task<T>> action, CircuitBreakerPolicy policy)
        {
            return policy.ExecuteAsync(action);
        }

        public static Task WithCircuitBreakerAsync(this Func<Task> action, CircuitBreakerPolicy policy)
        {
            return policy.ExecuteAsync(action);
        }
    }
}
