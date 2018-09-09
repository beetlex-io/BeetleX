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
        }

        public SslStreamX(
            IBufferPool pool, Encoding encoding, bool littleEndian, Stream innerStream, RemoteCertificateValidationCallback callback)
            : base(innerStream, false, callback, null)
        {
            BufferPool = pool;
            Encoding = encoding;
            LittleEndian = littleEndian;
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
            if (mPipeStream == null)
            {
                mPipeStream = new PipeStream(BufferPool, LittleEndian, Encoding);
                mPipeStream.FlashCompleted = OnWriterFlash;
                mPipeStream.InnerStream = this;
            }
            StreamHelper.ToPipeStream(this, mPipeStream);
            return mPipeStream;
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
