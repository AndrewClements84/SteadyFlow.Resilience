using System;

namespace SteadyFlow.Resilience.Retry
{
    public class JitterBackoffStrategy : IBackoffStrategy
    {
        private readonly Random _random = new Random();

        public TimeSpan GetDelay(int attempt, TimeSpan baseDelay)
        {
            double multiplier = Math.Pow(2, attempt - 1);
            double jitter = _random.NextDouble() * baseDelay.TotalMilliseconds;
            return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * multiplier + jitter);
        }
    }
}

