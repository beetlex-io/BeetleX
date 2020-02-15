using System;
using System.Collections.Generic;
using System.Text;
using BeetleX;
using BeetleX.Buffers;
using BeetleX.Clients;

namespace Messages
{
    public class ProtobufPacket : BeetleX.Packets.FixedHeaderPacket
    {
        static ProtobufPacket()
        {
            TypeHeader.Register(typeof(ProtobufClientPacket).Assembly);
        }
        public static BeetleX.Packets.CustomTypeHeader TypeHeader { get; set; } = new BeetleX.Packets.CustomTypeHeader(BeetleX.Packets.MessageIDType.INT);

        public override IPacket Clone()
        {
            return new ProtobufPacket();
        }

        protected override object OnRead(ISession session, PipeStream stream)
        {
            Type type = TypeHeader.ReadType(stream);
            var size = CurrentSize - 4;
            return ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(stream, null, type, size);
        }

        protected override void OnWrite(ISession session, object data, PipeStream stream)
        {
            TypeHeader.WriteType(data, stream);
            ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(stream, data);
        }
    }

    public class ProtobufClientPacket : BeetleX.Packets.FixeHeaderClientPacket
    {

        static ProtobufClientPacket()
        {
            TypeHeader.Register(typeof(ProtobufClientPacket).Assembly);
        }

        public static BeetleX.Packets.CustomTypeHeader TypeHeader { get; set; } = new BeetleX.Packets.CustomTypeHeader(BeetleX.Packets.MessageIDType.INT);

        public override IClientPacket Clone()
        {
            return new ProtobufClientPacket();
        }

        protected override object OnRead(IClient client, PipeStream stream)
        {
            Type type = TypeHeader.ReadType(stream);
            var size = CurrentSize - 4;
            return ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(stream, null, type, size);
        }

        protected override void OnWrite(object data, IClient client, PipeStream stream)
        {
            TypeHeader.WriteType(data, stream);
            ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(stream, data);
        }
    }

}
