using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeetleX.Buffers;
using BeetleX.Clients;
using BeetleX.EventArgs;

namespace BeetleX.Packets
{
    public abstract class EofBasePacket : IPacket
    {
        public EventHandler<PacketDecodeCompletedEventArgs> Completed { get; set; }

        public abstract IPacket Clone();

        public abstract byte[] EofData { get; }

        private PacketDecodeCompletedEventArgs mCompletedArgs = new PacketDecodeCompletedEventArgs();

        private int mSize;

        protected int CurrentSize => mSize;

        protected abstract object OnRead(ISession session, PipeStream stream);

        public void Decode(ISession session, System.IO.Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
        START:
            object data;
            var index = pstream.IndexOf(EofData);
            if (index.End != null)
            {
                mSize = index.Length - EofData.Length;
                data = OnRead(session, pstream);
                Completed?.Invoke(this, mCompletedArgs.SetInfo(session, data));
                pstream.ReadFree(EofData.Length);
                goto START;
            }
        }

        public virtual void Dispose()
        {

        }

        protected abstract void OnWrite(ISession session, object data, PipeStream stream);

        private void OnEncode(ISession session, object data, System.IO.Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            OnWrite(session, data, pstream);
            pstream.Write(EofData,0,EofData.Length);

        }

        public byte[] Encode(object data, IServer server)
        {
            byte[] result = null;
            using (Buffers.PipeStream stream = new PipeStream(server.SendBufferPool.Next(), server.Options.LittleEndian, server.Options.Encoding))
            {
                OnEncode(null, data, stream);
                stream.Position = 0;
                result = new byte[stream.Length];
                stream.Read(result, 0, result.Length);
            }
            return result;
        }

        public ArraySegment<byte> Encode(object data, IServer server, byte[] buffer)
        {
            using (Buffers.PipeStream stream = new PipeStream(server.SendBufferPool.Next(), server.Options.LittleEndian, server.Options.Encoding))
            {
                OnEncode(null, data, stream);
                stream.Position = 0;
                int count = (int)stream.Length;
                stream.Read(buffer, 0, count);
                return new ArraySegment<byte>(buffer, 0, count);
            }
        }

        public void Encode(object data, ISession session, System.IO.Stream stream)
        {
            OnEncode(session, data, stream);
        }
    }

    public abstract class LineBasePacket : EofBasePacket
    {
        public LineBasePacket()
        {
            mEof = Encoding.ASCII.GetBytes("\r\n");
        }

        private byte[] mEof;

        public override byte[] EofData => mEof;
    }

    public class StringLinePacket : LineBasePacket
    {
        public override IPacket Clone()
        {
            return new StringLinePacket();
        }

        protected override object OnRead(ISession session, PipeStream stream)
        {
            return stream.ReadString(CurrentSize);
        }
        protected override void OnWrite(ISession session, object data, PipeStream stream)
        {
            stream.Write((string)data);
        }
    }



    public abstract class EofBaseClientPacket : Clients.IClientPacket
    {
        public EventClientPacketCompleted Completed { get; set; }

        public abstract IClientPacket Clone();

        public abstract byte[] EofData { get; }

        private int mSize;

        protected int CurrentSize => mSize;

        protected abstract object OnRead(IClient client, PipeStream stream);

        public void Decode(IClient client, Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
        START:
            var index = pstream.IndexOf(EofData);
            if (index.End != null)
            {
                object data;
                mSize = index.Length - EofData.Length;
                data = OnRead(client, pstream);
                Completed?.Invoke(client, data);
                pstream.ReadFree(EofData.Length);
                goto START;
            }

        }

        public virtual void Dispose()
        {

        }

        protected abstract void OnWrite(object data, IClient client, PipeStream stream);

        public void Encode(object data, IClient client, Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            OnWrite(data, client, pstream);
            pstream.Write(EofData,0,EofData.Length);
        }
    }

    public abstract class LineBaseClientPacket : EofBaseClientPacket
    {
        public LineBaseClientPacket()
        {
            mEof = Encoding.ASCII.GetBytes("\r\n");
        }

        private byte[] mEof;

        public override byte[] EofData => mEof;
    }

    public class StringLineClientPacket : LineBaseClientPacket
    {
        public override IClientPacket Clone()
        {
            return new StringLineClientPacket();
        }

        protected override object OnRead(IClient client, PipeStream stream)
        {
            return stream.ReadString(CurrentSize);
        }

        protected override void OnWrite(object data, IClient client, PipeStream stream)
        {
            stream.Write((string)data);
        }
    }


}
