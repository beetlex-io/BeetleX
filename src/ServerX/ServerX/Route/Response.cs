using BeetleX;

namespace ServerX.Route
{
    public class Response
    {
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
        public void Error(string message, byte statuscode)
        {
            server.Send(new ResponseData(message, statuscode, "error"), session);
        }
        internal void Write<T>(T info, byte statuscode) where T : class
        {
            server.Send(new ResponseData(info, statuscode, null), session);
        }
        internal void Write(ResponseData data)
        {
            server.Send(data, session);
        }
    }
}
