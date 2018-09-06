using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeetleX.Buffers;
using BeetleX.Clients;
using BeetleX.EventArgs;
using System.Reflection;
using System.Collections;

namespace BeetleX.Packets
{

    public class DefaultTypeHeader : IMessageTypeHeader
    {
        private static Dictionary<string, TypeMapper> mNameToType = new Dictionary<string, TypeMapper>();

        private static Dictionary<Type, TypeMapper> mTypeToName = new Dictionary<Type, TypeMapper>();

        class TypeMapper
        {

            public TypeMapper()
            {
                IsArray = false;
                IsList = false;
            }

            public string Name
            {
                get;
                set;
            }

            public Type Type
            {
                get;
                set;
            }

            public Type SubType
            {
                get;
                set;
            }

            public bool IsArray
            {
                get;
                set;
            }

            public bool IsList
            {
                get;
                set;
            }

            public object CreateObject()
            {
                return Activator.CreateInstance(Type);
            }

            public object CreateSubObject()
            {
                return Activator.CreateInstance(SubType);
            }
        }

        public void Register(params Assembly[] assemblies)
        {
            foreach (Assembly assembly in assemblies)

                foreach (Type type in assembly.GetTypes())
                {

                    if (type.GetTypeInfo().GetInterface("BeetleX.Packets.IMessage") != null)
                    {
                        Type listtype = Type.GetType("System.Collections.Generic.List`1");
                        listtype = listtype.MakeGenericType(type);

                        TypeMapper mapper = new TypeMapper();
                        mapper.Name = listtype.Name;
                        mapper.Type = listtype;
                        mapper.SubType = type;
                        mapper.IsList = true;
                        mNameToType[listtype.Name] = mapper;
                        mTypeToName[listtype] = mapper;

                        Type arrayType = type.MakeArrayType();
                        mapper = new TypeMapper();
                        mapper.Name = arrayType.Name;
                        mapper.Type = arrayType;
                        mapper.SubType = type;
                        mapper.IsArray = true;
                        mNameToType[arrayType.Name] = mapper;
                        mTypeToName[arrayType] = mapper;

                        mapper = new TypeMapper();
                        mapper.Name = type.Name;
                        mapper.Type = type;
                        mNameToType[type.Name] = mapper;
                        mTypeToName[type] = mapper;
                    }
                }
        }

        public Type ReadType(IBinaryReader reader)
        {
            string typeName = reader.ReadShortUTF();
            TypeMapper mapper = GetMapper(typeName);
            if (mapper == null)
                throw new BXException("{0} type not registed!", typeName);
            if (mapper.IsArray)
            {
                return mapper.Type.MakeGenericType(mapper.SubType);
            }
            else
            {
                return mapper.Type;
            }
        }

        public void WriteType(object data, IBinaryWriter writer)
        {
            TypeMapper mapper = GetMapper(data);
            if (mapper == null)
                throw new BXException("{0} type not registed!", data.GetType());
            writer.WriteShortUTF(mapper.Name);
        }

        private TypeMapper GetMapper(string name)
        {
            TypeMapper result = null;
            mNameToType.TryGetValue(name, out result);
            return result;
        }

        private TypeMapper GetMapper(object data)
        {
            return GetMapper(data.GetType());
        }

        private TypeMapper GetMapper(Type type)
        {
            TypeMapper result = null;
            mTypeToName.TryGetValue(type, out result);
            return result;
        }
    }

    public class DefaultPacket : FixedHeaderPacket
    {

        public DefaultPacket()
        {
            TypeHeader = new DefaultTypeHeader();
        }

        public DefaultPacket(IMessageTypeHeader typeHeader)
        {
            TypeHeader = typeHeader;
        }

        private PacketDecodeCompletedEventArgs mCompletedArgs = new PacketDecodeCompletedEventArgs();

        public void Register(params Assembly[] assemblies)
        {
            TypeHeader.Register(assemblies);
        }

        public IMessageTypeHeader TypeHeader { get; set; }

        public override IPacket Clone()
        {
            return new DefaultPacket(this.TypeHeader);
        }

        protected override object OnReader(ISession session, IBinaryReader reader)
        {
            Type type = TypeHeader.ReadType(reader);
            if (type.IsGenericType)
            {
                IList items = (IList)Activator.CreateInstance(type);
                int count = reader.ReadInt32();
                Type subType = type.GetGenericArguments()[0];
                for (int i = 0; i < count; i++)
                {
                    IMessage item = (IMessage)Activator.CreateInstance(subType);
                    item.Load(reader);
                    items.Add(item);
                }
                return items;
            }
            else
            {
                IMessage msg = (IMessage)Activator.CreateInstance(type);
                msg.Load(reader);
                return msg;
            }
        }

        protected override void OnWrite(ISession session, object data, IBinaryWriter writer)
        {
            if (data is IMessage)
            {
                this.TypeHeader.WriteType(data, writer);
                ((IMessage)data).Save(writer);
            }
            else if (data is IList)
            {
                this.TypeHeader.WriteType(data, writer);
                IList items = (IList)data;
                if (items.Count > 0 && !(items[0] is IMessage))
                    throw new BXException("item object  not implement IMessage !");
                writer.Write(items.Count);
                foreach (IMessage item in items)
                {
                    item.Save(writer);
                }

            }
            else
            {
                throw new BXException("object  not implement IMessage !");
            }
        }
    }

    public class DefaultClientPacket : FixeHeaderClientPacket
    {

        public DefaultClientPacket()
        {
            TypeHeader = new DefaultTypeHeader();
        }

        public DefaultClientPacket(IMessageTypeHeader typeHeader)
        {
            TypeHeader = typeHeader;
        }

        public void Register(params Assembly[] assemblies)
        {
            TypeHeader.Register(assemblies);
        }

        public IMessageTypeHeader TypeHeader { get; set; }

        public override IClientPacket Clone()
        {
            return new DefaultClientPacket(this.TypeHeader);
        }

        protected override object OnRead(IClient client, IBinaryReader reader)
        {
            Type type = TypeHeader.ReadType(reader);
            if (type.IsGenericType)
            {
                IList items = (IList)Activator.CreateInstance(type);
                int count = reader.ReadInt32();
                Type subType = type.GetGenericArguments()[0];
                for (int i = 0; i < count; i++)
                {
                    IMessage item = (IMessage)Activator.CreateInstance(subType);
                    item.Load(reader);
                    items.Add(item);
                }
                return items;
            }
            else
            {
                IMessage msg = (IMessage)Activator.CreateInstance(type);
                msg.Load(reader);
                return msg;
            }

        }

        protected override void OnWrite(object data, IClient client, IBinaryWriter writer)
        {
            if (data is IMessage)
            {
                this.TypeHeader.WriteType(data, writer);
                ((IMessage)data).Save(writer);
            }
            else if (data is IList)
            {
                this.TypeHeader.WriteType(data, writer);
                IList items = (IList)data;
                if (items.Count > 0 && !(items[0] is IMessage))
                    throw new BXException("item object not implement IMessage !");
                writer.Write(items.Count);
                foreach (IMessage item in items)
                {
                    item.Save(writer);
                }

            }
            else
            {
                throw new BXException("object  not implement IMessage !");
            }
        }
    }

}
