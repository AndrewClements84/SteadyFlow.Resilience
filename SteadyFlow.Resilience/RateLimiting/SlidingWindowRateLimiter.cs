using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SteadyFlow.Resilience.RateLimiting
{
    public class SlidingWindowRateLimiter
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _window;
        private readonly ConcurrentQueue<DateTime> _timestamps = new ConcurrentQueue<DateTime>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public SlidingWindowRateLimiter(int maxRequests, TimeSpan window)
        {
            if (maxRequests <= 0) throw new ArgumentOutOfRangeException(nameof(maxRequests));
            if (window.TotalMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(window));

            _maxRequests = maxRequests;
            _window = window;
        }

        public async Task WaitForAvailabilityAsync()
        {
            while (true)
            {
                await _semaphore.WaitAsync();
                try
                {
                    var now = DateTime.UtcNow;

                    // Remove expired timestamps
                    while (_timestamps.TryPeek(out var ts) && now - ts > _window)
                        _timestamps.TryDequeue(out _);

                    if (_timestamps.Count < _maxRequests)
                    {
                        _timestamps.Enqueue(now);
                        return; // allowed
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                // Delay before retrying
                await Task.Delay(50);
            }
        }
    }
}
