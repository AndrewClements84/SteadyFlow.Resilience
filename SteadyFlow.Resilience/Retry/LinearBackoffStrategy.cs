using System;

namespace SteadyFlow.Resilience.Retry
{
    public class LinearBackoffStrategy : IBackoffStrategy
    {
        public TimeSpan GetDelay(int attempt, TimeSpan baseDelay)
        {
            return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * attempt);
        }
    }
}

