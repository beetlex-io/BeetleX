using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Clients
{
    public class PacketClient<T> : IDisposable
    where T : IClientPacket, new()
    {
        public PacketClient(string host, int port)
        {
            mClient = SocketFactory.CreateClient<AsyncTcpClient>(new T(), host, port);
        }

        private AsyncTcpClient mClient;
        public void Dispose()
        {
            mClient?.Dispose();
        }
    }

}
