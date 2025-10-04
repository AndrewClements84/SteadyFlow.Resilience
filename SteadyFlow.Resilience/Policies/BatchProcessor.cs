using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteadyFlow.Resilience.Metrics;

namespace SteadyFlow.Resilience.Policies
{
    public class BatchProcessor<T>
    {
        private readonly int _batchSize;
        private readonly TimeSpan _interval;
        private readonly Func<List<T>, Task> _processor;
        private readonly List<T> _items = new List<T>();
        private readonly Timer _timer;
        private readonly object _lock = new object();
        private readonly IMetricsObserver _observer;

        public BatchProcessor(int batchSize, TimeSpan interval, Func<List<T>, Task> processor, IMetricsObserver observer = null)
        {
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            if (interval.TotalMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(interval));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));

            _batchSize = batchSize;
            _interval = interval;
            _observer = observer;

            _timer = new Timer(async _ => await FlushAsync(), null, interval, interval);
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _items.Add(item);

                if (_items.Count >= _batchSize)
                {
                    _ = FlushAsync();
                }
            }
        }

        private async Task FlushAsync()
        {
            List<T> batch;
            lock (_lock)
            {
                if (_items.Count == 0) return;

                batch = new List<T>(_items);
                _items.Clear();
            }

            await _processor(batch);
            _observer?.OnBatchProcessed(batch.Count);
        }
    }
}
