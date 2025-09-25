using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteadyFlow.Resilience.RateLimiting
{
    public class TokenBucketRateLimiter
    {
        private readonly int _capacity;
        private readonly int _refillRatePerSecond;
        private int _tokens;
        private readonly object _lock = new object();
        private DateTime _lastRefill;

        public TokenBucketRateLimiter(int capacity, int refillRatePerSecond)
        {
            _capacity = capacity;
            _refillRatePerSecond = refillRatePerSecond;
            _tokens = capacity;
            _lastRefill = DateTime.UtcNow;
        }

        public bool TryConsume(int tokens = 1)
        {
            lock (_lock)
            {
                Refill();
                if (_tokens >= tokens)
                {
                    _tokens -= tokens;
                    return true;
                }
                return false;
            }
        }

        public async Task WaitForAvailabilityAsync(int tokens = 1, CancellationToken cancellationToken = default)
        {
            while (!TryConsume(tokens))
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            var secondsPassed = (now - _lastRefill).TotalSeconds;
            var refill = (int)(secondsPassed * _refillRatePerSecond);
            if (refill > 0)
            {
                _tokens = Math.Min(_capacity, _tokens + refill);
                _lastRefill = now;
            }
        }
    }
}
