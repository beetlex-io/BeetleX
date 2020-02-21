using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BeetleX
{
    public interface IServer : IDisposable
    {

        int Count
        { get; }

        ServerOptions Options
        { get; }

        Buffers.BufferPoolGroup ReceiveBufferPool
        { get; }

        Buffers.BufferPoolGroup SendBufferPool
        { get; }

        IServer Setting(Action<ServerOptions> handler);

        long Version { get; }

        ISession[] GetOnlines();

        bool Open();

        Action WriteLogo { get; set; }

        bool Pause();

        void Resume();

        long GetRunTime();

        string Name { get; set; }

        IServerHandler Handler
        {
            get;
            set;
        }

        IPacket Packet
        {
            get;
            set;
        }

        bool EnableLog(EventArgs.LogType logType);

        ISession GetSession(long id);

        ServerStatus Status { get; set; }

        void UpdateSession(ISession session);

        void Log(EventArgs.LogType type, ISession session, string message);

        void Log(EventArgs.LogType type, ISession session, string message, params object[] parameters);

        void Error(Exception error, ISession session, string message);

        void Error(Exception error, ISession session, string message, params object[] parameters);

        void SessionReceive(EventArgs.SessionReceiveEventArgs e);

        void CloseSession(ISession session);

        bool Send(object message, ISession session);

        bool[] Send(object message, params ISession[] sessions);

        bool[] Send(object message, System.ArraySegment<ISession> sessions);

        long SendQuantity { get; }

        long ReceiveQuantity { get; }

        long ReceivBytes
        {
            get;
        }

        long SendBytes
        { get; }

    }
}
