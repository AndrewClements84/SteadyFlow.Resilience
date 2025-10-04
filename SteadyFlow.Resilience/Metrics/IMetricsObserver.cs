using System;

namespace SteadyFlow.Resilience.Metrics
{
    /// <summary>
    /// Provides hooks for observing runtime events from resilience policies.
    /// Implement this to log, collect metrics, or send telemetry.
    /// </summary>
    public interface IMetricsObserver
    {
        // Retry events
        void OnRetry(int attempt, Exception exception);

        // Circuit breaker events
        void OnCircuitOpened();
        void OnCircuitClosed();
        void OnCircuitHalfOpen();

        // Rate limiting events
        void OnRateLimited(string limiterType);

        // Batch processing
        void OnBatchProcessed(int itemCount);

        // General fallback for unknown or custom policy events
        void OnEvent(string policyName, string message);
    }
}
