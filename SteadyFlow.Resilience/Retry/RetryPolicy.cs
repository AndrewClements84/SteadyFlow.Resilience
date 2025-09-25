using System;
using System.Threading.Tasks;

namespace SteadyFlow.Resilience.Retry
{
    public class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly double _backoffFactor;

        public RetryPolicy(int maxRetries = 3, int initialDelayMs = 200, double backoffFactor = 2.0)
        {
            _maxRetries = maxRetries;
            _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            _backoffFactor = backoffFactor;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            var attempt = 0;
            var delay = _initialDelay;

            while (true)
            {
                try
                {
                    return await action();
                }
                catch when (attempt < _maxRetries)
                {
                    attempt++;
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * _backoffFactor);
                }
            }
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            await ExecuteAsync(async () =>
            {
                await action();
                return true;
            });
        }
    }
}
