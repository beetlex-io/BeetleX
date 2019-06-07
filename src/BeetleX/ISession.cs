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


    public interface ISession : IDisposable
    {

        double TimeOut
        { get; set; }

        string Host { get; set; }

        int Port { get; set; }

        int MaxWaitMessages { get; set; }

        Buffers.IBufferPool ReceiveBufferPool { get; set; }

        Buffers.IBufferPool SendBufferPool { get; set; }

        void Initialization(IServer server, Action<ISession> setting);

        bool LittleEndian
        {
            get; set;
        }


        Buffers.SocketAsyncEventArgsX SendEventArgs { get; set; }

        Buffers.SocketAsyncEventArgsX ReceiveEventArgs { get; set; }

        ISessionSocketProcessHandler SocketProcessHandler
        {
            get; set;
        }

        long ID { get; }

        object this[string key] { get; set; }

        int Count { get; }

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

        System.IO.Stream Stream
        {
            get;
        }

        bool SSL { get; }

        AuthenticationType Authentication
        { get; set; }

    }

    public enum AuthenticationType : int
    {
        None = 1,

        Connected = 2,

        User = 4,

        Admin = 8,

        Security = 16

    }
}
