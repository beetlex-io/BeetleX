using System;
using System.Collections.Generic;

namespace ServerX.Client
{
    public class ResponseMessage
    {
        public ResponseMessage(IEnumerable<Type> handlers,object data,Type dataType)
        {
            Handlers = handlers;
            Data = data;
        }
        public IEnumerable<Type> Handlers { get; }
        public object Data { get; }
        public Type DataType { get; }
    }
    public class RequestMessage
    {
        public RequestMessage(string url,object data)
        {
            Url = url;
            Data = data;
        }
        public string Url { get; }
        public object Data { get; }
    }
}
