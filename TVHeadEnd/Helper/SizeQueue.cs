using System.Collections.Generic;
using System.Threading;
using System;

namespace TVHeadEnd.Helper
{
    public class SizeQueue<T>
    {
        private readonly TimeSpan _timeOut = new TimeSpan(0, 0, 30);
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly int _maxSize;
        public SizeQueue(int maxSize) { _maxSize = maxSize; }

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                while (_queue.Count >= _maxSize)
                {
                    Monitor.Wait(_queue, _timeOut);
                }
                _queue.Enqueue(item);
                if (_queue.Count == 1)
                {
                    // wake up any blocked dequeue
                    Monitor.PulseAll(_queue);
                }
            }
        }

        public T Dequeue()
        {
            lock (_queue)
            {
                while (_queue.Count == 0)
                {
                    Monitor.Wait(_queue, _timeOut);
                }
                T item = _queue.Dequeue();
                if (_queue.Count == _maxSize - 1)
                {
                    // wake up any blocked enqueue
                    Monitor.PulseAll(_queue);
                }
                return item;
            }
        }
    }
}
