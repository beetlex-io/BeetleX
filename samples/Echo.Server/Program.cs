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
            //config.SSL = true;
            //config.CertificateFile = @"c:\ssltest.pfx";
            //config.CertificatePassword = "123456";
            server = SocketFactory.CreateTcpServer<Program>(config);
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
