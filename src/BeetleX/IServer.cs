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

        NetConfig Config
        { get; set; }

        Buffers.BufferPoolGroup BufferPool
        { get; }

        long Version { get; }

        ISession[] GetOnlines();

        bool Open();

        bool Pause();

        void Resume();

        long GetRunTime();

        string Name { get; set; }

        X509Certificate2 Certificate { get; }

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

        void DetectionSession(int timeout);

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
