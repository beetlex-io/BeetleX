using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeetleX.Dispatchs
{
    class Dispatcher<T> : IDisposable
    {
        public Dispatcher(Action<T> process)
        {
            Process = process;
            mQueue = new System.Collections.Concurrent.ConcurrentQueue<T>();

        }

        private long mCount = 0;

        private bool mEnabled = true;

        private Action<T> Process;

        private System.Collections.Concurrent.ConcurrentQueue<T> mQueue;

        public void Enqueue(T item)
        {
            mQueue.Enqueue(item);
        }

        private T Dequeue()
        {
            T item;
            mQueue.TryDequeue(out item);
            return item;
        }

        private void OnStart(object state)
        {
            while (mEnabled)
            {
                T item = Dequeue();
                if (item != null)
                {
                    mCount = 0;
                    try
                    {
                        Process(item);
                    }
                    catch { }
                }
                else
                {
                    if (mCount > 10)
                    {
                        Thread.Sleep(10);
                        mCount = 0;
                    }
                    else
                    {
                        Thread.Yield();
                    }
                    mCount++;
                }
            }
        }

        public void Start()
        {
           ThreadPool.QueueUserWorkItem(OnStart);
        }

        public void Dispose()
        {
            mEnabled = false;
            mQueue.Clear();
        }
    }
}
