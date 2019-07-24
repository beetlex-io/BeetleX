using BeetleX;
using System;

namespace ServerX.Route
{
    public class Response
    {
        static readonly ResponseData HeartBeat = new ResponseData(null, 20, "beat");
        readonly IServer server;
        readonly ISession session;
        internal Response(IServer server, ISession session)
        {
            this.server = server;
            this.session = session;
        }
        public void OK(object info) 
        {
            Write(info, 20);
        }
        public void Error(string message,byte statuscode)
        {
            server.Send(new ResponseData(message, statuscode, "error"));
        }
        public void WriteHeartBeat()
        {
            server.Send(HeartBeat, session);
        }
        internal void Write<T>(T info, byte statuscode) where T : class
        {
            server.Send(new ResponseData(info, statuscode, null));
        }
    }
}
