using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX
{

    public interface ISessionSocketProcessHandler
    {
        void ReceiveCompleted(ISession session, System.Net.Sockets.SocketAsyncEventArgs e);

        void SendCompleted(ISession session, System.Net.Sockets.SocketAsyncEventArgs e);
    }


    public interface ISession : IDisposable, IDetectorItem
    {

        void Initialization(IServer server, Action<ISession> setting);

        bool LittleEndian
        {
            get; set;
        }


        ISessionSocketProcessHandler SocketProcessHandler
        {
            get; set;
        }

        long ID { get; }

        object this[string key] { get; set; }

        System.Net.Sockets.Socket Socket
        { get; }

        string Name { get; set; }

        IServer Server { get; }

        bool IsDisposed { get; }

        void Receive(Buffers.IBuffer buffer);

        bool Send(object data);

        object Tag { get; set; }

        System.Net.EndPoint RemoteEndPoint
        { get; }

        IPacket Packet
        { get; }

        Buffers.PipeStream NetStream
        {
            get;
        }

        AuthenticationType Authentication
        { get; set; }

    }

    public enum AuthenticationType
    {
        None = 1,

        User = 2,

        Admin = 4,

        security = 8

    }
}
