using SteadyFlow.Resilience.Metrics;

namespace SteadyFlow.Resilience.Tests
{
    public class FakeObserver : IMetricsObserver
    {
        public List<string> ObservedEvents { get; } = new();

        public void OnRetry(int attempt, Exception ex) => ObservedEvents.Add($"Retry:{attempt}");
        public void OnCircuitOpened() => ObservedEvents.Add("CircuitOpened");
        public void OnCircuitClosed() => ObservedEvents.Add("CircuitClosed");
        public void OnCircuitHalfOpen() => ObservedEvents.Add("CircuitHalfOpen");
        public void OnRateLimited(string limiterType) => ObservedEvents.Add($"RateLimited:{limiterType}");
        public void OnBatchProcessed(int itemCount) => ObservedEvents.Add($"BatchProcessed:{itemCount}");
        public void OnEvent(string policyName, string message) => ObservedEvents.Add($"{policyName}:{message}");
    }
}