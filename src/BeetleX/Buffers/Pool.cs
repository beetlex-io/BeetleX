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
                            int count = 4;
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


        public static int POOL_CLEAR_TIME = 60;

        public static int BUFFER_FREE_TIMEOUT = 600;

        public static int POOL_MINI_SIZE = 1000;

        public static int BUFFER_SIZE = 1024 * 4;

        public static int POOL_SIZE = 1024;

        public static int POOL_MAX_SIZE = 1024 * 20;

        private LinkedList<Buffer> linkBuffers = new LinkedList<Buffer>();

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

        private int mFreeStatusCount = 0;

        internal int FreeStatusCount => System.Threading.Interlocked.Add(ref mFreeStatusCount, 0);

        private long mTimeCount = 0;

        public static Action<IBuffer> BufferRemove { get; set; }

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
            mCleanTime = new Timer(OnClean, null, 1000 * 10, 1000 * 10);
        }

        private System.Threading.Timer mCleanTime;

        private void OnClean(object state)
        {
            try
            {
                if (Count > POOL_MINI_SIZE)
                {
                    System.Threading.Interlocked.Increment(ref mFreeStatusCount);
                }
                else
                {
                    System.Threading.Interlocked.Exchange(ref mFreeStatusCount, 0);
                }
                mTimeCount += 1;
                if (mTimeCount % POOL_CLEAR_TIME == 0)
                {
                    lock (linkBuffers)
                    {
                        var item = linkBuffers.First;
                        while (item != null)
                        {
                            var nitem = item.Next;
                            if (item.Value.Unused)
                            {
                                item.Value.Delete();
                                linkBuffers.Remove(item);
                                BufferRemove?.Invoke(item.Value);
                            }
                            item = nitem;
                        }
                    }
                }
            }
            catch (Exception e_)
            {

            }
        }

        public int Count
        {
            get
            {
                return System.Threading.Interlocked.Add(ref mCount, 0);
            }
        }

        private Buffer CreateBuffer()
        {
            if (mMaxCount > 0 && linkBuffers.Count > mMaxCount)
            {
                throw new BXException("Create buffer error, maximum number of buffer pools!");
            }
            Interlocked.Increment(ref mCount);
            Buffer item = new Buffer(mSize);
            item.Pool = this;
            BufferMonitor.Create(mSize);
            lock (linkBuffers)
                linkBuffers.AddLast(item);
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
            Interlocked.Decrement(ref mCount);
            return item;

        }

        public void Push(IBuffer item)
        {
            Interlocked.Increment(ref mCount);
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
                    if (mCleanTime != null)
                        mCleanTime.Dispose();
                    lock (linkBuffers)
                        linkBuffers.Clear();
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


    public class BufferMonitor
    {

        public static long CreateCount => mCreateCount;

        public static long FreeCount => mFreeCount;

        public static long Size => mSize;

        private static int mCreateCount;

        private static long mSize;

        private static long mFreeCount;

        public static void Create(int size)
        {
            System.Threading.Interlocked.Increment(ref mCreateCount);
            System.Threading.Interlocked.Add(ref mSize, size);
        }

        public static void Free(int size)
        {
            System.Threading.Interlocked.Increment(ref mFreeCount);
            System.Threading.Interlocked.Add(ref mSize, -size);
        }
    }
}
