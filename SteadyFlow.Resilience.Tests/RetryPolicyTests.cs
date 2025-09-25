using SteadyFlow.Resilience.Retry;

namespace SteadyFlow.Resilience.Tests
{
    public class RetryPolicyTests
    {
        [Fact]
        public async Task Should_Retry_Until_Success()
        {
            var attempts = 0;
            var policy = new RetryPolicy(maxRetries: 3);

            var result = await policy.ExecuteAsync(async () =>
            {
                attempts++;
                if (attempts < 3) throw new Exception("Fail");
                return "Success";
            });

            Assert.Equal("Success", result);
            Assert.Equal(3, attempts);
        }

        [Fact]
        public async Task Should_Fail_After_MaxRetries()
        {
            var policy = new RetryPolicy(maxRetries: 2);

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    throw new Exception("Always fails");
                });
            });
        }
    }
}
