using System;
using System.Threading.Tasks;
using SteadyFlow.Resilience.Extensions;
using SteadyFlow.Resilience.Retry;
using Xunit;

namespace SteadyFlow.Resilience.Tests
{
    public class TaskExtensionsTests
    {
        [Fact]
        public async Task WithRetryAsync_FuncTaskT_ExecutesSuccessfully()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 10, backoffFactor: 1.0);
            Func<Task<int>> action = async () =>
            {
                await Task.Delay(5);
                return 42;
            };

            // Act
            var result = await action.WithRetryAsync(policy);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task WithRetryAsync_FuncTask_ExecutesSuccessfully()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 3, initialDelayMs: 10, backoffFactor: 1.0);
            bool executed = false;

            Func<Task> action = async () =>
            {
                await Task.Delay(5);
                executed = true;
            };

            // Act
            await action.WithRetryAsync(policy);

            // Assert
            Assert.True(executed);
        }

        [Fact]
        public async Task WithRetryAsync_RetriesOnFailure()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, backoffFactor: 1.0);
            int attempts = 0;

            Func<Task<int>> action = () =>
            {
                attempts++;
                if (attempts < 2)
                {
                    throw new InvalidOperationException("Fail first attempt");
                }
                return Task.FromResult(99);
            };

            // Act
            var result = await action.WithRetryAsync(policy);

            // Assert
            Assert.Equal(99, result);
            Assert.Equal(2, attempts); // retried once
        }

        [Fact]
        public async Task WithRetryAsync_ThrowsAfterMaxRetries()
        {
            // Arrange
            var policy = new RetryPolicy(maxRetries: 2, initialDelayMs: 10, backoffFactor: 1.0);
            int attempts = 0;

            Func<Task<int>> action = () =>
            {
                attempts++;
                throw new InvalidOperationException("Always fails");
            };

            // Act + Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => action.WithRetryAsync(policy));
            Assert.Equal("Always fails", ex.Message);
            Assert.Equal(3, attempts); // initial try + 2 retries
        }
    }
}
