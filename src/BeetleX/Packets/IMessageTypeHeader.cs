using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BeetleX.Packets
{

    public interface IMessageTypeHeader
    {
        Type ReadType(PipeStream stream);

        void WriteType(object data, PipeStream stram);

        void Register(params Assembly[] assemblies);
    }

    public enum MessageIDType
    {
        BYTE,
        SHORT,
        INT,
        STRING
    }

    public class CustomTypeHeader : IMessageTypeHeader
    {

        public CustomTypeHeader()
        {
            IDType = MessageIDType.STRING;
        }

        public CustomTypeHeader(MessageIDType idtype)
        {
            IDType = idtype;
        }

        public MessageIDType IDType
        {
            get; set;
        }

        private System.Collections.Concurrent.ConcurrentDictionary<Type, MessageTypeAttribute> mTypeNames = new System.Collections.Concurrent.ConcurrentDictionary<Type, MessageTypeAttribute>();

        private System.Collections.Concurrent.ConcurrentDictionary<string, MessageTypeAttribute> mNameTypes = new System.Collections.Concurrent.ConcurrentDictionary<string, MessageTypeAttribute>();

        private Type GetType(string typeName)
        {
            MessageTypeAttribute result;
            if (!mNameTypes.TryGetValue(typeName, out result))
            {
                throw new BXException("{0} type not found!", typeName);
            }
            return result.Type;
        }

        public Type ReadType(PipeStream stream)
        {
            string name;
            switch (IDType)
            {
                case MessageIDType.BYTE:
                    name = stream.ReadByte().ToString();
                    break;
                case MessageIDType.INT:
                    name = stream.ReadInt32().ToString();
                    break;
                case MessageIDType.SHORT:
                    name = stream.ReadInt16().ToString();
                    break;
                default:
                    name = stream.ReadShortUTF();
                    break;
            }

            return GetType(name);
        }

        private MessageTypeAttribute GetMTA(Type type)
        {
            MessageTypeAttribute result;
            if (!mTypeNames.TryGetValue(type, out result))
            {
                throw new BXException("{0} type not found!", type);
            }
            return result;
        }



        public void WriteType(object data, PipeStream stream)
        {
            MessageTypeAttribute mta = GetMTA(data.GetType());
            switch (mta.mIDType)
            {
                case MessageIDType.BYTE:
                    stream.Write((byte)mta.ID);
                    break;
                case MessageIDType.INT:
                    stream.Write((int)mta.ID);
                    break;
                case MessageIDType.SHORT:
                    stream.Write((short)mta.ID);
                    break;
                case MessageIDType.STRING:
                    stream.WriteShortUTF((string)mta.ID);
                    break;
            }
        }


        public void Register(Type type, object id)
        {
            MessageTypeAttribute mta = new MessageTypeAttribute();
            mta.mIDType = this.IDType;
            mta.SetID(id);
            mta.Type = type;
            mNameTypes[mta.ID.ToString()] = mta;
            mTypeNames[type] = mta;
        }

        public void Register(params Assembly[] assemblies)
        {
            foreach (Assembly assembly in assemblies)
                foreach (Type type in assembly.GetTypes())
                {
                    MessageTypeAttribute mta = type.GetTypeInfo().GetCustomAttribute<MessageTypeAttribute>();

                    if (mta != null)
                    {
                        if (mta.mIDType == this.IDType)
                        {
                            mta.SetID(type.Name);
                            mta.Type = type;
                            mNameTypes[mta.ID.ToString()] = mta;
                            mTypeNames[type] = mta;
                        }
                    }
                }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class MessageTypeAttribute : Attribute
    {

        public MessageTypeAttribute()
        {
            mIDType = MessageIDType.STRING;
        }

        private Object mId;

        internal MessageIDType mIDType;

        public void SetID(Object id)
        {
            if (mId == null)
                mId = id;
        }

        public MessageTypeAttribute(byte id)
        {
            mId = id;
            mIDType = MessageIDType.BYTE;

        }

        public MessageTypeAttribute(short id)
        {
            mId = id;
            mIDType = MessageIDType.SHORT;
        }

        public MessageTypeAttribute(int id)
        {
            mId = id;
            mIDType = MessageIDType.INT;
        }

        public MessageTypeAttribute(string id)
        {
            mId = id;
            mIDType = MessageIDType.STRING;
        }

        public object ID
        {
            get
            { return mId; }
        }

        public Type Type { get; set; }

    }



}
