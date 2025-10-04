using SteadyFlow.Resilience.Metrics;
using SteadyFlow.Resilience.Policies;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SteadyFlow.Resilience.Tests
{
    public class CircuitBreakerPolicyObserverTests
    {
        [Fact]
        public async Task ExecuteAsync_Should_Notify_Observer_On_State_Transitions()
        {
            var observer = new FakeObserver();
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(100), observer);

            // cause 2 failures → Open
            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync(() => throw new Exception("fail 1")));
            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync(() => throw new Exception("fail 2")));

            Assert.Equal(CircuitState.Open, breaker.State);
            Assert.Contains("CircuitOpened", observer.ObservedEvents);

            // wait for open window to expire → HalfOpen
            await Task.Delay(120);
            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync(() => throw new Exception("half-open fail")));
            Assert.Contains("CircuitHalfOpen", observer.ObservedEvents);

            // next successful call → Closed
            await Task.Delay(120);
            await breaker.ExecuteAsync(() => Task.CompletedTask);
            Assert.Equal(CircuitState.Closed, breaker.State);
            Assert.Contains("CircuitClosed", observer.ObservedEvents);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Raise_CircuitOpened_When_Open()
        {
            var observer = new FakeObserver();
            var breaker = new CircuitBreakerPolicy(failureThreshold: 1, openDuration: TimeSpan.FromMilliseconds(500), observer);

            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync(() => throw new Exception("first fail")));

            Assert.Equal(CircuitState.Open, breaker.State);
            Assert.Contains("CircuitOpened", observer.ObservedEvents);

            // second call while open → immediately throws
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => breaker.ExecuteAsync(() => Task.CompletedTask));
            Assert.True(observer.ObservedEvents.Count(e => e.Contains("CircuitOpened")) >= 2);
        }
    }
}

