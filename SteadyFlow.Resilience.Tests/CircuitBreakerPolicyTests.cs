using SteadyFlow.Resilience.Policies;
using Xunit;

namespace SteadyFlow.Resilience.Tests
{
    public class CircuitBreakerPolicyTests
    {
        [Fact]
        public async Task Should_Allow_Calls_When_Closed()
        {
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(200));

            var result = await breaker.ExecuteAsync(() => Task.FromResult("ok"));

            Assert.Equal("ok", result);
            Assert.Equal(CircuitState.Closed, breaker.State);
        }

        [Fact]
        public async Task Should_Open_Circuit_After_Failures()
        {
            var breaker = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(200));

            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync<string>(() => throw new Exception("fail")));
            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync<string>(() => throw new Exception("fail")));

            Assert.Equal(CircuitState.Open, breaker.State);
        }

        [Fact]
        public async Task Should_Reject_Calls_When_Open()
        {
            var breaker = new CircuitBreakerPolicy(failureThreshold: 1, openDuration: TimeSpan.FromMilliseconds(500));

            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync<string>(() => throw new Exception("fail")));

            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => breaker.ExecuteAsync(() => Task.FromResult("won’t run")));
        }

        [Fact]
        public async Task Should_Transition_To_HalfOpen_After_OpenDuration()
        {
            var breaker = new CircuitBreakerPolicy(failureThreshold: 1, openDuration: TimeSpan.FromMilliseconds(200));

            await Assert.ThrowsAsync<Exception>(() => breaker.ExecuteAsync<string>(() => throw new Exception("fail")));
            Assert.Equal(CircuitState.Open, breaker.State);

            await Task.Delay(250);

            // Next call is allowed in half-open
            var result = await breaker.ExecuteAsync(() => Task.FromResult("success"));
            Assert.Equal("success", result);
            Assert.Equal(CircuitState.Closed, breaker.State);
        }
    }
}
