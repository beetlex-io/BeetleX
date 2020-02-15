using System;
using System.Collections.Generic;
using System.Text;
using BeetleX;
using BeetleX.Buffers;
using BeetleX.Clients;
using MessagePack;

namespace Messages
{
    public class MsgpackPacket : BeetleX.Packets.FixedHeaderPacket
    {
        static MsgpackPacket()
        {
            TypeHeader.Register(typeof(MsgpackClientPacket).Assembly);
        }
        public static BeetleX.Packets.CustomTypeHeader TypeHeader { get; set; } = new BeetleX.Packets.CustomTypeHeader(BeetleX.Packets.MessageIDType.INT);

        public override IPacket Clone()
        {
            return new MsgpackPacket();
        }

        protected override object OnRead(ISession session, PipeStream stream)
        {
            Type type = TypeHeader.ReadType(stream);
            var size = CurrentSize - 4;
            return MessagePackSerializer.NonGeneric.Deserialize(type, stream, true);
        }

        protected override void OnWrite(ISession session, object data, PipeStream stream)
        {
            TypeHeader.WriteType(data, stream);
            MessagePackSerializer.NonGeneric.Serialize(data.GetType(), stream, data);
        }
    }

    public class MsgpackClientPacket : BeetleX.Packets.FixeHeaderClientPacket
    {

        static MsgpackClientPacket()
        {
            TypeHeader.Register(typeof(MsgpackClientPacket).Assembly);
        }

        public static BeetleX.Packets.CustomTypeHeader TypeHeader { get; set; } = new BeetleX.Packets.CustomTypeHeader(BeetleX.Packets.MessageIDType.INT);

        public override IClientPacket Clone()
        {
            return new MsgpackClientPacket();
        }

        protected override object OnRead(IClient client, PipeStream stream)
        {
            Type type = TypeHeader.ReadType(stream);
            var size = CurrentSize - 4;
            return MessagePackSerializer.NonGeneric.Deserialize(type, stream, true);
        }

        protected override void OnWrite(object data, IClient client, PipeStream stream)
        {
            TypeHeader.WriteType(data, stream);
            MessagePackSerializer.NonGeneric.Serialize(data.GetType(),stream, data);
        }
    }

}
