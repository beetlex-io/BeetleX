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
           
            server = SocketFactory.CreateTcpServer<Program>();
            //server.Options.DefaultListen.CertificateFile = "text.pfx";
            //server.Options.DefaultListen.SSL = true;
            //server.Options.DefaultListen.CertificatePassword = "123456";
            server.Open();
            Console.Write(server);
            Console.Read();
        }
        public override void SessionReceive(IServer server, SessionReceiveEventArgs e)
        {
            string name = e.Stream.ToPipeStream().ReadLine();
            Console.WriteLine(name);
            e.Session.Stream.ToPipeStream().WriteLine("hello " + name);
            e.Session.Stream.Flush();
            base.SessionReceive(server, e);
        }
    }
}
