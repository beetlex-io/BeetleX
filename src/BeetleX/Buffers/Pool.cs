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

        public BufferPoolGroup(int buffrsize, int count, int maxCount, int groups)
        {
            for (int i = 0; i < groups; i++)
            {
                bufferPools.Add(new BufferPool(buffrsize, count, maxCount));
            }
        }

        public IBufferPool Next()
        {
            long i = System.Threading.Interlocked.Increment(ref mIndex);
            return bufferPools[(int)(i % bufferPools.Count)];
        }

        private static BufferPoolGroup mDefaultGroup;

        public static BufferPoolGroup DefaultGroup
        {
            get
            {
                if (mDefaultGroup == null)
                {
                    lock (typeof(BufferPoolGroup))
                    {
                        if (mDefaultGroup == null)
                        {
                            int count = Math.Min(Environment.ProcessorCount, 16);
                            int poolSize = BufferPool.POOL_SIZE / count;
                            int poolMaxSize = BufferPool.POOL_MAX_SIZE / count;
                            mDefaultGroup = new BufferPoolGroup(BufferPool.BUFFER_SIZE, poolSize, poolMaxSize, count);
                        }
                    }
                }
                return mDefaultGroup;
            }
        }
    }

    public class BufferPool : IBufferPool
    {

        public static int BUFFER_SIZE = 1024 * 4;

        public static int POOL_SIZE = 1024;

        public static int POOL_MAX_SIZE = 1024 * 20;

        public BufferPool()
        {
            Init(BUFFER_SIZE, POOL_SIZE, POOL_MAX_SIZE);
        }

        public BufferPool(int size, int count, int maxCount)
        {
            Init(size, count, maxCount);
        }

        private int mSize;

        private int mCount;

        private int mMaxCount;

        private void Init(int size, int count, int maxCount)
        {
            mMaxCount = maxCount;
            Buffer item;
            mSize = size;
            for (int i = 0; i < count; i++)
            {
                item = CreateBuffer();
                mPool.Push(item);
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
            mCount = System.Threading.Interlocked.Increment(ref mCount);
            if (mMaxCount > 0 && mCount > mMaxCount)
            {
                throw new BXException("Create buffer error, maximum number of buffer pools!");
            }
            Buffer item = new Buffer(mSize);
            item.Pool = this;
            return item;
        }

        private System.Collections.Concurrent.ConcurrentStack<IBuffer> mPool = new System.Collections.Concurrent.ConcurrentStack<IBuffer>();

        public IBuffer Pop()
        {

            IBuffer item;
            if (!mPool.TryPop(out item))
            {
                item = CreateBuffer();
            }
            item.Reset();
            return item;

        }

        public void Push(IBuffer item)
        {
            mPool.Push(item);
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
                        if (mPool.TryPop(out buffer))
                        {
                            buffer.Pool = null;
                            buffer.Next = null;
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

   public class PrivateBufferPool : IBufferPool
    {
        private Stack<IBuffer> mPool = new Stack<IBuffer>();

        private int mCount;

        private int mMaxCount;

        public int Count => mCount;

        private int mSize;

        public PrivateBufferPool(int bufferSize,int MaxSize)
        {
            mSize = bufferSize;
            mMaxCount = MaxSize/bufferSize+1;
            var item = CreateBuffer();
            Push(item);
        }

        public void Dispose()
        {
            while (true)
            {
                if (mPool.TryPop(out IBuffer buffer))
                {
                    buffer.Pool = null;
                    buffer.Next = null;
                    if (buffer is Buffer memory)
                    {
                        if (memory.GCHandle.IsAllocated)
                        {
                            memory.GCHandle.Free();
                        }
                    }
                }
                else
                    break;
            }
        }

        private Buffer CreateBuffer()
        {
            mCount++;
            if (mMaxCount > 0 && mCount > mMaxCount)
            {
                throw new BXException("Create buffer error, maximum number of buffer pools!");
            }
            Buffer item = new Buffer(mSize);
            item.Pool = this;

            return item;
        }

        public IBuffer Pop()
        {
            if (!mPool.TryPop(out IBuffer item))
            {
                item = CreateBuffer();
            }
            item.Reset();
            return item;
        }

        public void Push(IBuffer item)
        {
            mPool.Push(item);
        }
    }

}
