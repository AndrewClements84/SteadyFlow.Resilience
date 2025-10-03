using System;
using System.Threading.Tasks;

namespace SteadyFlow.Resilience.Policies
{
    public class CircuitBreakerPolicy
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _openDuration;
        private readonly object _lock = new object();

        private int _failureCount;
        private DateTime _lastFailureTime;
        private CircuitState _state = CircuitState.Closed;

        public CircuitBreakerPolicy(int failureThreshold, TimeSpan openDuration)
        {
            _failureThreshold = failureThreshold;
            _openDuration = openDuration;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open)
                {
                    if (DateTime.UtcNow - _lastFailureTime > _openDuration)
                    {
                        _state = CircuitState.HalfOpen;
                    }
                    else
                    {
                        throw new CircuitBreakerOpenException("Circuit is open");
                    }
                }
            }

            try
            {
                var result = await action();

                lock (_lock)
                {
                    _failureCount = 0;
                    if (_state == CircuitState.HalfOpen)
                        _state = CircuitState.Closed;
                }

                return result;
            }
            catch
            {
                lock (_lock)
                {
                    _failureCount++;
                    _lastFailureTime = DateTime.UtcNow;

                    if (_failureCount >= _failureThreshold)
                        _state = CircuitState.Open;
                }

                throw;
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

        public CircuitState State
        {
            get
            {
                lock (_lock) return _state;
            }
        }
    }

    public enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }
}
