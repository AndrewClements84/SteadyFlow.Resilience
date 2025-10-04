using Microsoft.Extensions.Logging;
using SteadyFlow.Resilience.Metrics;
using Xunit;

namespace SteadyFlow.Resilience.Tests
{
    public class LoggingMetricsObserverTests
    {
        private class TestLogger : ILogger
        {
            public List<string> Messages { get; } = new();

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel,
                                    EventId eventId,
                                    TState state,
                                    Exception exception,
                                    Func<TState, Exception, string> formatter)
            {
                Messages.Add(formatter(state, exception));
            }

            private class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new();
                public void Dispose() { }
            }
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_IsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new LoggingMetricsObserver(null!));
        }

        [Fact]
        public void OnRetry_Should_Log_Message()
        {
            var logger = new TestLogger();
            var observer = new LoggingMetricsObserver(logger);

            observer.OnRetry(1, new Exception("fail"));

            Assert.Contains(logger.Messages, m => m.Contains("[Retry] Attempt 1 failed."));
        }

        [Fact]
        public void CircuitBreaker_Should_Log_Messages()
        {
            var logger = new TestLogger();
            var observer = new LoggingMetricsObserver(logger);

            observer.OnCircuitOpened();
            observer.OnCircuitClosed();
            observer.OnCircuitHalfOpen();

            Assert.Contains(logger.Messages, m => m.Contains("[CircuitBreaker] Circuit opened."));
            Assert.Contains(logger.Messages, m => m.Contains("[CircuitBreaker] Circuit closed."));
            Assert.Contains(logger.Messages, m => m.Contains("[CircuitBreaker] Circuit half-open."));
        }

        [Fact]
        public void OnRateLimited_Should_Log_Message()
        {
            var logger = new TestLogger();
            var observer = new LoggingMetricsObserver(logger);

            observer.OnRateLimited("TokenBucket");

            Assert.Contains(logger.Messages, m => m.Contains("[RateLimiter] TokenBucket limited request."));
        }

        [Fact]
        public void OnBatchProcessed_Should_Log_Message()
        {
            var logger = new TestLogger();
            var observer = new LoggingMetricsObserver(logger);

            observer.OnBatchProcessed(5);

            Assert.Contains(logger.Messages, m => m.Contains("[BatchProcessor] Processed 5 items."));
        }

        [Fact]
        public void OnEvent_Should_Log_Custom_Message()
        {
            var logger = new TestLogger();
            var observer = new LoggingMetricsObserver(logger);

            observer.OnEvent("RetryPolicy", "Custom message here");

            Assert.Contains(logger.Messages, m => m.Contains("[RetryPolicy] Custom message here"));
        }
    }
}
