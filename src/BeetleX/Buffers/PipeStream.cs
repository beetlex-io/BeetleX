using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace BeetleX.Buffers
{
    public class PipeStream : System.IO.Stream, IBinaryReader, IBinaryWriter, IDisposable
    {
        public PipeStream() : this(BufferPool.Default, true, Encoding.UTF8)
        {

        }

        public PipeStream(bool littelEndian, Encoding coding) : this(BufferPool.Default, littelEndian, coding)
        {

        }

        public PipeStream(IBufferPool pool, bool littelEndian, Encoding coding)
        {
            mPool = pool;
            this.LittleEndian = littelEndian;
            this.mEncoding = coding;
            mSubStringLen = mCacheBlockLen / this.Encoding.GetMaxByteCount(1);
            mDecoder = mEncoding.GetDecoder();
        }

        public System.Net.Sockets.Socket Socket { get; set; }

        private Decoder mDecoder;

        private Encoding mEncoding;

        private const int mCacheBlockLen = 256;

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

        private XSpinLock mLockBuffer = new XSpinLock();

        private IBuffer GetReadBuffer()
        {
            if (mReadFirstBuffer == null)
                return null;
            if (mReadFirstBuffer.Eof)
            {
                using (mLockBuffer.Enter())
                {
                    IBuffer buf = mReadFirstBuffer;
                    mReadFirstBuffer = mReadFirstBuffer.Next;
                    buf.Next = null;
                    buf.Free();
                    if (buf == mReadLastBuffer)
                        mReadLastBuffer = null;
                }
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
            using (mLockBuffer.Enter())
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
        }

        public IBuffer GetWriteCacheBufers()
        {
            IBuffer result = mWriteFirstBuffer;
            mWriteFirstBuffer = null;
            mWriteLastBuffer = null;
            System.Threading.Interlocked.Exchange(ref mWriteLength, 0);
            return result;
        }

        public IBuffer GetReadBuffers()
        {
            using (mLockBuffer.Enter())
            {
                IBuffer result = mReadFirstBuffer;
                mReadFirstBuffer = null;
                mReadLastBuffer = null;
                System.Threading.Interlocked.Exchange(ref mLength, 0);
                return result;
            }
        }

        public void Import(IBuffer buffer)
        {
            using (mLockBuffer.Enter())
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
            }
            if (buffer != null && buffer.Next != null)
                Import(buffer.Next);
        }

        private IBuffer GetAndVerifyReadBuffer()
        {
            IBuffer result = GetReadBuffer();
            if (result == null)
                throw new NullReferenceException("PipeStream no data!");
            return result;
        }

        public IndexOfResult indexOf(Byte[] eof)
        {
            int eoflen = eof.Length;
            int index = 0;
            int len = 0;
            IndexOfResult result = new IndexOfResult();
            if (mLength < eoflen)
                return result;
            IBuffer rbuffer = GetReadBuffer();
            while (rbuffer != null)
            {
                ReadOnlySpan<byte> data = rbuffer.Memory.Span;
                for (int i = rbuffer.Postion; i < rbuffer.Length; i++)
                {
                    len++;
                    if (data[i] == eof[index])
                    {
                        index++;
                    }
                    else
                    {
                        index = 0;
                    }
                    if (index == eoflen)
                    {
                        result.EndPostion = i;
                        result.EndBufferID = rbuffer.ID;
                        result.Length = len;
                        return result;
                    }
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

        public void WriteAdvance(int bytes)
        {
            System.Threading.Interlocked.Add(ref mWriteLength, bytes);
        }

        public void ReadAdvance(int bytes)
        {
            System.Threading.Interlocked.Add(ref mLength, -bytes);
            if (mLength == 0)
                GetReadBuffer();
        }
        public void AddReadLength(int length)
        {
            System.Threading.Interlocked.Add(ref mLength, length);
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
            base.Dispose(disposing);
            if (!mIsDispose)
            {
                OnDisposed();
                mIsDispose = true;
            }
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

        public System.IO.Stream InnerStream { get; set; }


        public Action<IBuffer> FlashCompleted
        {
            get;
            set;
        }

        public override void Flush()
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



        #region read

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
            StringBuilder sb = new StringBuilder();
            Span<char> charSpan = mCharCacheBlock.AsSpan();

            int rlen = 0;
            while (length > 0)
            {
                if (length > mCacheBlockLen)
                    rlen = mCacheBlockLen;
                else
                    rlen = length;
                IBuffer rbuffer = GetAndVerifyReadBuffer();
                Span<byte> data;
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
            IndexOfResult result = indexOf(eof);
            if (result.Length > 0)
            {
                if (returnEof)
                {
                    value = ReadString(result.Length);
                }
                else
                {
                    value = ReadString(result.Length - eof.Length);
                    //ReadString(eof.Length);
                    Read(eof, 0, eof.Length);
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
            int result = 0;
            ReadOnlySpan<char> valueSpan = value.AsSpan();
            Span<byte> cacheSpan = mCacheBlock.AsSpan();
            int cvalueLen = valueSpan.Length;
            int index = 0;
            int encodingLen = 0;
            while (cvalueLen > 0)
            {
                if (cvalueLen > mSubStringLen)
                    encodingLen = mSubStringLen;
                else
                    encodingLen = cvalueLen;
                IBuffer wbuffer = GetWriteBuffer();
                Span<byte> wspan = null;
                if (wbuffer.TryGetSpan(mCacheBlockLen, out wspan))
                {
                    var len = this.Encoding.GetBytes(valueSpan.Slice(index, encodingLen), wspan);
                    cvalueLen -= encodingLen;
                    index += encodingLen;
                    result += len;
                    wbuffer.WriteAdvance(len);
                    WriteAdvance(len);
                }
                else
                {
                    var len = this.Encoding.GetBytes(valueSpan.Slice(index, encodingLen), cacheSpan);
                    cvalueLen -= encodingLen;
                    index += encodingLen;
                    result += len;
                    Write(mCacheBlock, 0, len);
                }
            }
            return result;
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
