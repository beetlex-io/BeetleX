using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Buffers
{
    public interface IMemoryBlock
    {
        long ID { get; }

        Span<byte> Bytes { get; }

        IMemoryBlock NextMemory { get; }
    }

    public interface IBuffer : IMemoryBlock
    {

        int Length { get; }

        Memory<byte> Memory { get; }

        byte[] Data { get; }

        bool Eof { get; }

        int Size { get; }

        int Postion { get; set; }

        byte Read();

        void Write(byte data);

        int Write(byte[] buffer, int offset, int count);

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

        BufferPool Pool { get; set; }

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
            mData = new Memory<byte>(mBufferData);
            mID = System.Threading.Interlocked.Increment(ref mIDQueue);
            mSAEA = new SocketAsyncEventArgsX();
            mSAEA.SetBuffer(mBufferData, 0, size);
            mSAEA.BufferX = this;
        }

        private SocketAsyncEventArgsX mSAEA;

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

        public BufferPool Pool { get; set; }

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

        public void Reset()
        {
            mLength = 0;
            mPostion = 0;
            mFree = mSize;
            Next = null;

        }

        public void SetLength(int length)
        {
            mLength = length;
        }

        public int Write(byte[] buffer, int offset, int count)
        {
            Span<byte> dest = AllocateSpan(count);
            if (dest.Length <= 8)
            {
                for (int i = 0; i < dest.Length; i++)
                {
                    dest[i] = buffer[offset + i];
                }
            }
            else
            {
                Span<byte> source = new Span<byte>(buffer, offset, dest.Length);
                source.CopyTo(dest);
            }
            return dest.Length;
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

        public void Free()
        {
            if (Pool != null)
                Pool.Push(this);

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

        private bool mBindIOCompleted = false;

        public bool BindIOCompleted
        {
            get
            {
                return mBindIOCompleted;
            }
        }

        public byte[] Data => mBufferData;

        public int Size => mSize;

        public Span<byte> Bytes => Memory.Span;

        public IMemoryBlock NextMemory => Next;

        public void BindIOEvent(EventHandler<System.Net.Sockets.SocketAsyncEventArgs> e)
        {
            if (!mBindIOCompleted)
            {
                mSAEA.Completed += e;
                mBindIOCompleted = true;
            }
        }


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

        public void AsyncFrom(System.Net.Sockets.Socket socket)
        {
            mSAEA.IsReceive = true;
            mSAEA.UserToken = UserToken;
            mSAEA.SetBuffer(0, mSize);
            if (!socket.ReceiveAsync(mSAEA))
            {

                mSAEA.InvokeCompleted();
            }
        }

        public void AsyncFrom(ISession session)
        {
            mSAEA.IsReceive = true;
            mSAEA.UserToken = UserToken;
            mSAEA.Session = session;
            mSAEA.SetBuffer(0, mSize);
            if (!session.Socket.ReceiveAsync(mSAEA))
            {
                mSAEA.InvokeCompleted();
            }
        }

        public void AsyncTo(System.Net.Sockets.Socket socket)
        {
            mSAEA.IsReceive = false;
            mSAEA.UserToken = UserToken;
            mSAEA.SetBuffer(0, mLength);
            if (!socket.SendAsync(mSAEA))
            {
                mSAEA.InvokeCompleted();
            }
        }

        public void AsyncTo(ISession session)
        {
            mSAEA.IsReceive = false;
            mSAEA.SetBuffer(0, mLength);
            mSAEA.Session = session;
            mSAEA.UserToken = UserToken;
            if (!session.Socket.SendAsync(mSAEA))
            {
                mSAEA.InvokeCompleted();
            }
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
                    IBuffer next = buffer.Next;
                    while (next != null)
                    {
                        Last = next;
                        next = buffer.Next;
                    }
                }
            }
        }


    }

}
