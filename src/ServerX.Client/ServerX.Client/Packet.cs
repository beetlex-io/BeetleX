using BeetleX;
using BeetleX.Buffers;
using BeetleX.Clients;
using BeetleX.Packets;
using System;

namespace ServerX
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
    public class Packet : FixeHeaderClientPacket
    {
        readonly static TypeHandler typeHandler;
        static Packet()
        {
            typeHandler = new TypeHandler();
        }

        public TypeHandler TypeHandler { get { return typeHandler; } }

        public override IPacket Clone()
        {
            return this;
        }

        protected override object OnRead(IClient client, PipeStream reader)
        {
            var statusCode = reader.ReadByte();
            var size = reader.ReadByte();
            var rspkey = reader.ReadString(size);
        }

        protected override void OnWrite(object data, IClient client, PipeStream stream)
        {
            var sendinfo = (ResponseData)send;
            var statuscode = sendinfo.StatusCode;
            stream.WriteByte(statuscode);
            var typename = sendinfo.TypeName;
            var data = sendinfo.Data;
            if (string.IsNullOrEmpty(typename)) typeHandler.WriteType(data, stream);
            else
            {
                var l = (byte)typename.Length;
                stream.WriteByte(l);
                stream.Write(typename);
            }
            if (data != null) data.Serialize(stream);
        }
    }
}
