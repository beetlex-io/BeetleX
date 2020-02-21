using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeetleX.Buffers;
using BeetleX.Clients;
using BeetleX.EventArgs;

namespace BeetleX.Packets
{
    public enum FixedSizeType
    {
        INT,
        SHORT
    }


    public abstract class FixedHeaderPacket : IPacket
    {

        public FixedHeaderPacket()
        {
            SizeType = FixedSizeType.INT;
        }

        public FixedSizeType SizeType
        { get; set; }

        public EventHandler<PacketDecodeCompletedEventArgs> Completed { get; set; }

        public abstract IPacket Clone();

        private PacketDecodeCompletedEventArgs mCompletedArgs = new PacketDecodeCompletedEventArgs();

        private int mSize;


        protected int CurrentSize => mSize;

        protected abstract object OnRead(ISession session, PipeStream stream);

        public void Decode(ISession session, System.IO.Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
        START:
            object data;
            if (mSize == 0)
            {
                if (SizeType == FixedSizeType.INT)
                {
                    if (pstream.Length < 4)
                        return;
                    mSize = pstream.ReadInt32();
                }
                else
                {
                    if (pstream.Length < 2)
                        return;
                    mSize = pstream.ReadInt16();
                }
            }
            if (pstream.Length < mSize)
                return;
            data = OnRead(session, pstream);
            mSize = 0;
            Completed?.Invoke(this, mCompletedArgs.SetInfo(session, data));
            goto START;

        }


        public virtual
            void Dispose()
        {

        }

        protected abstract void OnWrite(ISession session, object data, PipeStream stream);

        private void OnEncode(ISession session, object data, System.IO.Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            MemoryBlockCollection msgsize = pstream.Allocate(4);
            int length = (int)pstream.CacheLength;
            OnWrite(session, data, pstream);
            if (SizeType == FixedSizeType.INT)
            {
                msgsize.Full((int)pstream.CacheLength - length);
            }
            else
            {
                msgsize.Full((Int16)pstream.CacheLength - length);
            }

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

    public abstract class FixeHeaderClientPacket : IClientPacket
    {
        public FixeHeaderClientPacket()
        {
            SizeType = FixedSizeType.INT;

        }

        public FixedSizeType SizeType
        { get; set; }

        public EventClientPacketCompleted Completed { get; set; }

        public abstract IClientPacket Clone();

        private int mSize;

        protected int CurrentSize => mSize;

        protected abstract object OnRead(IClient client, PipeStream stream);

        public void Decode(IClient client, Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
        START:
            object data;
            if (mSize == 0)
            {
                if (SizeType == FixedSizeType.INT)
                {
                    if (pstream.Length < 4)
                        return;
                    mSize = pstream.ReadInt32();
                }
                else
                {
                    if (pstream.Length < 2)
                        return;
                    mSize = pstream.ReadInt16();
                }
            }
            if (pstream.Length < mSize)
                return;
            data = OnRead(client, pstream);
            mSize = 0;
            Completed?.Invoke(client, data);
            goto START;

        }

        public virtual void Dispose()
        {

        }

        protected abstract void OnWrite(object data, IClient client, PipeStream stream);

        public void Encode(object data, IClient client, Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            MemoryBlockCollection msgsize = pstream.Allocate(4);
            int length = (int)pstream.CacheLength;
            OnWrite(data, client, pstream);
            if (SizeType == FixedSizeType.INT)
            {
                msgsize.Full((int)pstream.CacheLength - length);
            }
            else
            {
                msgsize.Full((Int16)pstream.CacheLength - length);
            }
        }
    }
}
