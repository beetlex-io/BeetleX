using BeetleX.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX
{
    public class SocketFactory
    {


        public static IServer CreateTcpServer(NetConfig config, IServerHandler handler, IPacket packet)
        {
            TcpServer server = new TcpServer(config);
            server.Handler = handler;
            server.Packet = packet;
            return server;
        }


        public static IServer CreateTcpServer<HANDLER, IPACKET>(NetConfig config)
            where HANDLER : IServerHandler, new()
            where IPACKET : IPacket, new()
        {
            return CreateTcpServer(config, new HANDLER(), new IPACKET());
        }

        public static IServer CreateTcpServer<HANDLER>(NetConfig config) where HANDLER : IServerHandler, new()
        {
            return CreateTcpServer(config, new HANDLER(), null);
        }

        public static CLIENT CreateTcpClient<CLIENT>(string host, int port)
           where CLIENT : IClient, new()
        {
            CLIENT client = new CLIENT();
            client.Init(host, port, null);
            return client;
        }


        public static CLIENT CreateTcpClient<CLIENT, PACKET>(string host, int port)
            where PACKET : Clients.IClientPacket, new()
            where CLIENT : IClient, new()
        {
            CLIENT client = new CLIENT();
            client.Init(host, port, new PACKET());
            return client;
        }

        public static CLIENT CreateTcpClient<CLIENT>(IClientPacket packet, string host, int port) where CLIENT : IClient, new()
        {
            CLIENT client = new CLIENT();
            client.Init(host, port, packet);
            return client;
        }

      

        [ThreadStatic]
        private static bool mChangeExecuteContext = false;

        internal static void ChangeExecuteContext(bool executionContextEnabled)
        {
            if (!mChangeExecuteContext && !executionContextEnabled)
            {
                if (!System.Threading.ExecutionContext.IsFlowSuppressed())
                    System.Threading.ExecutionContext.SuppressFlow();
                mChangeExecuteContext = true;
            }

        }
    }
}
