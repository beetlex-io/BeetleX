using System;
using System.Collections.Generic;

namespace ServerX.Client
{
    public class ResponseMessage
    {
        public ResponseMessage(IEnumerable<SubInfo> handlers, object data, Type dataType)
        {
            Handlers = handlers;
            Data = data;
        }
        public IEnumerable<SubInfo> Handlers { get; }
        public object Data { get; }
        public Type DataType { get; }
    }

    public class SubInfo
    {

        public SubInfo(Type type, IEventHandler @event)
        {
            HandlerType = type;
            Instance = @event;
        }

        public static SubInfo Create(Type type, IEventHandler @event)
        {
            return new SubInfo(type, @event);
        }
        public Type HandlerType { get; }
        public IEventHandler Instance { get; }
    }

    public class RequestMessage
    {
        public RequestMessage(string url, object data)
        {
            Url = url;
            Data = data;
        }
        public string Url { get; }
        public object Data { get; }
    }
}
