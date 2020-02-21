using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeetleX.Buffers
{
    public partial class PipeStream : System.IO.Stream, IBinaryReader, IBinaryWriter, IDisposable
    {
        public PipeStream() : this(BufferPoolGroup.DefaultGroup.Next(), true, Encoding.UTF8)
        {

        }

        public PipeStream(bool littelEndian, Encoding coding) : this(BufferPoolGroup.DefaultGroup.Next(), littelEndian, coding)
        {

        }

        public PipeStream(IBufferPool pool) : this(pool, true, Encoding.UTF8) { }

        public PipeStream(IBufferPool pool, bool littelEndian, Encoding coding)
        {
            mPool = pool;
            this.LittleEndian = littelEndian;
            this.mEncoding = coding;
            mSubStringLen = mCacheBlockLen / this.Encoding.GetMaxByteCount(1);
            mDecoder = mEncoding.GetDecoder();
            mMaxCharBytes = this.Encoding.GetMaxByteCount(1);
        }

        private int mMaxCharBytes;

        public System.Net.Sockets.Socket Socket { get; set; }

        private Decoder mDecoder;

        private Encoding mEncoding;

        private const int mCacheBlockLen = 512;

        private byte[] mCacheBlock = new byte[mCacheBlockLen];

        private char[] mCharCacheBlock = new char[mCacheBlockLen];

        private int mSubStringLen = 0;

        private IBufferPool mPool;

        private bool mIsDispose = false;

        private IBuffer mReadFirstBuffer;

        private IBuffer mReadLastBuffer;

        private IBuffer mWriteFirstBuffer;

        private IBuffer mWriteLastBuffer;

        private int mLength = 0;

        public bool LittleEndian { get; set; }

        public Encoding Encoding
        {
            get
            {
                return mEncoding;
            }
            set
            {
                mEncoding = value;
                mSubStringLen = mCacheBlockLen / this.mEncoding.GetMaxByteCount(1);
                mDecoder = mEncoding.GetDecoder();
            }
        }

        public override long Position
        {
            get { return 0; }
            set
            {
                if (value == 0)
                {
                    IBuffer writeCache = GetWriteCacheBufers();
                    Import(writeCache);
                }
            }
        }

        public IBuffer FirstBuffer => mReadFirstBuffer;

        public IBuffer LastBuffer => mReadLastBuffer;

        public override long Length => mLength;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public Stream Stream => this;

        public IBuffer WriteFirstBuffer => mWriteFirstBuffer;

        private IBuffer GetReadBuffer()
        {
            if (mReadFirstBuffer == null)
                return null;
            if (mReadFirstBuffer.Eof)
            {
                IBuffer buf = mReadFirstBuffer;
                mReadFirstBuffer = mReadFirstBuffer.Next;
                buf.Next = null;
                buf.Free();
                if (buf == mReadLastBuffer)
                    mReadLastBuffer = null;
            }
            return mReadFirstBuffer;
        }

        private IBuffer GetWriteBuffer()
        {
            IBuffer result;
            if (mWriteLastBuffer == null)
            {
                result = mPool.Pop();
                mWriteFirstBuffer = result;
                mWriteLastBuffer = result;
            }
            else
            {
                if (mWriteLastBuffer.Eof)
                {
                    result = mPool.Pop();
                    mWriteLastBuffer.Next = result;
                    mWriteLastBuffer = result;
                }
                else
                {
                    result = mWriteLastBuffer;
                }
            }
            return result;
        }

        public IBuffer GetFirstBuffer()
        {
            IBuffer result = FirstBuffer;
            if (result != null)
            {
                if (result.Next == null)
                {
                    mReadFirstBuffer = null;
                    mReadLastBuffer = null;
                    System.Threading.Interlocked.Exchange(ref mLength, 0);
                }
                else
                {
                    System.Threading.Interlocked.Add(ref mLength, -result.Length);
                    mReadFirstBuffer = result.Next;
                    result.Next = null;
                }
            }
            return result;
        }

        public IBuffer GetWriteCacheBufers()
        {
            IBuffer result = mWriteFirstBuffer;
            mWriteFirstBuffer = null;
            mWriteLastBuffer = null;
            mWriteLength = 0;
            //System.Threading.Interlocked.Exchange(ref mWriteLength, 0);
            return result;
        }

        public IBuffer GetReadBuffers()
        {
            IBuffer result = mReadFirstBuffer;
            mReadFirstBuffer = null;
            mReadLastBuffer = null;
            //System.Threading.Interlocked.Exchange(ref mLength, 0);
            mLength = 0;
            return result;
        }

        public void Import(IBuffer buffer)
        {
            buffer.Postion = 0;
            AddReadLength(buffer.Length);
            if (mReadLastBuffer == null)
            {
                mReadLastBuffer = buffer;
                mReadFirstBuffer = buffer;
            }
            else
            {
                mReadLastBuffer.Next = buffer;
                mReadLastBuffer = buffer;
            }
            if (buffer != null && buffer.Next != null)
            {
                Import(buffer.Next);
            }
            else
            {
                if (readCompletionSource != null)
                {
                    int len = Read(readCompletionSource.Buffer, readCompletionSource.Offset, readCompletionSource.Count);
                    readCompletionSource.TrySetResult(len);
                }
            }

        }

        private IBuffer GetAndVerifyReadBuffer()
        {
            IBuffer result = GetReadBuffer();
            if (result == null)
                throw new NullReferenceException("PipeStream no data!");
            return result;
        }

        private IBuffer mStartBuffer;

        private int mStartCacheLength;

        private int mStartPostion;

        public void Start()
        {
            mStartBuffer = GetWriteBuffer();
            mStartCacheLength = CacheLength;
            mStartPostion = mStartBuffer.Postion;
        }

        public IndexOfResult End()
        {
            IndexOfResult result = new IndexOfResult();
            result.Start = mStartBuffer;
            result.StartPostion = mStartPostion;
            int length = this.CacheLength - mStartCacheLength;
            result.Length = length;
            IBuffer nextbuf = mStartBuffer;
            while (nextbuf != null)
            {
                if (nextbuf == mStartBuffer)
                {
                    if (mStartBuffer.Postion - result.StartPostion >= length)
                    {
                        result.End = nextbuf;
                        result.EndPostion = nextbuf.Postion - 1;
                        break;
                    }
                    else
                    {
                        length -= (mStartBuffer.Postion - result.StartPostion);
                    }
                }
                else
                {
                    if (nextbuf.Postion >= length)
                    {
                        result.End = nextbuf;
                        result.EndPostion = nextbuf.Postion - 1;
                        break;
                    }
                    else
                    {
                        length -= nextbuf.Postion;
                    }
                }
                nextbuf = nextbuf.Next;
            }
            mStartBuffer = null;
            return result;
        }

        public IndexOfResult IndexOf(int length)
        {
            IndexOfResult result = new IndexOfResult();
            if (mLength < length)
                return result;
            IBuffer rbuffer = GetReadBuffer();
            result.Start = rbuffer;
            result.Length = length;
            if (rbuffer != null)
                result.StartPostion = rbuffer.Postion;
            while (rbuffer != null)
            {
                int count = rbuffer.Length - rbuffer.Postion;
                if (count >= length)
                {
                    result.End = rbuffer;
                    result.EndPostion = rbuffer.Postion + length - 1;
                    break;
                }
                else
                {
                    length -= count;
                }
                rbuffer = rbuffer.Next;
            }
            return result;
        }

        private bool IndexOf(ref IndexOfResult result, IBuffer buffer)
        {
            int start = buffer.Postion;
            int length = buffer.Length;
            Span<byte> data = buffer.Memory.Span;
            Span<byte> eof = result.EofData;
            int eoflen = eof.Length;
            result.End = buffer;
            int eofindex = result.EofIndex;
            int endpoint = 0;
            for (int i = start; i < length; i++)
            {
                result.Length++;
                if (data[i] == eof[eofindex])
                {
                    eofindex++;
                    endpoint = i;
                    if (eofindex == eoflen)
                    {
                        result.EndPostion = endpoint;
                        return true;
                    }
                }
                else
                {
                    eofindex = 0;
                }
            }
            result.EofIndex = eofindex;
            result.End = null;
            return false;
        }

        public unsafe int IndexOf(byte[] eof, byte[] data)
        {
            int matchLength = eof.Length;
            int bufferSize = data.Length;
            int length;
            Span<byte> eofBlock = stackalloc byte[matchLength];
            for (int i = 0; i < matchLength; i++)
            {
                eofBlock[i] = eof[i];
            }
            int count = 0;
            if (eof == null || mLength < eof.Length)
                return count;
            int match = 0;
            fixed (byte* dest = data)
            {
                byte* destData = dest;
                IBuffer rbuffer = GetReadBuffer();
                while (rbuffer != null)
                {
                    fixed (byte* source = &rbuffer.Data[rbuffer.Postion])
                    {
                        if (count >= bufferSize)
                            return 0;
                        byte* sourceData = source;
                        length = rbuffer.Length;
                        for (int i = 0; i < length; i++)
                        {
                            *destData = *sourceData;
                            count++;
                            if (eofBlock[match] == *sourceData)
                            {
                                match++;
                            }
                            else
                            {
                                match = 0;
                            }
                            if (match == matchLength)
                            {
                                return count;
                            }
                            sourceData++;
                            destData++;
                        }
                    }
                    rbuffer = GetReadBuffer();
                }
            }
            return 0;
        }

        public IndexOfResult IndexOf(Byte[] eof)
        {
            IndexOfResult result = new IndexOfResult();
            result.EofData = eof;
            if (eof == null || mLength < eof.Length)
                return result;
            IBuffer rbuffer = GetReadBuffer();
            if (rbuffer == null)
                return result;
            result.Start = rbuffer;
            result.StartPostion = rbuffer.Postion;
            if (IndexOf(ref result, rbuffer))
            {
                return result;
            }
            rbuffer = rbuffer.Next;
            while (rbuffer != null)
            {
                if (IndexOf(ref result, rbuffer))
                {
                    return result;
                }
                rbuffer = rbuffer.Next;
            }
            return result;
        }

        private int mWriteLength;

        public int CacheLength
        {
            get { return mWriteLength; }
        }

        internal void WriteAdvance(int bytes)
        {
            mWriteLength += bytes;
        }

        internal void ReadAdvance(int bytes)
        {
            mLength -= bytes;
            if (mLength == 0)
                GetReadBuffer();
        }
        internal void AddReadLength(int length)
        {
            mLength += length;
        }

        protected virtual void OnDisposed()
        {
            InnerStream = null;
            Buffer.Free(mReadFirstBuffer);
            mReadFirstBuffer = null;
            mReadLastBuffer = null;
            Buffer.Free(mWriteFirstBuffer);
            mWriteFirstBuffer = null;
            mWriteLastBuffer = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (mFreeState)
            {
                base.Dispose(disposing);
                if (!mIsDispose)
                {
                    OnDisposed();
                    mIsDispose = true;
                }
            }
        }

        public override void Flush()
        {
            if (mFreeState)
            {
                if (FlashCompleted != null)
                {
                    FlashCompleted(GetWriteCacheBufers());
                }
                else
                {
                    IBuffer writeCache = GetWriteCacheBufers();
                    Import(writeCache);
                }
            }
        }

        private bool mFreeState = true;

        public IDisposable LockFree()
        {
            mFreeState = false;
            return new LockFreeImpl(this);
        }

        struct LockFreeImpl : IDisposable
        {
            public LockFreeImpl(PipeStream stream)
            {
                mStream = stream;
            }

            private PipeStream mStream;
            public void Dispose()
            {
                mStream.UnLockFree();
            }
        }

        public void UnLockFree()
        {
            mFreeState = true;
        }

        public MemoryBlockCollection Allocate(int size)
        {
            List<Memory<byte>> blocks = new List<Memory<byte>>();
            WriteAdvance(size);
            while (size > 0)
            {
                IBuffer buffer = GetWriteBuffer();
                Memory<byte> item = buffer.AllocateMemory(size);
                blocks.Add(item);
                size -= item.Length;
                if (size == 0)
                    break;
            }

            return new MemoryBlockCollection(blocks);
        }

        public Stream InnerStream { get; set; }


        public Action<IBuffer> FlashCompleted
        {
            get;
            set;
        }


        #region async write
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!SSLConfirmed && SSL)
            {
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }
            else
            {
                Write(buffer, offset, count);
                return Task.CompletedTask;
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!SSLConfirmed && SSL)
            {
                return base.WriteAsync(buffer, cancellationToken);
            }
            else
            {
                Write(buffer.Span);
                return new ValueTask();
            }
        }
        #endregion


        #region read


        public bool SSL { get; set; } = false;

        public bool SSLConfirmed { get; set; }

        private ReadTaskCompletionSource readCompletionSource;

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!SSLConfirmed && SSL)
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }
            else
            {
                if (Length > 0)
                {
                    readCompletionSource = null;
                    var len = Read(buffer, offset, count);
                    return Task.FromResult(len);
                }
                else
                {
                    readCompletionSource = new ReadTaskCompletionSource();
                    readCompletionSource.Buffer = buffer;
                    readCompletionSource.Offset = offset;
                    readCompletionSource.Count = count;
                    return readCompletionSource.Task;
                }
            }
        }

        class ReadTaskCompletionSource : TaskCompletionSource<int>
        {
            public byte[] Buffer { get; set; }

            public int Offset { get; set; }

            public int Count { get; set; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int recount = 0;
            IBuffer sbuffer = GetReadBuffer();
            while (sbuffer != null)
            {
                int rc = sbuffer.Read(buffer, offset, count);
                offset += rc;
                count -= rc;
                recount += rc;
                if (count == 0)
                    break;
                sbuffer = GetReadBuffer();
            }
            ReadAdvance(recount);
            return recount;
        }

        public override int ReadByte()
        {
            IBuffer sbuffer = GetAndVerifyReadBuffer();
            byte result = sbuffer.Read();
            ReadAdvance(1);
            return result;
        }

        public bool ReadBool()
        {
            IBuffer sbuffer = GetAndVerifyReadBuffer();
            byte result = sbuffer.Read();
            ReadAdvance(1);
            return result != 0;
        }

        public char ReadChar()
        {
            return (char)this.ReadInt16();
        }

        public DateTime ReadDateTime()
        {
            return new DateTime(this.ReadInt64());
        }

        public unsafe float ReadFloat()
        {
            int num;
            num = this.ReadInt32();
            return *(float*)(&num);
        }

        public unsafe double ReadDouble()
        {
            long num;
            num = this.ReadInt64();
            return *(double*)(&num);
        }

        public short ReadInt16()
        {
            short result = 0;
            IBuffer rbuffer = GetAndVerifyReadBuffer();
            if (!rbuffer.TryRead(out result))
            {
                var len = Read(this.mCacheBlock, 0, 2);
                result = BitConverter.ToInt16(this.mCacheBlock, 0);
            }
            else
            {
                ReadAdvance(2);
            }
            if (!LittleEndian)
                result = BitHelper.SwapInt16(result);
            return result;
        }

        public int ReadInt32()
        {
            int result = 0;
            IBuffer rbuffer = GetAndVerifyReadBuffer();
            if (!rbuffer.TryRead(out result))
            {
                var len = Read(this.mCacheBlock, 0, 4);
                result = BitConverter.ToInt32(this.mCacheBlock, 0);
            }
            else
            {
                ReadAdvance(4);
            }
            if (!LittleEndian)
                result = BitHelper.SwapInt32(result);
            return result;
        }

        public unsafe long ReadInt64()
        {
            long result = 0;
            IBuffer rbuffer = GetAndVerifyReadBuffer();
            if (!rbuffer.TryRead(out result))
            {
                var len = Read(this.mCacheBlock, 0, 8);
                result = BitConverter.ToInt64(this.mCacheBlock, 0);
            }
            else
            {
                ReadAdvance(8);
            }
            if (!LittleEndian)
                result = BitHelper.SwapInt64(result);
            return result;
        }

        public string ReadLine()
        {
            string result;
            TryReadLine(out result);
            return result;
        }

        public string ReadShortUTF()
        {
            short len = ReadInt16();
            return ReadString(len);
        }

        public string ReadUTF()
        {
            int len = ReadInt32();
            return ReadString(len);
        }

        public string ReadString(int length)
        {
            if (length == 0)
                return string.Empty;
            IBuffer rbuffer;
            Span<byte> data;
            Span<char> charSpan = mCharCacheBlock.AsSpan();
            if (length < mCacheBlockLen)
            {
                rbuffer = GetAndVerifyReadBuffer();
                int freelen = rbuffer.Length - rbuffer.Postion;
                if (freelen > length)
                {
                    data = rbuffer.Read(length);
                    ReadAdvance(length);
                    var l = mDecoder.GetChars(data, charSpan, false);
                    return new string(charSpan.Slice(0, l));
                }
            }
            StringBuilder sb = new StringBuilder();
            int rlen = 0;
            while (length > 0)
            {
                if (length > mCacheBlockLen)
                    rlen = mCacheBlockLen;
                else
                    rlen = length;
                rbuffer = GetAndVerifyReadBuffer();
                int freelen = rbuffer.Length - rbuffer.Postion;
                if (freelen > rlen)
                {
                    data = rbuffer.Read(rlen);
                }
                else
                {
                    data = rbuffer.Read(freelen);
                }
                ReadAdvance(data.Length);
                length -= data.Length;
                var l = mDecoder.GetChars(data, charSpan, false);
                if (l > 0)
                {
                    sb.Append(charSpan.Slice(0, l));
                }
            }

            return sb.ToString();
        }

        public string ReadToEnd()
        {
            return ReadString(mLength);
        }

        public ushort ReadUInt16()
        {
            ushort result = 0;
            IBuffer rbuffer = GetAndVerifyReadBuffer();
            if (!rbuffer.TryRead(out result))
            {
                var len = Read(this.mCacheBlock, 0, 4);
                result = BitConverter.ToUInt16(this.mCacheBlock, 0);
            }
            else
            {
                ReadAdvance(2);
            }
            if (!LittleEndian)
                result = BitHelper.SwapUInt16(result);
            return result;
        }


        public void ReadFree(int count)
        {
            int free = 0;
            IBuffer sbuffer = GetReadBuffer();
            while (sbuffer != null)
            {
                int rc = sbuffer.ReadFree(count);
                free += rc;
                count -= rc;
                if (count == 0)
                    break;
                sbuffer = GetReadBuffer();
            }
            ReadAdvance(free);
        }

        public uint ReadUInt32()
        {
            uint result = 0;
            IBuffer rbuffer = GetAndVerifyReadBuffer();
            if (!rbuffer.TryRead(out result))
            {
                var len = Read(this.mCacheBlock, 0, 4);
                result = BitConverter.ToUInt32(this.mCacheBlock, 0);
            }
            else
            {
                ReadAdvance(4);
            }
            if (!LittleEndian)
                result = BitHelper.SwapUInt32(result);
            return result;
        }

        public ulong ReadUInt64()
        {
            ulong result = 0;
            IBuffer rbuffer = GetAndVerifyReadBuffer();
            if (!rbuffer.TryRead(out result))
            {
                var len = Read(this.mCacheBlock, 0, 8);
                result = BitConverter.ToUInt64(this.mCacheBlock, 0);
            }
            else
            {
                ReadAdvance(8);
            }
            if (!LittleEndian)
                result = BitHelper.SwapUInt64(result);
            return result;
        }

        public bool TryRead(int count)
        {
            return mLength >= count;
        }

        public bool TryReadLine(out string value, bool returnEof = false)
        {
            return TryReadWith("\r\n", out value, returnEof);
        }

        public bool TryReadWith(string eof, out string value, bool returnEof = false)
        {
            return TryReadWith(Encoding.GetBytes(eof), out value, returnEof);
        }

        public bool TryReadWith(byte[] eof, out string value, bool returnEof = false)
        {
            value = null;
            IndexOfResult result = IndexOf(eof);
            int length = result.Length;
            if (result.End != null)
            {
                if (result.Start.ID == result.End.ID)
                {
                    char[] charSpan = mCharCacheBlock;
                    if (result.Length < mCacheBlockLen)
                    {
                        var len = Encoding.GetChars(result.Start.Data, result.StartPostion, length, charSpan, 0);
                        if (returnEof)
                            value = new string(charSpan, 0, len);
                        else
                            value = new string(charSpan, 0, len - eof.Length);
                        ReadFree(length);
                    }
                    else
                    {
                        value = ReadString(result.Length);
                    }
                }
                else
                {
                    if (returnEof)
                    {
                        value = ReadString(result.Length);
                    }
                    else
                    {
                        value = ReadString(result.Length - eof.Length);
                        Read(eof, 0, eof.Length);
                    }
                }

                return true;
            }
            return false;
        }

        #endregion

        #region write

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAdvance(count);
            while (count > 0)
            {
                IBuffer wbuffer = GetWriteBuffer();
                int len = wbuffer.Write(buffer, offset, count);
                offset += len;
                count -= len;
            }

        }

        public override void WriteByte(byte value)
        {
            Write(value);
        }

        public void Write(byte value)
        {
            IBuffer wbuffer = GetWriteBuffer();
            wbuffer.Write(value);
            WriteAdvance(1);
        }

        public void Write(bool value)
        {
            IBuffer wbuffer = GetWriteBuffer();
            if (value)
            {
                wbuffer.Write((byte)1);
            }
            else
            {
                wbuffer.Write((byte)0);
            }
            WriteAdvance(1);

        }

        public unsafe void Write(short value)
        {
            if (!LittleEndian)
            {
                value = BitHelper.SwapInt16(value);
            }
            IBuffer wbuffer = GetWriteBuffer();
            if (!wbuffer.TryWrite(value))
            {

                BitHelper.Write(mCacheBlock, 0, value);
                Write(this.mCacheBlock, 0, 2);
            }
            else
            {
                WriteAdvance(2);
            }
        }

        public unsafe void Write(int value)
        {
            if (!LittleEndian)
            {
                value = BitHelper.SwapInt32(value);
            }
            IBuffer wbuffer = GetWriteBuffer();
            if (!wbuffer.TryWrite(value))
            {
                BitHelper.Write(mCacheBlock, 0, value);
                Write(this.mCacheBlock, 0, 4);
            }
            else
            {
                WriteAdvance(4);
            }
        }

        public unsafe void Write(long value)
        {
            if (!LittleEndian)
            {
                value = BitHelper.SwapInt64(value);
            }
            IBuffer wbuffer = GetWriteBuffer();
            if (!wbuffer.TryWrite(value))
            {
                BitHelper.Write(mCacheBlock, 0, value);
                Write(this.mCacheBlock, 0, 8);
            }
            else
            {
                WriteAdvance(8);
            }
        }

        public unsafe void Write(ushort value)
        {
            if (!LittleEndian)
            {
                value = BitHelper.SwapUInt16(value);
            }
            IBuffer wbuffer = GetWriteBuffer();
            if (!wbuffer.TryWrite(value))
            {
                BitHelper.Write(mCacheBlock, 0, value);
                Write(this.mCacheBlock, 0, 1);
            }
            else
            {
                WriteAdvance(2);
            }
        }

        public unsafe void Write(uint value)
        {
            if (!LittleEndian)
            {
                value = BitHelper.SwapUInt32(value);
            }
            IBuffer wbuffer = GetWriteBuffer();
            if (!wbuffer.TryWrite(value))
            {
                BitHelper.Write(mCacheBlock, 0, value);
                Write(this.mCacheBlock, 0, 4);
            }
            else
            {
                WriteAdvance(4);
            }
        }

        public unsafe void Write(ulong value)
        {
            if (!LittleEndian)
            {
                value = BitHelper.SwapUInt64(value);
            }
            IBuffer wbuffer = GetWriteBuffer();
            if (!wbuffer.TryWrite(value))
            {
                BitHelper.Write(mCacheBlock, 0, value);
                Write(this.mCacheBlock, 0, 8);
            }
            else
            {
                WriteAdvance(8);
            }
        }

        public void Write(DateTime value)
        {
            Write(value.Ticks);
        }

        public unsafe void Write(char value)
        {
            short num = (short)value;
            Write(num);
        }

        public unsafe void Write(float value)
        {
            int num = *(int*)(&value);
            Write(num);
        }

        public unsafe void Write(double value)
        {
            long num = *(long*)(&value);
            Write(num);
        }

        public int Write(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            int cvalueLen = value.Length;
            int index = 0;
            int encodingLen = 0;
            int count = 0;
            int ensize = value.Length * mMaxCharBytes;
            IBuffer buffer = GetWriteBuffer();
            if (buffer.FreeSpace > ensize)
            {
                var len = Encoding.GetBytes(value, index, value.Length, buffer.Data, buffer.Postion);
                buffer.WriteAdvance(len);
                WriteAdvance(len);
                return len;
            }
            while (cvalueLen > 0)
            {
                if (cvalueLen > mSubStringLen)
                    encodingLen = mSubStringLen;
                else
                    encodingLen = cvalueLen;
                var len = Encoding.GetBytes(value, index, encodingLen, mCacheBlock, 0);
                count += len;
                cvalueLen -= encodingLen;
                index += encodingLen;
                Write(mCacheBlock, 0, len);
            }
            return count;
        }

        public int Write(string value, params object[] parameters)
        {
            return Write(string.Format(value, parameters));
        }

        public int WriteLine(string value)
        {
            return Write(value + "\r\n");
        }

        public int WriteLine(string value, params object[] parameters)
        {
            return Write(string.Format(value, parameters) + "\r\n");
        }

        public unsafe void WriteShortUTF(string value)
        {
            Span<byte> header = null;
            MemoryBlockCollection? mbc = null;
            IBuffer wbuffer = GetWriteBuffer();
            if (wbuffer.TryAllocateSpan(2, out header))
            {
                WriteAdvance(2);
            }
            else
            {
                mbc = this.Allocate(2);
            }
            short len = (short)Write(value);
            if (!LittleEndian)
                len = BitHelper.SwapInt16(len);
            if (mbc != null)
            {
                mbc.Value.Full(len);
            }
            else
            {
                BitHelper.Write(header, len);
            }

        }

        public unsafe void WriteUTF(string value)
        {
            Span<byte> header = null;
            MemoryBlockCollection? mbc = null;
            IBuffer wbuffer = GetWriteBuffer();
            if (wbuffer.TryAllocateSpan(4, out header))
            {
                WriteAdvance(4);
            }
            else
            {
                mbc = this.Allocate(4);
            }
            int len = Write(value);
            if (!LittleEndian)
                len = BitHelper.SwapInt32(len);
            if (mbc != null)
            {
                mbc.Value.Full(len);
            }
            else
            {
                BitHelper.Write(header, len);
            }
        }

        #endregion

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {

        }


        #region pipistream intersocket send receive methods

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            Socket streamSocket = Socket;
            if (streamSocket == null)
            {
                throw new BXException("PipeStream Socket is null!");
            }
            IAsyncResult asyncResult = streamSocket.BeginReceive(buffer, offset, size, SocketFlags.None, callback, state);
            return asyncResult;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            Socket streamSocket = this.Socket;
            if (streamSocket == null)
            {
                throw new BXException("PipeStream Socket is null!");
            }

            int num = streamSocket.EndReceive(asyncResult);
            return num;
        }



        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {

            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            Socket streamSocket = this.Socket;
            if (streamSocket == null)
            {
                throw new BXException("PipeStream Socket is null!");
            }
            IAsyncResult asyncResult = streamSocket.BeginSend(buffer, offset, size, SocketFlags.None, callback, state);
            return asyncResult;

        }

        public override void EndWrite(IAsyncResult asyncResult)
        {

            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            Socket streamSocket = this.Socket;
            if (streamSocket == null)
            {
                throw new BXException("PipeStream Socket is null!");
            }
            streamSocket.EndSend(asyncResult);
        }

        #endregion


    }
}
