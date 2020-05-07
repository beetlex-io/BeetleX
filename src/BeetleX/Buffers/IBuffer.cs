using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Buffers
{
    public interface IMemoryBlock
    {
        long ID { get; }

        byte[] Data { get; }

        Memory<byte> Memory { get; }

        int Length { get; }

        IMemoryBlock NextMemory { get; }
    }

    public interface IBuffer : IMemoryBlock
    {


        bool Eof { get; }

        int Size { get; }

        int Postion { get; set; }

        int FreeSpace { get; }

        byte Read();

        void Write(byte data);

        int Write(byte[] buffer, int offset, int count);

        int ReadFree(int count);

        int Read(byte[] buffer, int offset, int count);

        Span<byte> GetSpan(int size);

        Span<byte> GetSpan();

        bool TryGetSpan(int size, out Span<byte> result);

        Memory<byte> GetMemory(int size);

        Memory<byte> GetMemory();

        Span<byte> Read(int bytes);

        void WriteAdvance(int bytes);

        void ReadAdvance(int bytes);

        void Reset();

        void SetLength(int length);

        bool TryAllocateSpan(int size, out Span<byte> result);

        Span<byte> AllocateSpan(int bytes);

        Memory<byte> AllocateMemory(int bytes);

        IBuffer Next { get; set; }

        void Free();

        IBufferPool Pool { get; set; }

        bool TryWrite(Int16 value);

        bool TryWrite(Int32 value);


        bool TryWrite(Int64 value);


        bool TryWrite(UInt16 value);


        bool TryWrite(UInt32 value);


        bool TryWrite(UInt64 value);


        bool TryRead(out Int16 value);


        bool TryRead(out Int32 value);


        bool TryRead(out Int64 value);


        bool TryRead(out UInt16 value);


        bool TryRead(out UInt32 value);


        bool TryRead(out UInt64 value);

        object UserToken
        {
            get;
            set;
        }

        Action<IBuffer> Completed { get; set; }
    }

    public class Buffer : IBuffer
    {

        static long mIDQueue = 100000000;

        public Buffer(int size)
        {
            mSize = size;
            mLength = 0;
            mPostion = 0;
            mFree = size;
            mBufferData = new byte[size];
            _gcHandle = GCHandle.Alloc(mBufferData, GCHandleType.Pinned);
            mData = new Memory<byte>(mBufferData);
            mID = System.Threading.Interlocked.Increment(ref mIDQueue);
        }

        private GCHandle _gcHandle;

        public GCHandle GCHandle => _gcHandle;

        private byte[] mBufferData;

        private long mID;

        private int mLength = 0;

        private bool mEof;

        private Memory<byte> mData;

        private int mSize;

        private int mPostion;

        private int mFree;

        public int Length => mLength;

        public Memory<byte> Memory => mData;

        public bool Eof
        {
            get { return mEof; }
        }

        public int Postion
        {
            get => mPostion;
            set
            {
                mPostion = value;
                mEof = mPostion >= mLength;
            }
        }

        public IBuffer Next { get; set; }

        public IBufferPool Pool { get; set; }

        public long ID => mID;

        public void WriteAdvance(int bytes)
        {
            mLength += bytes;
            mPostion += bytes;
            mEof = mLength == mSize;
            mFree = mSize - mLength;
        }

        public void ReadAdvance(int bytes)
        {
            mPostion += bytes;
            mEof = mLength == mPostion;
        }

        public bool TryGetMemory(int size, out Span<byte> buffer)
        {
            buffer = null;
            if (mFree >= size)
            {
                buffer = mData.Span.Slice(mPostion, size);
                return true;
            }
            return false;
        }

        public Span<byte> GetSpan(int size)
        {
            if (mFree > size)
            {
                return mData.Span.Slice(mPostion, size);
            }
            else
            {
                return mData.Span.Slice(mPostion, mFree);
            }
        }

        public bool TryGetSpan(int size, out Span<byte> result)
        {
            result = null;
            if (mFree >= size)
            {
                result = mData.Span.Slice(mPostion, size);
                return true;
            }
            return false;
        }

        public Span<byte> GetSpan()
        {
            return mData.Span.Slice(mPostion, mFree);
        }

        public Memory<byte> GetMemory(int size)
        {
            if (mFree > size)
            {
                return mData.Slice(mPostion, size);
            }
            else
            {
                return mData.Slice(mPostion, mFree);
            }
        }

        public Memory<byte> GetMemory()
        {
            return mData.Slice(mPostion, mFree);
        }

        public Span<byte> AllocateSpan(int bytes)
        {
            Span<byte> result = GetSpan(bytes);
            WriteAdvance(result.Length);
            return result;
        }


        public int ReadFree(int count)
        {
            int space = mLength - mPostion;
            int read = space;
            if (space >= count)
                read = count;
            ReadAdvance(read);
            return read;
        }

        public Span<byte> Read(int bytes)
        {
            Span<byte> result;
            int space = mLength - mPostion;
            if (space > bytes)
            {
                result = mData.Span.Slice(mPostion, bytes);
                ReadAdvance(bytes);
            }
            else
            {
                result = mData.Span.Slice(mPostion, space);
                ReadAdvance(space);
            }
            return result;
        }

        private int mUsing = 0;

        private bool mDeleteTag = false;

        public void Reset()
        {
            mLength = 0;
            mPostion = 0;
            mFree = mSize;
            Next = null;
            System.Threading.Interlocked.Exchange(ref mUsing, 1);

        }
        #region delete

        internal long mLastActiveTime;

        internal bool Unused
        {
            get
            {
                return mDeleteTag|| (mUsing == 1 && (TimeWatch.GetElapsedMilliseconds() - mLastActiveTime) > 1000 * BufferPool.BUFFER_FREE_TIMEOUT);
            }
        }

        internal void Delete()
        {
            if (mDeleteTag || (System.Threading.Interlocked.CompareExchange(ref mUsing, -1, 1) == 1))
            {
                BufferMonitor.Free(mSize);
                if (GCHandle.IsAllocated)
                {
                    GCHandle.Free();
                }
                Pool = null;
                Next = null;
            }
        }
        #endregion

        public void Free()
        {
            if (System.Threading.Interlocked.CompareExchange(ref mUsing, 0, 1) == 1)
            {
                if (Pool != null)
                {
                    var pool = Pool as BufferPool;
                    if (pool.FreeStatusCount > 10 && pool.Count > BufferPool.POOL_MINI_SIZE)
                    {
                        mDeleteTag = true;
                    }
                    else
                    {
                        Pool.Push(this);
                        mLastActiveTime = TimeWatch.GetElapsedMilliseconds();
                    }
                }
            }

        }

        public void SetLength(int length)
        {
            mLength = length;
        }

        public unsafe int Write(byte[] buffer, int offset, int count)
        {
            int len = mFree;
            if (mFree > count)
                len = count;
            if (len <= 8)
            {
                for (int i = 0; i < len; i++)
                {
                    mBufferData[i + Postion] = buffer[offset + i];
                }
            }
            else
            {
                System.Buffer.BlockCopy(buffer, offset, mBufferData, mPostion, len);
            }
            WriteAdvance(len);
            return len;
        }



        public int Read(byte[] buffer, int offset, int count)
        {
            Span<byte> source = Read(count);
            if (source.Length <= 8)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    buffer[offset + i] = source[i];
                }
            }
            else
            {
                Span<byte> dest = new Span<byte>(buffer, offset, source.Length);
                source.CopyTo(dest);
            }
            return source.Length;
        }

        public byte Read()
        {
            byte result = mBufferData[mPostion];
            ReadAdvance(1);
            return result;
        }

        public void Write(byte data)
        {
            mBufferData[mPostion] = data;
            WriteAdvance(1);
        }



        public Memory<byte> AllocateMemory(int bytes)
        {
            Memory<byte> result = GetMemory(bytes);
            WriteAdvance(result.Length);
            return result;
        }

        public bool TryAllocateSpan(int size, out Span<byte> result)
        {
            result = null;
            if (mFree >= size)
            {
                result = mData.Span.Slice(mPostion, size);
                WriteAdvance(size);
                return true;
            }
            return false;
        }

        public unsafe bool TryWrite(Int16 value)
        {
            int length = 2;
            if (mFree >= length)
            {
                BitHelper.Write(mBufferData, mPostion, value);
                WriteAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryWrite(Int32 value)
        {
            int length = 4;
            if (mFree >= length)
            {
                BitHelper.Write(mBufferData, mPostion, value);
                WriteAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryWrite(Int64 value)
        {
            int length = 8;
            if (mFree >= length)
            {
                BitHelper.Write(mBufferData, mPostion, value);
                WriteAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryWrite(UInt16 value)
        {
            int length = 2;
            if (mFree >= length)
            {
                BitHelper.Write(mBufferData, mPostion, value);
                WriteAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryWrite(UInt32 value)
        {
            int length = 4;
            if (mFree >= length)
            {
                BitHelper.Write(mBufferData, mPostion, value);
                WriteAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryWrite(UInt64 value)
        {
            int length = 8;
            if (mFree >= length)
            {
                BitHelper.Write(mBufferData, mPostion, value);
                WriteAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryRead(out Int16 value)
        {
            value = 0;
            int length = 2;
            if (mLength - mPostion >= length)
            {
                value = BitHelper.ReadInt16(mBufferData, mPostion);
                ReadAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryRead(out Int32 value)
        {
            value = 0;
            int length = 4;
            if (mLength - mPostion >= length)
            {
                value = BitHelper.ReadInt32(mBufferData, mPostion);
                ReadAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryRead(out Int64 value)
        {
            value = 0;
            int length = 8;
            if (mLength - mPostion >= length)
            {
                value = BitHelper.ReadInt64(mBufferData, mPostion);
                ReadAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryRead(out UInt16 value)
        {
            value = 0;
            int length = 2;
            if (mLength - mPostion >= length)
            {
                value = BitHelper.ReadUInt16(mBufferData, mPostion);
                ReadAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryRead(out UInt32 value)
        {
            value = 0;
            int length = 4;
            if (mLength - mPostion >= length)
            {
                value = BitHelper.ReadUInt32(mBufferData, mPostion);
                ReadAdvance(length);
                return true;
            }
            return false;
        }

        public unsafe bool TryRead(out UInt64 value)
        {
            value = 0;
            int length = 8;
            if (mLength - mPostion >= length)
            {
                value = BitHelper.ReadUInt64(mBufferData, mPostion);
                ReadAdvance(length);
                return true;
            }
            return false;
        }

        public object UserToken
        {
            get;
            set;
        }

        public byte[] Data => mBufferData;

        public int Size => mSize;

        public IMemoryBlock NextMemory => Next;

        public int FreeSpace => mFree;

        public Action<IBuffer> Completed { get; set; }

        public int From(System.Net.Sockets.Socket socket)
        {
            int result = 0;
            result = socket.Receive(mBufferData);
            SetLength(result);
            return result;
        }

        public int To(System.Net.Sockets.Socket socket)
        {
            int count = 0;
            int index = 0;
            while (count < mLength)
            {
                int len = socket.Send(mBufferData, index + count, mLength - count, System.Net.Sockets.SocketFlags.None);
                count += len;
            }
            return count;
        }

        public void AsyncFrom(SocketAsyncEventArgsX argsX, System.Net.Sockets.Socket socket)
        {
            argsX.BufferX = this;
            argsX.AsyncFrom(socket, UserToken, mSize);
        }

        public void AsyncFrom(SocketAsyncEventArgsX argsX, ISession session)
        {
            argsX.BufferX = this;
            argsX.AsyncFrom(session, UserToken, mSize);
        }

        public void AsyncTo(SocketAsyncEventArgsX argsX, System.Net.Sockets.Socket socket)
        {
            argsX.BufferX = this;
            argsX.AsyncTo(socket, UserToken, mLength);
        }

        public void AsyncTo(SocketAsyncEventArgsX argsX, ISession session)
        {
            argsX.BufferX = this;
            argsX.AsyncTo(session, UserToken, mLength);
        }

        internal static void Free(IBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Free();
                IBuffer next = buffer.Next;
                while (next != null)
                {
                    next.Free();
                    next = next.Next;
                }
            }
        }

    }

    struct BufferLink
    {
        public IBuffer First;

        public IBuffer Last;

        public void Import(IBuffer buffer)
        {
            if (buffer != null)
            {
                if (Last == null)
                {
                    Last = buffer;
                    First = buffer;
                }
                else
                {
                    Last.Next = buffer;
                    Last = buffer;
                    IBuffer next = buffer.Next;
                    while (next != null)
                    {
                        Last = next;
                        //next = buffer.Next;
                        next = next.Next;
                    }
                }
            }
        }
    }

}
