using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeetleX.Dispatchs
{
    public class SingleThreadDispatcher<T> : IDisposable
    {
        public SingleThreadDispatcher(Action<T> process)
        {
            Process = process;
            mQueue = new System.Collections.Concurrent.ConcurrentQueue<T>();
        }

        private int mRunStatus = 0;

        private int mCount;

        private Action<T> Process;

        private System.Collections.Concurrent.ConcurrentQueue<T> mQueue;

        public Action<T, Exception> ProcessError { get; set; }

        public int Count => mCount;

        public void Enqueue(T item)
        {
            mQueue.Enqueue(item);
            System.Threading.Interlocked.Increment(ref mCount);
            CheckStart();
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

        private void CheckStart()
        {
            if (System.Threading.Interlocked.CompareExchange(ref mRunStatus, 1, 0) == 0)
            {
                if (mCount > 0)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(OnStart, null);
                }
                else
                {
                    System.Threading.Interlocked.Exchange(ref mRunStatus, 0);
                }
            }
        }

        private void OnStart(object state)
        {
            try
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
                        break;
                }
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref mRunStatus, 0);
            }
            CheckStart();
        }

        public void Dispose()
        {
#if(NETSTANDARD2_0)
            while (mQueue.TryDequeue(out T item))
            {

            }
#else
            mQueue.Clear();
#endif
        }
    }

    public class MultiThreadDispatcher<T> : IDisposable
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

        public T Dequeue()
        {
            T item;
            if (mQueue.TryDequeue(out item))
            {
                System.Threading.Interlocked.Decrement(ref mCount);
            }
            return item;
        }

        private void InvokeProcess()
        {
            if (mCount > 0)
            {
                int addthread = Interlocked.Increment(ref mThreads);
                if (addthread == 1)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(OnRun, null);
                }
                else
                {
                    if (addthread > mMaxThreads || mCount < mWaitLength)
                    {
                        Interlocked.Decrement(ref mThreads);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(OnRun, null);
                    }
                }
            }
        }
        private void OnRun()
        {
            OnRun(null);
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


        public void Dispose()
        {
#if (NETSTANDARD2_0)
            while (mQueue.TryDequeue(out T item))
            { }
#else
            mQueue.Clear();
#endif
        }

    }
}
