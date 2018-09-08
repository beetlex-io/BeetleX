using System;
using System.Collections.Generic;
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

        protected abstract object OnReader(ISession session, IBinaryReader reader);

        public void Decode(ISession session, IBinaryReader reader)
        {
            START:

            object data;
            if (mSize == 0)
            {
                if (SizeType == FixedSizeType.INT)
                {
                    if (reader.Length < 4)
                        return;
                    mSize = reader.ReadInt32();
                }
                else
                {
                    if (reader.Length < 2)
                        return;
                    mSize = reader.ReadInt16();
                }
            }
            if (reader.Length < mSize)
                return;
            data = OnReader(session, reader);
            mSize = 0;

            Completed?.Invoke(this, mCompletedArgs.SetInfo(session, data));

            goto START;

        }

        public virtual
            void Dispose()
        {

        }

        protected abstract void OnWrite(ISession session, object data, IBinaryWriter writer);

        private void OnEncode(ISession session, object data, IBinaryWriter writer)
        {
            MemoryBlockCollection msgsize = writer.Allocate(4);
            int length = (int)writer.CacheLength;
            OnWrite(session, data, writer);
            if (SizeType == FixedSizeType.INT)
            {
                msgsize.Full((int)writer.CacheLength - length);
            }
            else
            {
                msgsize.Full((Int16)writer.CacheLength - length);
            }
        }

        public byte[] Encode(object data, IServer server)
        {
            byte[] result = null;
            using (Buffers.PipeStream stream = new PipeStream(server.BufferPool, server.Config.LittleEndian, server.Config.Encoding))
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
            using (Buffers.PipeStream stream = new PipeStream(server.BufferPool, server.Config.LittleEndian, server.Config.Encoding))
            {
                OnEncode(null, data, stream);
                stream.Position = 0;
                int count = (int)stream.Length;
                stream.Read(buffer, 0, count);
                return new ArraySegment<byte>(buffer, 0, count);
            }
        }

        public void Encode(object data, ISession session, IBinaryWriter writer)
        {
            OnEncode(session, data, writer);
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

        protected abstract object OnRead(IClient client, IBinaryReader reader);

        public void Decode(IClient client, IBinaryReader reader)
        {
            START:

            object data;
            if (mSize == 0)
            {
                if (SizeType == FixedSizeType.INT)
                {
                    if (reader.Length < 4)
                        return;
                    mSize = reader.ReadInt32();
                }
                else
                {
                    if (reader.Length < 2)
                        return;
                    mSize = reader.ReadInt16();
                }
            }
            if (reader.Length < mSize)
                return;
            data = OnRead(client, reader);
            mSize = 0;
            Completed?.Invoke(client, data);
            goto START;

        }

        public virtual void Dispose()
        {

        }

        protected abstract void OnWrite(object data, IClient client, IBinaryWriter writer);

        public void Encode(object data, IClient client, IBinaryWriter writer)
        {
            MemoryBlockCollection msgsize = writer.Allocate(4);
            int length = (int)writer.CacheLength;
            OnWrite(data, client, writer);
            if (SizeType == FixedSizeType.INT)
            {
                msgsize.Full((int)writer.CacheLength - length);
            }
            else
            {
                msgsize.Full((Int16)writer.CacheLength - length);
            }
        }
    }
}
