using BeetleX.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX
{
    public interface IServerHandler
    {

        void Connecting(IServer server, EventArgs.ConnectingEventArgs e);

        void Connected(IServer server, EventArgs.ConnectedEventArgs e);

        void Log(IServer server, EventArgs.ServerLogEventArgs e);

        void Error(IServer server, EventArgs.ServerErrorEventArgs e);

        void SessionReceive(IServer server, SessionReceiveEventArgs e);

        void SessionPacketDecodeCompleted(IServer server, EventArgs.PacketDecodeCompletedEventArgs e);

        void Disconnect(IServer server, EventArgs.SessionEventArgs e);

        void SessionDetection(IServer server, SessionDetectionEventArgs e);

    }
}
