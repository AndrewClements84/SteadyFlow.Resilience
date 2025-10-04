using System;

namespace SteadyFlow.Resilience.Retry
{
    public interface IBackoffStrategy
    {
        /// <summary>
        /// Calculates the delay for the specified retry attempt.
        /// </summary>
        /// <param name="attempt">The current attempt number (starting from 1).</param>
        /// <param name="baseDelay">The base delay configured in the RetryPolicy.</param>
        /// <returns>A TimeSpan representing the delay before the next retry.</returns>
        TimeSpan GetDelay(int attempt, TimeSpan baseDelay);
    }
}
