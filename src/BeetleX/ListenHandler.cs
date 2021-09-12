using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
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

        public SslProtocols SslProtocols { get; set; } = SslProtocols.Tls11 | SslProtocols.Tls12;

        public string CertificatePassword { get; set; }

        public string Name { get; set; }

        public string Tag { get; set; }

        public bool SyncAccept { get; set; } = false;

        public bool SSL { get; set; }

        public Socket Socket { get; internal set; }

        public IPEndPoint IPEndPoint { get; private set; }

        public bool ReuseAddress { get; set; } = false;

        public IServer Server { get; internal set; }

        public X509Certificate2 Certificate { get; internal set; }

        private Action<AcceptSocketInfo> mAcceptCallBack;

        private bool mIsDisposed = false;

        public Exception Error { get; set; }

        internal void Run(IServer server, Action<AcceptSocketInfo> acceptCallback)
        {
            Server = server;
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

        private IPAddress MatchIPAddress(string matchIP)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (ip.ToString().IndexOf(matchIP) == 0)
                    {
                        return ip;
                    }
                }
            }
            throw new Exception($"No {matchIP} IPv4 address in the system!");
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
                    if (Host.EndsWith("*"))
                    {
                        address = MatchIPAddress(Host.Replace("*", ""));
                        Host = address.ToString();
                    }
                    else
                    {
                        address = System.Net.IPAddress.Parse(Host);
                    }
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
                Socket.Listen(512);
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

        private int mAccetpError = 0;

        private void OnSyncAccept()
        {
            while (true)
            {
                try
                {
                    while (Server.Status == ServerStatus.Stop)
                        System.Threading.Thread.Sleep(500);
                    var acceptSocket = Socket.Accept();
                    AcceptSocketInfo item = new AcceptSocketInfo();
                    item.Socket = acceptSocket;
                    item.Listen = this;
                    mAcceptCallBack(item);
                    mAccetpError = 0;
                }
                catch (Exception e_)
                {
                    if (mIsDisposed)
                        break;
                    Error = e_;
                    mAccetpError++;
                    if (Server.EnableLog(EventArgs.LogType.Error))
                    {
                        Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} accept error {e_.Message}|{e_.StackTrace}!");
                    }
                    if (mAccetpError >= 10)
                    {
                        if (Server.EnableLog(EventArgs.LogType.Warring))
                        {
                            Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} accept stoped!");
                        }
                        Server.Status = ServerStatus.Error;
                        break;
                    }
                }
            }
        }

        private int mAsyncAccepts = 0;

        private async Task OnAsyncAccept()
        {
            if (Server.EnableLog(EventArgs.LogType.Debug))
            {
                Server.Log(EventArgs.LogType.Debug, null, $"{Host}@{Port} begin accept");
            }
            while (true)
            {
                try
                {
                    while (Server.Status == ServerStatus.Stop)
                        System.Threading.Thread.Sleep(500);
                    var socket = await Socket.AcceptAsync();
                    AcceptSocketInfo item = new AcceptSocketInfo();
                    item.Socket = socket;
                    item.Listen = this;
                    mAcceptCallBack(item);
                    mAccetpError = 0;
                }
                catch (Exception e_)
                {
                    if (mIsDisposed)
                        break;
                    Error = e_;
                    mAccetpError++;
                    if (Server.EnableLog(EventArgs.LogType.Error))
                    {
                        Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} accept error {e_.Message}|{e_.StackTrace}!");
                    }
                    if (mAccetpError >= 10)
                    {
                        if (Server.EnableLog(EventArgs.LogType.Warring))
                        {
                            Server.Log(EventArgs.LogType.Error, null, $"{Host}@{Port} accept stoped!");
                        }
                        Server.Status = ServerStatus.Error;
                        break;
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"Listen {Host}:{Port}\t[SSL:{SSL}]\t[Status:{(Error == null ? "success" : $"error {Error.Message}")}]";
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
            finally
            {
                mIsDisposed = true;
            }
        }
    }

    class AcceptSocketInfo
    {
        public ListenHandler Listen { get; set; }

        public Socket Socket { get; set; }
    }
}
