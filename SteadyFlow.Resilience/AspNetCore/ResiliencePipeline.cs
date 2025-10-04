using System;
using System.Threading.Tasks;
using SteadyFlow.Resilience.Extensions;
using SteadyFlow.Resilience.Metrics;
using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.RateLimiting;
using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.AspNetCore
{
    public class ResilienceOptions
    {
        public RetryPolicy Retry { get; set; }
        public CircuitBreakerPolicy CircuitBreaker { get; set; }
        public TokenBucketRateLimiter TokenBucketLimiter { get; set; }
        public SlidingWindowRateLimiter SlidingWindowLimiter { get; set; }
        public IMetricsObserver Observer { get; set; } // NEW
    }

    public class ResiliencePipeline
    {
        private readonly ResilienceOptions _options;

        public ResiliencePipeline(ResilienceOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Func<Task> Build(Func<Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var pipeline = action;

            if (_options.TokenBucketLimiter != null)
                pipeline = pipeline.WithTokenBucketAsync(_options.TokenBucketLimiter);

            if (_options.SlidingWindowLimiter != null)
                pipeline = pipeline.WithSlidingWindowAsync(_options.SlidingWindowLimiter);

            if (_options.Retry != null)
                pipeline = pipeline.WithRetryAsync(_options.Retry);

            if (_options.CircuitBreaker != null)
                pipeline = pipeline.WithCircuitBreakerAsync(_options.CircuitBreaker);

            return pipeline;
        }
    }
}
