using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Text;

namespace BeetleX.Buffers
{
    class SslStreamX : SslStream
    {
        public SslStreamX(IBufferPool pool, Encoding encoding, bool littleEndian, Stream innerStream, bool leaveInnerStreamOpen) : base(innerStream, leaveInnerStreamOpen)
        {
            BufferPool = pool;
            Encoding = encoding;
            LittleEndian = littleEndian;
            mPipeStream = new PipeStream(BufferPool, LittleEndian, Encoding);
            mPipeStream.FlashCompleted = OnWriterFlash;
            mPipeStream.InnerStream = this;
        }

        public SslStreamX(
            IBufferPool pool, Encoding encoding, bool littleEndian, Stream innerStream, RemoteCertificateValidationCallback callback)
            : base(innerStream, false, callback, null)
        {
            BufferPool = pool;
            Encoding = encoding;
            LittleEndian = littleEndian;
            mPipeStream = new PipeStream(BufferPool, LittleEndian, Encoding);
            mPipeStream.FlashCompleted = OnWriterFlash;
            mPipeStream.InnerStream = this;
        }


        public IBufferPool BufferPool { get; set; }

        public Encoding Encoding { get; set; }

        public bool LittleEndian { get; set; }

        private PipeStream mPipeStream;

        private void OnWriterFlash(Buffers.IBuffer data)
        {
            StreamHelper.WriteBuffer(this, data);
        }

        public override void Flush()
        {
            if (mPipeStream != null)
                mPipeStream.Flush();
            base.Flush();
        }

        public PipeStream GetPipeStream()
        {
            return mPipeStream;
        }

        public Exception SyncDataError { get; set; }

        public bool AsyncDataStatus { get; set; }

        public async void SyncData(Action receive)
        {
            while (true)
            {
                var dest = GetPipeStream();
                IBuffer buffer = null;
                try
                {
                    buffer = BufferPoolGroup.DefaultGroup.Next().Pop();
                    int rlen = await ReadAsync(buffer.Data, 0, buffer.Size);
                    if (rlen > 0)
                    {
                        buffer.SetLength(rlen);
                        dest.Import(buffer);
                    }
                    else
                    {
                        buffer.Free();
                        SyncDataError = new BXException("ssl receive null data!");
                        break;
                    }

                }
                catch (Exception e_)
                {
                    SyncDataError = e_;
                    buffer?.Free();
                    break;
                }
                finally
                {
                   receive?.Invoke();

                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (mPipeStream != null)
            {
                mPipeStream.Dispose();
                mPipeStream = null;
            }
        }
    }
}
