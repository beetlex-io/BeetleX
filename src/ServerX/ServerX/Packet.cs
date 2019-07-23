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
    public class RequestMessage
    {
        public static RequestMessage Create(string url)
        {
            return new RequestMessage(url, null);
        }
        public static RequestMessage Create(string url, object token)
        {
            return new RequestMessage(url, token);
        }
        public RequestMessage Clone(object token)
        {
            return new RequestMessage(RouteInfo, token);
        }
        RequestMessage(RouteAction action, object token)
        {
            RouteInfo = action;
            Token = token;
        }
        RequestMessage(string url, object token)
        {
            RouteInfo = GetRouteInfo(url);
            Token = token;
        }

        private RouteAction GetRouteInfo(string url)
        {
            throw new NotImplementedException();
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
            var type = TypeHandler.ReadTypeByUrl(url);
            var typesize = packetSize - size - 1;
            return RequestMessage.Create(url, reader.Stream.Deserialize(typesize, type));
        }

        protected override void OnWrite(ISession session, object data, PipeStream stream)
        {
            throw new NotImplementedException();
        }
    }
}
