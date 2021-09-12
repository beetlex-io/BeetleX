using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Buffers
{
#if !NETSTANDARD2_0

    class PipeStreamBufferWriter : IBufferWriter<byte>, IDisposable
    {

        public PipeStream PipeStream { get; set; }

        private byte[] mData;

        private IBuffer mBuffer;

        private int mOffset = 0;

        public void Advance(int count)
        {
            if (mBuffer != null)
            {
                mBuffer.WriteAdvance(count);
                PipeStream.WriteAdvance(count);
            }
            else
            {
                PipeStream.Write(mData, mOffset, count);
                mOffset += count;
            }
        }

        private void OnReturn()
        {
            if (mData != null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(mData);
                mData = null;
                mOffset = 0;
            }
        }

        public void Dispose()
        {
            OnReturn();
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var buffer = PipeStream.GetWriteBuffer();
            if (buffer.FreeSpace >= sizeHint)
            {
                mBuffer = buffer;
                return mBuffer.GetMemory(sizeHint);
            }
            else
            {
                mBuffer = null;
                OnReturn();
                mData = System.Buffers.ArrayPool<byte>.Shared.Rent(sizeHint);
                return new Memory<byte>(mData);
            }

        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var buffer = PipeStream.GetWriteBuffer();
            if (buffer.FreeSpace >= sizeHint)
            {
                mBuffer = buffer;
                return mBuffer.GetSpan(sizeHint);
            }
            else
            {
                mBuffer = null;
                OnReturn();
                mData = System.Buffers.ArrayPool<byte>.Shared.Rent(sizeHint);
                return new Span<byte>(mData);
            }
        }
    }

    public partial class PipeStream
    {

        private PipeStreamBufferWriter mPipeStreamBufferWriter = null;

        public IBufferWriter<byte> CreateBufferWriter()
        {
            if (mPipeStreamBufferWriter == null)
            {
                mPipeStreamBufferWriter = new PipeStreamBufferWriter { PipeStream = this };
            }
            return mPipeStreamBufferWriter;
        }
    }

#endif
}
