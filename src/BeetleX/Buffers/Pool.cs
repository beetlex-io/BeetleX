using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BeetleX.Buffers
{
    class XSpinLock : IDisposable
    {
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
                if (count > 10)
                {
                    Thread.Yield();

                }
                else
                {
                    Thread.SpinWait(4 << count);
                }
                count++;
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

    public class BufferPool : IBufferPool
    {

        public BufferPool()
        {
            Init(1024 * 8, 1024);

        }

        private EventHandler<System.Net.Sockets.SocketAsyncEventArgs> mCompleted;

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
                    mReceiveDefault = new BufferPool();
                return mReceiveDefault;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mPool.Clear();
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
