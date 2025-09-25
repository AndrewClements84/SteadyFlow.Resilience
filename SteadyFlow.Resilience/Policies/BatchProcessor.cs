using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SteadyFlow.Resilience.Policies
{
    public class BatchProcessor<T>
    {
        private readonly int _batchSize;
        private readonly TimeSpan _interval;
        private readonly Func<List<T>, Task> _processBatch;
        private readonly List<T> _items = new List<T>();
        private readonly object _lock = new object();
        private readonly Timer _timer;

        public BatchProcessor(int batchSize, TimeSpan interval, Func<List<T>, Task> processBatch)
        {
            _batchSize = batchSize;
            _interval = interval;
            _processBatch = processBatch;

            _timer = new Timer(async _ => await FlushAsync(), null, _interval, _interval);
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

        public async Task FlushAsync()
        {
            List<T> batch;
            lock (_lock)
            {
                if (_items.Count == 0) return;
                batch = _items.ToList();
                _items.Clear();
            }
            await _processBatch(batch);
        }
    }
}
