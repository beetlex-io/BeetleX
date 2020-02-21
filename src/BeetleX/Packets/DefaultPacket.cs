using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeetleX.Buffers;
using BeetleX.Clients;
using BeetleX.EventArgs;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Text;

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

        public Type ReadType(PipeStream stream)
        {
            string typeName = stream.ReadShortUTF();
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

        public void WriteType(object data, PipeStream stream)
        {
            TypeMapper mapper = GetMapper(data);
            if (mapper == null)
                throw new BXException("{0} type not registed!", data.GetType());
            stream.WriteShortUTF(mapper.Name);

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
            IPacket result = new DefaultPacket(this.TypeHeader);
            return result;
        }

        protected override object OnRead(ISession session, PipeStream stream)
        {
            Type type = TypeHeader.ReadType(stream);
            if (type.IsGenericType)
            {
                IList items = (IList)Activator.CreateInstance(type);
                int count = stream.ReadInt32();
                Type subType = type.GetGenericArguments()[0];
                for (int i = 0; i < count; i++)
                {
                    IMessage item = (IMessage)Activator.CreateInstance(subType);
                    item.Load(stream);
                    items.Add(item);
                }
                return items;
            }
            else
            {
                IMessage msg = (IMessage)Activator.CreateInstance(type);
                msg.Load(stream);
                return msg;
            }
        }

        protected override void OnWrite(ISession session, object data, PipeStream stream)
        {
            if (data is IMessage)
            {
                this.TypeHeader.WriteType(data, stream);
                ((IMessage)data).Save(stream);
            }
            else if (data is IList)
            {
                this.TypeHeader.WriteType(data, stream);
                IList items = (IList)data;
                if (items.Count > 0 && !(items[0] is IMessage))
                    throw new BXException("{0} object not implement IMessage !", items[0].GetType());
                stream.Write(items.Count);
                foreach (IMessage item in items)
                {
                    item.Save(stream);
                }

            }
            else
            {
                throw new BXException("{0} not implement IMessage !", data.GetType());
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
            IClientPacket result = new DefaultClientPacket(this.TypeHeader);
            return result;
        }

        protected override object OnRead(IClient client, PipeStream stream)
        {
            Type type = TypeHeader.ReadType(stream);
            if (type.IsGenericType)
            {
                IList items = (IList)Activator.CreateInstance(type);
                int count = stream.ReadInt32();
                Type subType = type.GetGenericArguments()[0];
                for (int i = 0; i < count; i++)
                {
                    IMessage item = (IMessage)Activator.CreateInstance(subType);
                    item.Load(stream);
                    items.Add(item);
                }
                return items;
            }
            else
            {
                IMessage msg = (IMessage)Activator.CreateInstance(type);
                msg.Load(stream);
                return msg;
            }
        }
        protected override void OnWrite(object data, IClient client, PipeStream stream)
        {
            if (data is IMessage)
            {
                this.TypeHeader.WriteType(data, stream);
                ((IMessage)data).Save(stream);
            }
            else if (data is IList)
            {
                this.TypeHeader.WriteType(data, stream);
                IList items = (IList)data;
                if (items.Count > 0 && !(items[0] is IMessage))
                    throw new BXException("{0} object not implement IMessage !", items[0].GetType());
                stream.Write(items.Count);
                foreach (IMessage item in items)
                {
                    item.Save(stream);
                }
            }
            else
            {
                throw new BXException("{0}  not implement IMessage !",data.GetType());
            }
        }
    }

}
