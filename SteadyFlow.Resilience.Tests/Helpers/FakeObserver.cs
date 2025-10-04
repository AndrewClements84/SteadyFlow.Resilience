using SteadyFlow.Resilience.Metrics;

namespace SteadyFlow.Resilience.Tests.Helpers
{
    /// <summary>
    /// A simple in-memory metrics observer for testing.
    /// Collects all event messages into a list for later assertions.
    /// </summary>
    public class FakeObserver : IMetricsObserver
    {
        public List<string> Events { get; } = new List<string>();

        private void Log(string message) => Events.Add(message);

        public void OnRetry(int attempt, Exception exception) =>
            Log($"RetryAttempt:{attempt}");

        public void OnCircuitOpened() => Log("CircuitOpened");
        public void OnCircuitClosed() => Log("CircuitClosed");
        public void OnCircuitHalfOpen() => Log("CircuitHalfOpen");

        public void OnRateLimited(string limiterType) =>
            Log($"RateLimited:{limiterType}");

        public void OnBatchProcessed(int itemCount) =>
            Log($"BatchProcessed:{itemCount}");

        public void OnEvent(string policyName, string message) =>
            Log($"{policyName}:{message}");
    }
}
