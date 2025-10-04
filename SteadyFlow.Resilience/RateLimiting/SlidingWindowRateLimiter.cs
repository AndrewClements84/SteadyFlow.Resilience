using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteadyFlow.Resilience.Metrics;

namespace SteadyFlow.Resilience.RateLimiting
{
    public class SlidingWindowRateLimiter
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _window;
        private readonly Queue<DateTime> _requests = new Queue<DateTime>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly IMetricsObserver _observer;

        public SlidingWindowRateLimiter(int maxRequests, TimeSpan window, IMetricsObserver observer = null)
        {
            if (maxRequests <= 0) throw new ArgumentOutOfRangeException(nameof(maxRequests));
            if (window.TotalMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(window));

            _maxRequests = maxRequests;
            _window = window;
            _observer = observer;
        }

        public async Task WaitForAvailabilityAsync()
        {
            while (true)
            {
                await _lock.WaitAsync();
                try
                {
                    var now = DateTime.UtcNow;

                    while (_requests.Count > 0 && now - _requests.Peek() > _window)
                        _requests.Dequeue();

                    if (_requests.Count < _maxRequests)
                    {
                        _requests.Enqueue(now);
                        return;
                    }
                    else
                    {
                        _observer?.OnRateLimited("SlidingWindow");
                    }
                }
                finally
                {
                    _lock.Release();
                }

                await Task.Delay(100);
            }
        }
    }
}
