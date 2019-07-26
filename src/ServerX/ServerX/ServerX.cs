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
        TypeHandler typehandler;
        public ServerX()
        {
            typehandler = new TypeHandler();
            var heartbeat = new HeartBeatRequestDeal();
            var method = heartbeat.GetType().GetMethod("SendHeartResponse");
            typehandler.Add(heartbeat, method, "beat", true);
        }
        public void Init(ILifetimeScope scope, ServerOptions options = null)
        {
            _autofac = scope;
            server = SocketFactory.CreateTcpServer(this, new Packet(typehandler), options);
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
    class HeartBeatRequestDeal : Controller
    {
        static readonly ResponseData HeartBeat = new ResponseData(null, 20, "beat");
        public void SendHeartResponse(object data)
        {
            System.Console.WriteLine("SendHeartResponse");
            //Response.Write(HeartBeat);
        }
    }
}
