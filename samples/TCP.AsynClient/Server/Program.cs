using BeetleX;
using BeetleX.EventArgs;
using System;

namespace Server
{
    class Program : ServerHandlerBase
    {
        private static IServer server;
        public static void Main(string[] args)
        {
            server = SocketFactory.CreateTcpServer<Program>();
            //server.Options.DefaultListen.Port =9090;
            //server.Options.DefaultListen.Host = "127.0.0.1";
            server.Open();
            Console.Read();
        }
        public override void SessionReceive(IServer server, SessionReceiveEventArgs e)
        {
            var pipeStream = e.Stream.ToPipeStream();
            if (pipeStream.TryReadLine(out string name))
            {
                Console.WriteLine(name);
                e.Session.Stream.ToPipeStream().WriteLine("hello " + name);
                e.Session.Stream.Flush();
            }
            base.SessionReceive(server, e);
        }
    }
}
