using BeetleX.Buffers;
using BeetleX.Packets;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ServerX
{
    public class TypeHandler : IMessageTypeHeader
    {
        public Type ReadType(PipeStream stream)
        {
            throw new NotImplementedException();
        }

        public void Register(params Assembly[] assemblies)
        {
            throw new NotImplementedException();
        }

        public void WriteType(object data, PipeStream stram)
        {
            throw new NotImplementedException();
        }
        public Type ReadTypeByUrl(string url)
        {

        }
    }
}
