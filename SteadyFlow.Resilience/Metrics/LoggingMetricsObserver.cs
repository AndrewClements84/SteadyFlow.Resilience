using Microsoft.Extensions.Logging;
using System;

namespace SteadyFlow.Resilience.Metrics
{
    internal class LoggingMetricsObserver : IMetricsObserver
    {
        private readonly ILogger _logger;
        public LoggingMetricsObserver(ILogger logger) => _logger = logger;

        public void OnRetry(int attempt, Exception ex) =>
            _logger.LogWarning(ex, "[Retry] Attempt {Attempt} failed.", attempt);

        public void OnCircuitOpened() => _logger.LogError("[CircuitBreaker] Circuit opened.");
        public void OnCircuitClosed() => _logger.LogInformation("[CircuitBreaker] Circuit closed.");
        public void OnCircuitHalfOpen() => _logger.LogInformation("[CircuitBreaker] Circuit half-open.");

        public void OnRateLimited(string limiterType) =>
            _logger.LogWarning("[RateLimiter] {LimiterType} limited request.", limiterType);

        public void OnBatchProcessed(int itemCount) =>
            _logger.LogInformation("[BatchProcessor] Processed {ItemCount} items.", itemCount);

        public void OnEvent(string policyName, string message) =>
            _logger.LogInformation("[{Policy}] {Message}", policyName, message);
    }
}
