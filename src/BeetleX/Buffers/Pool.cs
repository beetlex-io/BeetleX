using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BeetleX.Buffers
{
    class XSpinLock : IDisposable
    {
        public XSpinLock()
        {
            mIsSingleProcessor = Environment.ProcessorCount == 1;
        }

        private bool mIsSingleProcessor;

        private int mStatus = 0;

        public IDisposable Enter()
        {
            int count = 0;
            while (true)
            {
                if (System.Threading.Interlocked.CompareExchange(ref mStatus, 1, 0) == 0)
                {
                    break;
                }
                if (count > 10 || mIsSingleProcessor)
                {
                    int num = (count >= 10) ? (count - 10) : count;
                    if (num % 20 == 19)
                    {
                        Thread.Sleep(1);
                    }
                    else
                    {
                        if (num % 5 == 4)
                        {
                            Thread.Sleep(0);
                        }
                        else
                        {
                            Thread.Yield();
                        }
                    }
                }
                else
                {
                    Thread.SpinWait(4 << count);
                }
                count = ((count == 2147483647) ? 10 : (count + 1));
            }

            return this;
        }

        public void Exit()
        {
            System.Threading.Interlocked.Exchange(ref mStatus, 0);
        }

        public void Dispose()
        {
            Exit();
        }
    }





    public interface IBufferPool : IDisposable
    {
        IBuffer Pop();
        int Count { get; }
        void Push(IBuffer item);
    }

    public class BufferPoolGroup
    {
        private List<BufferPool> bufferPools = new List<BufferPool>();

        private long mIndex = 1;

        public BufferPoolGroup(int buffrsize, int count, int groups, EventHandler<System.Net.Sockets.SocketAsyncEventArgs> completed)
        {
            for (int i = 0; i < groups; i++)
            {
                bufferPools.Add(new BufferPool(buffrsize, count, completed));
            }
        }

        public BufferPool Next()
        {
            long i = System.Threading.Interlocked.Increment(ref mIndex);
            return bufferPools[(int)(i % bufferPools.Count)];
        }
    }

    public class BufferPool : IBufferPool
    {

        public BufferPool()
        {
            Init(1024 * 8, 1024);

        }

        private EventHandler<System.Net.Sockets.SocketAsyncEventArgs> mCompleted;

        public BufferPool(int size, int count) : this(size, count, null
            )
        { }

        public BufferPool(int size, int count, EventHandler<System.Net.Sockets.SocketAsyncEventArgs> completed)
        {
            mCompleted = completed;
            Init(size, count);
        }

        private int mSize;

        private int mCount;

        private void Init(int size, int count)
        {
            Buffer item;
            mSize = size;
            mCount = count;
            for (int i = 0; i < count; i++)
            {
                item = CreateBuffer();
                mPool.Enqueue(item);
            }
        }

        public int Count
        {
            get
            {
                return mPool.Count;
            }
        }

        private Buffer CreateBuffer()
        {
            Buffer item = new Buffer(mSize);
            item.Pool = this;
            if (mCompleted != null)
                item.BindIOEvent(mCompleted);
            return item;
        }

        private System.Collections.Concurrent.ConcurrentQueue<IBuffer> mPool = new System.Collections.Concurrent.ConcurrentQueue<IBuffer>();

        public IBuffer Pop()
        {
            IBuffer item;
            if (!mPool.TryDequeue(out item))
            {
                item = CreateBuffer();
            }
            item.Reset();
            return item;

        }

        public void Push(IBuffer item)
        {
            mPool.Enqueue(item);
        }

        private static BufferPool mDefault;

        public static BufferPool Default
        {
            get
            {
                if (mDefault == null)
                    mDefault = new BufferPool();
                return mDefault;
            }
        }

        private static BufferPool mReceiveDefault;

        internal static BufferPool ReceiveDefault
        {
            get
            {
                if (mReceiveDefault == null)
                    mReceiveDefault = new BufferPool(1024 * 16, 100);
                return mReceiveDefault;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    IBuffer buffer;
                    while (true)
                    {
                        if (mPool.TryDequeue(out buffer))
                        {
                            if (((Buffer)buffer).GCHandle.IsAllocated)
                            {
                                ((Buffer)buffer).GCHandle.Free();
                            }
                        }
                        else
                            break;
                    }
                }
                disposedValue = true;
            }
        }


        public void Dispose()
        {

            Dispose(true);

        }
        #endregion

    }
}
