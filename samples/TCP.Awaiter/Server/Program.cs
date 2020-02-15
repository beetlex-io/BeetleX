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
            server = SocketFactory.CreateTcpServer<Program,StringPacket>();
            //server.Options.DefaultListen.Port =9090;
            //server.Options.DefaultListen.Host = "127.0.0.1";
            server.Open();
            Console.Read();
        }
        protected override void OnReceiveMessage(IServer server, ISession session, object message)
        {
            Console.WriteLine(message);
            server.Send($"hello {message} {DateTime.Now}", session);
        }
    }

    public class StringPacket : BeetleX.Packets.FixedHeaderPacket
    {
        public override IPacket Clone()
        {
            return new StringPacket();
        }

        protected override object OnRead(ISession session, PipeStream stream)
        {
            return stream.ReadString(CurrentSize);
        }
        protected override void OnWrite(ISession session, object data, PipeStream stream)
        {
            stream.Write((string)data);
        }
    }

}
