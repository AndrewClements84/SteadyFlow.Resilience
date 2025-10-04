using SteadyFlow.Resilience.Policies;
using SteadyFlow.Resilience.Tests.Helpers;

namespace SteadyFlow.Resilience.Tests
{
    public class CircuitBreakerPolicyTests
    {
        [Fact]
        public async Task Should_Allow_Execution_When_Closed()
        {
            var breaker = new CircuitBreakerPolicy(2, TimeSpan.FromMilliseconds(200), observer: null);

            var result = await breaker.ExecuteAsync(() => Task.FromResult("ok"));

            Assert.Equal("ok", result);
            Assert.Equal(CircuitState.Closed, breaker.State);
        }

        [Fact]
        public async Task Should_Open_After_Threshold_Reached()
        {
            var breaker = new CircuitBreakerPolicy(2, TimeSpan.FromMilliseconds(500), observer: null);

            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync<string>(() => throw new Exception("fail1")));
            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync<string>(() => throw new Exception("fail2")));

            Assert.Equal(CircuitState.Open, breaker.State);
        }

        [Fact]
        public async Task Should_Throw_OpenException_While_Open()
        {
            var breaker = new CircuitBreakerPolicy(1, TimeSpan.FromMilliseconds(500), observer: null);

            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync<string>(() => throw new Exception("fail")));

            Assert.Equal(CircuitState.Open, breaker.State);

            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
                breaker.ExecuteAsync<string>(() => Task.FromResult("ok")));
        }

        [Fact]
        public async Task Should_HalfOpen_And_Close_On_Success()
        {
            var breaker = new CircuitBreakerPolicy(1, TimeSpan.FromMilliseconds(50), observer: null);

            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync<string>(() => throw new Exception("fail")));

            // Wait for open period to expire
            await Task.Delay(100);

            // Next call should be half-open, then closed after success
            var result = await breaker.ExecuteAsync(() => Task.FromResult("ok"));

            Assert.Equal("ok", result);
            Assert.Equal(CircuitState.Closed, breaker.State);
        }

        [Fact]
        public async Task Should_Report_Events_To_Observer()
        {
            var observer = new FakeObserver();
            var breaker = new CircuitBreakerPolicy(1, TimeSpan.FromMilliseconds(200), observer);

            // Trip the breaker
            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync<string>(() => throw new Exception("fail")));

            // Circuit should be open
            Assert.Equal(CircuitState.Open, breaker.State);
            Assert.Contains("CircuitOpened", observer.Events);

            // Wait for reset window
            await Task.Delay(250);

            // Next call succeeds → should transition half-open → closed
            var result = await breaker.ExecuteAsync(() => Task.FromResult("ok"));

            Assert.Equal("ok", result);
            Assert.Equal(CircuitState.Closed, breaker.State);

            Assert.Contains("CircuitHalfOpen", observer.Events);
            Assert.Contains("CircuitClosed", observer.Events);
        }

        [Fact]
        public void Constructor_Should_Throw_When_FailureThreshold_IsZeroOrNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CircuitBreakerPolicy(failureThreshold: 0, openDuration: TimeSpan.FromSeconds(1)));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CircuitBreakerPolicy(failureThreshold: -5, openDuration: TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public void Constructor_Should_Throw_When_OpenDuration_IsZeroOrNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.Zero));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(-1)));
        }
    }
}
