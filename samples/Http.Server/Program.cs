using BeetleX;
using BeetleX.EventArgs;
using BeetleX.HttpExtend;
using System;
using System.Net.Sockets;

namespace Http.Server
{
    public class Program : ServerHandlerBase, BeetleX.ISessionSocketProcessHandler
    {
        private static IServer server;

        public static void Main(string[] args)
        {   
            NetConfig config = new NetConfig();
            HttpPacket packet = new HttpPacket(new StringSerializer());
            server = SocketFactory.CreateTcpServer(config, new Program(), packet);
            server.Open();
            while (true)
            {
                Console.Write(server);
                System.Threading.Thread.Sleep(1000);
            }
            Console.Read();
        }

        public override void Connecting(IServer server, ConnectingEventArgs e)
        {

        }

        public override void Disconnect(IServer server, SessionEventArgs e)
        {

        }

        public override void Connected(IServer server, ConnectedEventArgs e)
        {
            e.Session.SocketProcessHandler = this;
        }

        public override void SessionPacketDecodeCompleted(IServer server, PacketDecodeCompletedEventArgs e)
        {
            HttpRequest request = (HttpRequest)e.Message;
            if (!request.KeepAlive)
            {
                e.Session.Tag = "close";
            }
            HttpResponse response = request.CreateResponse();
            response.Body = DateTime.Now.ToString();
            server.Send(response, e.Session);
            base.SessionPacketDecodeCompleted(server, e);
        }

        public void ReceiveCompleted(ISession session, SocketAsyncEventArgs e)
        {

        }

        public void SendCompleted(ISession session, SocketAsyncEventArgs e)
        {
            if (session.SendMessages == 0 && session.Tag != null)
            {
                session.Dispose();
            }
        }
    }
}
