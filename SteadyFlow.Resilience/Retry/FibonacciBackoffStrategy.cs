using System;

namespace SteadyFlow.Resilience.Retry
{
    public class FibonacciBackoffStrategy : IBackoffStrategy
    {
        public TimeSpan GetDelay(int attempt, TimeSpan baseDelay)
        {
            int fib = GetFibonacci(attempt);
            return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * fib);
        }

        private int GetFibonacci(int n)
        {
            if (n <= 1) return n;
            int a = 0, b = 1, c = 1;
            for (int i = 2; i <= n; i++)
            {
                c = a + b;
                a = b;
                b = c;
            }
            return c;
        }
    }
}

