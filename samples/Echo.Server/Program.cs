using BeetleX;
using BeetleX.EventArgs;
using System;

namespace Echo.Server
{
    class Program : ServerHandlerBase
    {
        private static IServer server;
        public static void Main(string[] args)
        {
            NetConfig config = new NetConfig();
            server = SocketFactory.CreateTcpServer<Program>(config);
            server.Open();
            Console.Write(server);
            Console.Read();
        }
        public override void SessionReceive(IServer server, SessionReceiveEventArgs e)
        {
            string name = e.Reader.ReadLine();
            Console.WriteLine(name);
            var w = e.Session.NetworkStream;
            w.WriteLine("hello " + name);
            w.Flush();
            base.SessionReceive(server, e);
        }
    }
}
