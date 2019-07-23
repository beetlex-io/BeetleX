using BeetleX;
using BeetleX.Buffers;
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
    public class RequestMessage
    {
        public readonly static RequestMessage HeartBeat = Create("HeartBeat");
        public readonly static RequestMessage PubKey = Create("PubKey");
        public readonly static RequestMessage SecretKey = Create("SecretKey");
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
            return new RequestMessage(this.Url, token);
        }
        public bool IsSecretRequest()
        {
            return Url == "SecretKey";
        }
        public RequestMessage(string url, object token)
        {
            Url = url;
            Token = token;
        }
        public string Url { get; }
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
            if (string.Compare(url, "beat", true) == 0) return RequestMessage.HeartBeat;
            else if (string.Compare(url, "pubkey", true) == 0) return RequestMessage.PubKey;
            else if (string.Compare(url, "key", true) == 0)
            {
                var datasize = packetSize - size - 1;
                var encryptRaw = reader.ReadString(datasize);
                return RequestMessage.SecretKey.Clone(encryptRaw);
            }
            else
            {
                var type = TypeHandler.ReadTypeByUrl(url);
                var typesize = packetSize - size - 1;
                return RequestMessage.Create(url, reader.Stream.Deserialize(typesize, type));
            }
        }

        protected override void OnWrite(ISession session, object data, PipeStream stream)
        {
            throw new NotImplementedException();
        }
    }
}
