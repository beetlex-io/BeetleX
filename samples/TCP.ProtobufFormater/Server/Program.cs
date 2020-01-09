using System;
using BeetleX;
using BeetleX.Buffers;
using BeetleX.EventArgs;

namespace Server
{
    class Program : ServerHandlerBase
    {
        private static IServer server;
        public static void Main(string[] args)
        {
            server = SocketFactory.CreateTcpServer<Program, Messages.ProtobufPacket>();
            //server.Options.DefaultListen.Port =9090;
            //server.Options.DefaultListen.Host = "127.0.0.1";
            server.Open();
            Console.Read();
        }
        protected override void OnReceiveMessage(IServer server, ISession session, object message)
        {
            ((Messages.Register)message).DateTime = DateTime.Now;
            server.Send(message, session);
        }
    }


}
