using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeetleX.Dispatchs
{
    class SingleThreadDispatcher<T> : IDisposable
    {
        public SingleThreadDispatcher(Action<T> process)
        {
            Process = process;
            mQueue = new System.Collections.Concurrent.ConcurrentQueue<T>();

        }

        private long mCount = 0;

        private int mRunStatus = 0;

        private Action<T> Process;

        private System.Collections.Concurrent.ConcurrentQueue<T> mQueue;

        public Action<T, Exception> ProcessError { get; set; }

        public void Enqueue(T item)
        {
            mQueue.Enqueue(item);
            System.Threading.Interlocked.Increment(ref mCount);
            InvokeStart();
        }

        private T Dequeue()
        {
            T item;
            if (mQueue.TryDequeue(out item))
            {
                System.Threading.Interlocked.Decrement(ref mCount);
            }
            return item;
        }

        private void InvokeStart()
        {
            if (System.Threading.Interlocked.CompareExchange(ref mRunStatus, 1, 0) == 0)
            {
                if (mCount > 0)
                {
                    ThreadPool.QueueUserWorkItem(OnStart);
                }
                else
                {
                    System.Threading.Interlocked.Exchange(ref mRunStatus, 0);
                }
            }
        }

        private void OnStart(object state)
        {
            while (true)
            {
                T item = Dequeue();
                if (item != null)
                {

                    try
                    {
                        Process(item);
                    }
                    catch (Exception e_)
                    {
                        try
                        {
                            if (ProcessError != null)
                                ProcessError(item, e_);
                        }
                        catch { }
                    }
                }
                else
                {
                    break;
                }
            }
            System.Threading.Interlocked.Exchange(ref mRunStatus, 0);
            InvokeStart();
        }

        public void Start()
        {
            InvokeStart();
        }

        public void Dispose()
        {
            mQueue.Clear();
        }
    }



    class MultiThreadDispatcher<T> : IDisposable
    {

        public MultiThreadDispatcher(Action<T> process, int waitLength, int maxThreads)
        {
            mProcess = process;
            mWaitLength = waitLength;
            mMaxThreads = maxThreads;
        }

        private int mWaitLength;

        private int mMaxThreads;

        private int mThreads;

        private Action<T> mProcess;

        public int WaitLength => mWaitLength;

        public Action<T> Process => mProcess;

        private System.Collections.Concurrent.ConcurrentQueue<T> mQueue = new System.Collections.Concurrent.ConcurrentQueue<T>();

        public int Count => mCount;

        public int Threads => mThreads;

        private int mCount;

        public Action<T, Exception> ProcessError { get; set; }

        public void Enqueue(T item)
        {
            mQueue.Enqueue(item);
            System.Threading.Interlocked.Increment(ref mCount);
            InvokeProcess();
        }

        private void InvokeProcess()
        {
            if (mCount > 0)
            {
                int addthread = Interlocked.Increment(ref mThreads);
                if (addthread == 1)
                {
                     ThreadPool.QueueUserWorkItem(OnRun);
                }
                else
                {
                    if (addthread > mMaxThreads || mCount< mWaitLength)
                    {
                        Interlocked.Decrement(ref mThreads);
                    }
                    else
                    {                     
                        ThreadPool.QueueUserWorkItem(OnRun);
                    }
                }
            }
        }

        private void OnRun(object state)
        {
            while (true)
            {
                T item = Dequeue();
                if (item != null)
                {
                    try
                    {
                        Process(item);
                    }
                    catch (Exception e_)
                    {
                        try
                        {
                            ProcessError?.Invoke(item, e_);
                        }
                        catch { }
                    }
                }
                else
                {
                    break;
                }
            }
            Interlocked.Decrement(ref mThreads);
            InvokeProcess();
        }

        public T Dequeue()
        {
            T item;
            if (mQueue.TryDequeue(out item))
            {
                System.Threading.Interlocked.Decrement(ref mCount);
            }
            return item;
        }

        public void Dispose()
        {
            mQueue.Clear();
        }

    }
}
