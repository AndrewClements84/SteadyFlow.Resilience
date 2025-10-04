using System;

namespace SteadyFlow.Resilience.Retry
{
    public class ExponentialBackoffStrategy : IBackoffStrategy
    {
        public TimeSpan GetDelay(int attempt, TimeSpan baseDelay)
        {
            return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
        }
    }
}

