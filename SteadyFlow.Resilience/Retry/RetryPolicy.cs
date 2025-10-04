using SteadyFlow.Resilience.Metrics;
using System;
using System.Threading.Tasks;

namespace SteadyFlow.Resilience.Retry
{
    public class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _baseDelay;
        private readonly IBackoffStrategy _strategy;
        private readonly IMetricsObserver _observer;

        public RetryPolicy(
            int maxRetries = 3,
            int initialDelayMs = 200,
            IMetricsObserver observer = null,
            IBackoffStrategy strategy = null)
        {
            if (maxRetries <= 0) throw new ArgumentOutOfRangeException(nameof(maxRetries));
            if (initialDelayMs <= 0) throw new ArgumentOutOfRangeException(nameof(initialDelayMs));

            _maxRetries = maxRetries;
            _baseDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            _strategy = strategy ?? new ExponentialBackoffStrategy();
            _observer = observer;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var attempt = 0;
            while (true)
            {
                try
                {
                    var result = await action();
                    _observer?.OnEvent("RetryPolicy", "Success");
                    return result;
                }
                catch (Exception ex)
                {
                    attempt++;
                    _observer?.OnRetry(attempt, ex);

                    if (attempt >= _maxRetries)
                    {
                        _observer?.OnEvent("RetryPolicy", "Failure");
                        throw;
                    }

                    var delay = _strategy.GetDelay(attempt, _baseDelay);
                    await Task.Delay(delay);
                }
            }
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            await ExecuteAsync(async () =>
            {
                await action();
                return true;
            });
        }
    }
}
