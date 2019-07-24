using BeetleX;
using BeetleX.Buffers;
using BeetleX.Packets;
using ServerX.Route;
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
    internal class RequestMessage
    {
        public RequestMessage(RouteAction action, object args)
        {
            RouteInfo = action;
            Token = args;
        }
        public RouteAction RouteInfo { get; }
        public object Token { get; }
    }
    public class Packet : FixedHeaderPacket
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

        protected override object OnReader(ISession session, PipeStream reader, int packetSize)
        {
            var size = reader.ReadByte();
            var url = reader.ReadString(size);
            var routeInfo = TypeHandler.GetRouteInfo(url);
            var typesize = packetSize - size - 1;
            if (typesize == 0) return new RequestMessage(routeInfo, null);
            else
            {
                var obj = reader.Stream.Deserialize(typesize, routeInfo.InArgumentType);
                return new RequestMessage(routeInfo, obj);
            }
        }

        protected override void OnWrite(ISession session, object send, PipeStream stream)
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
