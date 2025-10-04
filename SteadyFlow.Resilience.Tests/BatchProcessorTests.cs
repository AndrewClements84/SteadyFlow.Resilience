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
                batchSize: 2,
                interval: TimeSpan.FromSeconds(5),
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                },
                observer: null);

            batcher.Add(1);
            batcher.Add(2);

            await Task.Delay(200); // allow batch flush

            Assert.Contains(1, processed);
            Assert.Contains(2, processed);
        }

        [Fact]
        public async Task Should_Process_Batch_When_Interval_Reached()
        {
            var processed = new List<int>();
            var batcher = new BatchProcessor<int>(
                batchSize: 10,
                interval: TimeSpan.FromMilliseconds(200),
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                },
                observer: null);

            batcher.Add(42);

            await Task.Delay(300); // interval should trigger flush

            Assert.Contains(42, processed);
        }

        [Fact]
        public async Task Should_Report_OnBatchProcessed_To_Observer()
        {
            var processed = new List<int>();
            var observer = new FakeObserver();

            var batcher = new BatchProcessor<int>(
                batchSize: 2,
                interval: TimeSpan.FromMilliseconds(500),
                async batch =>
                {
                    processed.AddRange(batch);
                    await Task.CompletedTask;
                },
                observer);

            batcher.Add(100);
            batcher.Add(200);

            await Task.Delay(300); // allow flush

            Assert.Contains(100, processed);
            Assert.Contains(200, processed);

            Assert.Contains(observer.ObservedEvents, e => e.StartsWith("BatchProcessed:"));
        }

        [Fact]
        public void Constructor_Should_Throw_When_BatchSize_IsZeroOrNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BatchProcessor<int>(
                    batchSize: 0,
                    interval: TimeSpan.FromMilliseconds(100),
                    async batch => await Task.CompletedTask));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BatchProcessor<int>(
                    batchSize: -5,
                    interval: TimeSpan.FromMilliseconds(100),
                    async batch => await Task.CompletedTask));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Interval_IsZeroOrNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BatchProcessor<int>(
                    batchSize: 5,
                    interval: TimeSpan.Zero,
                    async batch => await Task.CompletedTask));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BatchProcessor<int>(
                    batchSize: 5,
                    interval: TimeSpan.FromMilliseconds(-1),
                    async batch => await Task.CompletedTask));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Processor_IsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new BatchProcessor<int>(
                    batchSize: 5,
                    interval: TimeSpan.FromMilliseconds(100),
                    processor: null));
        }
    }
}
