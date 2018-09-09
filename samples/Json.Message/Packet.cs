using BeetleX;
using BeetleX.Buffers;
using BeetleX.Clients;
using BeetleX.EventArgs;
using BeetleX.Packets;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Json.Messages
{

    public class TypeHandler : BeetleX.Packets.IMessageTypeHeader
    {
        private System.Collections.Concurrent.ConcurrentDictionary<Type, string> mTypeNames = new System.Collections.Concurrent.ConcurrentDictionary<Type, string>();

        private System.Collections.Concurrent.ConcurrentDictionary<string, Type> mNameTypes = new System.Collections.Concurrent.ConcurrentDictionary<string, Type>();

        private Type GetType(string typeName)
        {
            Type result;
            if (!mNameTypes.TryGetValue(typeName, out result))
            {
                if (typeName == null)
                    throw new BXException("{0} type not found!", typeName);
                result = Type.GetType(typeName);
                if (result == null)
                    throw new BXException("{0} type not found!", typeName);

                mNameTypes[typeName] = result;
            }
            return result;
        }

        

        public Type ReadType(PipeStream reader)
        {
            string typeName = reader.ReadShortUTF();
            return GetType(typeName);
        }

        private string GetTypeName(Type type)
        {
            string result;
            if (!mTypeNames.TryGetValue(type, out result))
            {
                TypeInfo info = type.GetTypeInfo();
                if (info.FullName.IndexOf("System") >= 0)
                    result = info.FullName;
                else
                    result = string.Format("{0},{1}", info.FullName, info.Assembly.GetName().Name);
                mTypeNames[type] = result;
            }
            return result;
        }

        public void WriteType(object data, PipeStream writer)
        {
            string name = GetTypeName(data.GetType());
            writer.WriteShortUTF(name);
        }

        public void Register(params Assembly[] assemblies)
        {

        }
    }


    public class ClientPacket : FixeHeaderClientPacket
    {
        public ClientPacket()
        {
            TypeHeader = new TypeHandler();
        }

        public void Register(params Assembly[] assemblies)
        {
            TypeHeader.Register(assemblies);
        }

        public IMessageTypeHeader TypeHeader { get; set; }


        public override IClientPacket Clone()
        {
            ClientPacket result = new ClientPacket();
            result.TypeHeader = TypeHeader;
            return result;
        }



        protected override object OnRead(IClient client, PipeStream stream)
        {
            Type type = TypeHeader.ReadType(stream);
            string value = stream.ReadUTF();
            return Newtonsoft.Json.JsonConvert.DeserializeObject(value, type);
        }

        protected override void OnWrite(object data, IClient client, PipeStream stream)
        {
            TypeHeader.WriteType(data, stream);
            string value = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            stream.WriteUTF(value);
        }
    }

    public class Packet : FixedHeaderPacket
    {
        public Packet()
        {
            TypeHeader = new TypeHandler();
        }

        private PacketDecodeCompletedEventArgs mCompletedEventArgs = new PacketDecodeCompletedEventArgs();

        public void Register(params Assembly[] assemblies)
        {
            TypeHeader.Register(assemblies);
        }

        public IMessageTypeHeader TypeHeader { get; set; }

        public override IPacket Clone()
        {
            Packet result = new Packet();
            result.TypeHeader = TypeHeader;
            return result;
        }

        protected override object OnReader(ISession session, PipeStream reader)
        {
            Type type = TypeHeader.ReadType(reader);
            string value = reader.ReadUTF();
            return Newtonsoft.Json.JsonConvert.DeserializeObject(value, type);
        }

        protected override void OnWrite(ISession session, object data, PipeStream writer)
        {
            TypeHeader.WriteType(data, writer);
            string value = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            writer.WriteUTF(value);
        }
    }
}
