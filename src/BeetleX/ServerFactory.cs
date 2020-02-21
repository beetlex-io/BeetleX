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
        public static IServer CreateTcpServer(IServerHandler handler, IPacket packet, ServerOptions options = null)
        {
            TcpServer server = new TcpServer(options);
            server.Handler = handler;
            server.Packet = packet;
            return server;
        }

        public static IServer CreateTcpServer<HANDLER, IPACKET>(ServerOptions options = null)
            where HANDLER : IServerHandler, new()
            where IPACKET : IPacket, new()
        {
            return CreateTcpServer(new HANDLER(), new IPACKET(), options);
        }

        public static IServer CreateTcpServer<HANDLER>(ServerOptions options = null) where HANDLER : IServerHandler, new()
        {
            return CreateTcpServer(new HANDLER(), null, options);
        }



        public static CLIENT CreateClient<CLIENT>(string host, int port)
           where CLIENT : IClient, new()
        {
            CLIENT client = new CLIENT();
            client.Init(host, port, null);

            return client;
        }

        public static CLIENT CreateClient<CLIENT, PACKET>(string host, int port)
            where PACKET : Clients.IClientPacket, new()
            where CLIENT : IClient, new()
        {
            CLIENT client = new CLIENT();
            client.Init(host, port, new PACKET());

            return client;
        }

        public static CLIENT CreateClient<CLIENT>(IClientPacket packet, string host, int port) where CLIENT : IClient, new()
        {
            CLIENT client = new CLIENT();
            client.Init(host, port, packet);

            return client;
        }


        public static CLIENT CreateSslClient<CLIENT>(string host, int port, string serviceName)
          where CLIENT : IClient, new()
        {
            CLIENT client = new CLIENT();
            client.Init(host, port, null);
            client.SSL = true;
            client.SslServiceName = serviceName;
            return client;
        }

        public static CLIENT CreateSslClient<CLIENT, PACKET>(string host, int port, string serviceName)
            where PACKET : Clients.IClientPacket, new()
            where CLIENT : IClient, new()
        {
            CLIENT client = new CLIENT();
            client.Init(host, port, new PACKET());
            client.SSL = true;
            client.SslServiceName = serviceName;
            return client;
        }

        public static CLIENT CreateSslClient<CLIENT>(IClientPacket packet, string host, int port, string serviceName) where CLIENT : IClient, new()
        {
            CLIENT client = new CLIENT();
            client.Init(host, port, packet);
            client.SSL = true;
            client.SslServiceName = serviceName;
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
