using BeetleX.Buffers;
using BeetleX.Clients;
using BeetleX.Packets;
using System;

namespace ServerX.Client
{
    static class ObjectSerializeExpand
    {

        public static void Serialize(this object data, System.IO.Stream stream)
        {
            ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(stream, data);
        }
        public static object Deserialize(this System.IO.Stream stream, int length, Type type)
        {
            return ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(stream, null, type, length);
        }
        public static T Deserialize<T>(this System.IO.Stream stream, int length)
        {
            return (T)Deserialize(stream, length, typeof(T));
        }
    }
    internal class Packet : FixeHeaderClientPacket
    {
        private readonly SubscrptionManager submgr;
        internal Packet(SubscrptionManager manager)
        {
            submgr = manager;
        }

        public override IClientPacket Clone()
        {
            return this;
        }

        protected override object OnRead(IClient client, PipeStream reader, int packetSize)
        {
            var statusCode = reader.ReadByte();
            var size = reader.ReadByte();
            var rspkey = reader.ReadString(size);
            var handlers = submgr.GetHandlers(rspkey, out Type eventType);
            var typesize = packetSize - size - 1;
            if (typesize == 0) return new ResponseMessage(handlers, null, eventType);
            else
            {
                var obj = reader.Stream.Deserialize(typesize, eventType);
                return new ResponseMessage(handlers, obj, eventType);
            }
        }

        protected override void OnWrite(object data, IClient client, PipeStream stream)
        {
            var msg = (RequestMessage)data;
            var url = msg.Url;
            var d = msg.Data;
            var l = (byte)url.Length;
            stream.Write(l);
            stream.Write(url);
            if (d != null) d.Serialize(stream);
        }
    }
}
