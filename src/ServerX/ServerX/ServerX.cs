using Autofac;
using BeetleX;
using BeetleX.EventArgs;
using ServerX.Route;

namespace ServerX
{
    public class ServerX : ServerHandlerBase
    {
        ILifetimeScope _autofac;
        IServer server;
        public void Init(ILifetimeScope scope, ServerOptions options = null)
        {
            _autofac = scope;
            server = SocketFactory.CreateTcpServer<ServerX, Packet>(options);
            server.Open();
        }
        public override void Connected(IServer server, ConnectedEventArgs e)
        {
            e.Session.Tag = new Request(server, e.Session, _autofac);
            base.Connected(server, e);
        }
        protected override void OnReceiveMessage(IServer server, ISession session, object message)
        {
            var info = (RequestMessage)message;
            var request = (Request)session.Tag;
            request.Process(info);
        }
    }
}
