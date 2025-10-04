using System;
using System.Threading;
using System.Threading.Tasks;
using SteadyFlow.Resilience.Metrics;

namespace SteadyFlow.Resilience.RateLimiting
{
    public class TokenBucketRateLimiter
    {
        private readonly int _capacity;
        private readonly double _refillRatePerSecond;
        private double _tokens;
        private DateTime _lastRefill;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly IMetricsObserver _observer;

        public TokenBucketRateLimiter(int capacity, double refillRatePerSecond, IMetricsObserver observer = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (refillRatePerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(refillRatePerSecond));

            _capacity = capacity;
            _refillRatePerSecond = refillRatePerSecond;
            _tokens = capacity;
            _lastRefill = DateTime.UtcNow;
            _observer = observer;
        }

        public async Task WaitForAvailabilityAsync()
        {
            while (true)
            {
                await _lock.WaitAsync();
                try
                {
                    Refill();

                    if (_tokens >= 1)
                    {
                        _tokens -= 1;
                        return;
                    }
                    else
                    {
                        _observer?.OnRateLimited("TokenBucket");
                    }
                }
                finally
                {
                    _lock.Release();
                }

                await Task.Delay(100);
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastRefill).TotalSeconds;
            _lastRefill = now;

            _tokens = Math.Min(_capacity, _tokens + elapsed * _refillRatePerSecond);
        }
    }
}
