using System;
using System.Collections.Generic;
using System.Text;
using BeetleX;
using BeetleX.Buffers;
using BeetleX.Clients;

namespace Messages
{
    public class JsonPacket : BeetleX.Packets.FixedHeaderPacket
    {
        static JsonPacket()
        {
            TypeHeader.Register(typeof(JsonClientPacket).Assembly);
        }
        public static BeetleX.Packets.CustomTypeHeader TypeHeader { get; set; } = new BeetleX.Packets.CustomTypeHeader(BeetleX.Packets.MessageIDType.INT);

        public override IPacket Clone()
        {
            return new JsonPacket();
        }

        protected override object OnRead(ISession session, PipeStream stream)
        {
            Type type = TypeHeader.ReadType(stream);
            var size = CurrentSize - 4;
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(size);
            stream.Read(buffer, 0, size);
            try
            {
                return SpanJson.JsonSerializer.NonGeneric.Utf8.Deserialize(new ReadOnlySpan<byte>(buffer, 0, size), type);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        protected override void OnWrite(ISession session, object data, PipeStream stream)
        {
            TypeHeader.WriteType(data, stream);
            var buffer = SpanJson.JsonSerializer.NonGeneric.Utf8.SerializeToArrayPool(data);
            try
            {
                stream.Write(buffer.Array, buffer.Offset, buffer.Count);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer.Array);
            }
        }
    }

    public class JsonClientPacket : BeetleX.Packets.FixeHeaderClientPacket
    {

        static JsonClientPacket()
        {
            TypeHeader.Register(typeof(JsonClientPacket).Assembly);
        }

        public static BeetleX.Packets.CustomTypeHeader TypeHeader { get; set; } = new BeetleX.Packets.CustomTypeHeader(BeetleX.Packets.MessageIDType.INT);

        public override IClientPacket Clone()
        {
            return new JsonClientPacket();
        }

        protected override object OnRead(IClient client, PipeStream stream)
        {
            Type type = TypeHeader.ReadType(stream);
            var size = CurrentSize - 4;
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(size);
            stream.Read(buffer, 0, size);
            try
            {
                return SpanJson.JsonSerializer.NonGeneric.Utf8.Deserialize(new ReadOnlySpan<byte>(buffer, 0, size), type);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        protected override void OnWrite(object data, IClient client, PipeStream stream)
        {
            TypeHeader.WriteType(data, stream);
            var buffer = SpanJson.JsonSerializer.NonGeneric.Utf8.SerializeToArrayPool(data);
            try
            {
                stream.Write(buffer.Array, buffer.Offset, buffer.Count);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer.Array);
            }
        }
    }

}
