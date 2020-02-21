using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX
{
    public class ListenHandler : IDisposable
    {
        public int Port { get; set; }

        public string Host { get; set; }

        public string CertificateFile { get; set; }

        public string CertificatePassword { get; set; }

        public bool SyncAccept { get; set; } = true;

        public bool SSL { get; set; }

        public Socket Socket { get; internal set; }

        public IPEndPoint IPEndPoint { get; private set; }

        public bool ReuseAddress { get; set; } = false;

        public IServer Server { get; internal set; }

        public X509Certificate2 Certificate { get; internal set; }

        private SocketAsyncEventArgs mAcceptEventArgs = new SocketAsyncEventArgs();

        private Action<AcceptSocketInfo> mAcceptCallBack;

        public Exception Error { get; set; }

        internal void Run(IServer server, Action<AcceptSocketInfo> acceptCallback)
        {
            Server = server;
            mAcceptEventArgs.Completed += OnAcceptCompleted;
            mAcceptEventArgs.UserToken = this;
            mAcceptCallBack = acceptCallback;
            if (SSL)
            {
                if (string.IsNullOrEmpty(CertificateFile))
                {
                    if (server.EnableLog(EventArgs.LogType.Error))
                    {
                        server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} enabled ssl error certificate file name can not be null!");
                    }
                    return;
                }
                try
                {
                    Certificate = new X509Certificate2(CertificateFile, CertificatePassword);
                    if (server.EnableLog(EventArgs.LogType.Info))
                        server.Log(EventArgs.LogType.Info, null, $"load ssl certificate {Certificate}");
                }
                catch (Exception e_)
                {
                    Error = e_;
                    if (Server.EnableLog(EventArgs.LogType.Error))
                    {
                        Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} enabled ssl load certificate file error {e_.Message}|{e_.StackTrace}!");
                    }
                    return;
                }
            }
            BeginListen();
        }

        private void BeginListen()
        {
            try
            {
                System.Net.IPAddress address;
                if (string.IsNullOrEmpty(Host))
                {
                    if (Socket.OSSupportsIPv6 && Server.Options.UseIPv6)
                    {
                        address = IPAddress.IPv6Any;
                    }
                    else
                    {
                        address = IPAddress.Any;
                    }
                }
                else
                {
                    address = System.Net.IPAddress.Parse(Host);
                }
                IPEndPoint = new System.Net.IPEndPoint(address, Port);
                Socket = new Socket(IPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (IPEndPoint.Address == IPAddress.IPv6Any)
                {
                    Socket.DualMode = true;
                }
                if (this.ReuseAddress)
                {
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                }
                Socket.Bind(IPEndPoint);
                Socket.Listen(512 * 4);
                if (Server.EnableLog(EventArgs.LogType.Info))
                    Server.Log(EventArgs.LogType.Info, null, $"listen {Host}@{Port} success ssl:{SSL}");
                if (SyncAccept)
                {
                    System.Threading.ThreadPool.QueueUserWorkItem((o) => OnSyncAccept());
                }
                else
                {
                    OnAsyncAccept();
                }
            }
            catch (Exception e_)
            {
                Error = e_;
                if (Server.EnableLog(EventArgs.LogType.Error))
                {
                    Server.Log(EventArgs.LogType.Error, null, $"listen {Host}@{Port} error {e_.Message}|{e_.StackTrace}!");
                }
            }
        }

        private void OnSyncAccept()
        {
            try
            {
                while (true)
                {
                    var acceptSocket = Socket.Accept();
                    AcceptSocketInfo item = new AcceptSocketInfo();
                    item.Socket = acceptSocket;
                    item.Listen = this;
                    mAcceptCallBack(item);
                }
            }
            catch (Exception e_)
            {
                Error = e_;
                if (Server.EnableLog(EventArgs.LogType.Error))
                {
                    Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} accept error {e_.Message}|{e_.StackTrace}!");
                }
                if (Server.EnableLog(EventArgs.LogType.Warring))
                {
                    Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} accept stoped!");
                }
            }
        }

        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    if (Server.EnableLog(EventArgs.LogType.Debug))
                    {
                        Server.Log(EventArgs.LogType.Debug, null, $"{Host}@{Port} accept success from {e.AcceptSocket.RemoteEndPoint}");
                    }
                    AcceptSocketInfo item = new AcceptSocketInfo();
                    item.Socket = e.AcceptSocket;
                    item.Listen = this;
                    e.AcceptSocket = null;
                    mAcceptCallBack(item);
                }
                else
                {
                    if (Server.EnableLog(EventArgs.LogType.Error))
                    {
                        Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} accept completed socket error {e.SocketError}!");
                    }
                }
            }
            catch (Exception e_)
            {
                if (Server.EnableLog(EventArgs.LogType.Error))
                {
                    Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} accept completed error {e_.Message}|{e_.StackTrace}!");
                }
            }
            finally
            {
                if (mAsyncAccepts >= 50)
                {
                    mAsyncAccepts = 0;
                    Task.Run(() => { OnAsyncAccept(); });
                }
                else
                {
                    OnAsyncAccept();
                }
            }
        }



        private int mAsyncAccepts = 0;

        private void OnAsyncAccept()
        {
            if (Server.EnableLog(EventArgs.LogType.Debug))
            {
                Server.Log(EventArgs.LogType.Debug, null, $"{Host}@{Port} begin accept");
            }
            try
            {

                mAcceptEventArgs.AcceptSocket = null;
                if (!Socket.AcceptAsync(mAcceptEventArgs))
                {
                    mAsyncAccepts++;
                    OnAcceptCompleted(this, mAcceptEventArgs);
                }
                else
                {
                    mAsyncAccepts = 0;
                }

            }
            catch (Exception e_)
            {
                if (Server.EnableLog(EventArgs.LogType.Error))
                {
                    Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} accept error {e_.Message}|{e_.StackTrace}!");
                }
                if (Server.EnableLog(EventArgs.LogType.Warring))
                {
                    Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} accept stoped!");
                }
            }
        }

        public override string ToString()
        {
            return $"Listen {Host}:{Port}\t[SSL:{SSL}]\t[Status:{(Error == null ? "success" : "error")}]";
        }

        public void Dispose()
        {
            try
            {
                TcpServer.CloseSocket(Socket);
            }
            catch
            {

            }
        }
    }

    class AcceptSocketInfo
    {
        public ListenHandler Listen { get; set; }

        public Socket Socket { get; set; }
    }
}
