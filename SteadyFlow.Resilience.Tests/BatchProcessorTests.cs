using SteadyFlow.Resilience.Policies;

namespace SteadyFlow.Resilience.Tests
{
    public class BatchProcessorTests
    {
        [Fact]
        public async Task Should_Process_Batch_When_Size_Reached()
        {
            var processed = new List<int>();
            var batcher = new BatchProcessor<int>(
                batchSize: 3,
                interval: TimeSpan.FromSeconds(10),
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                });

            batcher.Add(1);
            batcher.Add(2);
            batcher.Add(3);

            await Task.Delay(200); // allow background flush
            Assert.Equal(new[] { 1, 2, 3 }, processed);
        }

        [Fact]
        public async Task Should_Process_Batch_On_Interval()
        {
            var processed = new List<int>();
            var batcher = new BatchProcessor<int>(
                batchSize: 10,
                interval: TimeSpan.FromMilliseconds(200),
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                });

            batcher.Add(42);
            await Task.Delay(300); // let timer trigger

            Assert.Contains(42, processed);
        }
    }
}
