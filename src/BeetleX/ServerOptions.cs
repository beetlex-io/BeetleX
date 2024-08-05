using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace BeetleX
{
    public class ServerOptions
    {
        public ServerOptions()
        {
            MaxConnections = 10000;
            MaxAcceptQueue = 0;
            BufferSize = 1024 * 4;
            BufferPoolSize = 100;
            LittleEndian = true;
            Encoding = System.Text.Encoding.UTF8;
            ExecutionContextEnabled = false;
            Combined = 0;
            Statistical = true;
            LogLevel = EventArgs.LogType.Warring;
            IOQueueEnabled = false;
            UseIPv6 = true;
            SessionTimeOut = 0;
            BufferPoolMaxMemory = 100;
            Listens = new List<ListenHandler>();
            Listens.Add(new ListenHandler() { Port = 9090 });
            int threads = (Environment.ProcessorCount / 2);
            if (threads == 0)
                threads = 1;
            IOQueues = Math.Min(threads, 16);
            BufferPoolGroups = 4;

        }

        public string ApplicationName { get; set; } = "tcp framework";

        public bool WriteLog { get; set; } = false;

        public string LogTag { get; set; } = "beetlex_tcp";

        public bool LogToConsole { get; set; } = true;

        public int MaxWaitMessages { get; set; } = 0;

        public int IOQueues { get; set; }

        public bool SyncAccept { get; set; } = false;

        public int SessionTimeOut { get; set; }

        public bool UseIPv6 { get; set; }

        public bool UseAccessQueue { get; set; } = false;

        public EventArgs.LogType LogLevel { get; set; }

        public bool Statistical { get; set; }

        public int Combined
        {
            get; set;
        }

        public ListenHandler DefaultListen => Listens[0];

        public ServerOptions DefaultPackeg<PACKET>()
        where PACKET : IPacket, new()
        {
            DefaultListen.Packet = new PACKET();
            return this;
        }

        public IList<ListenHandler> Listens { get; private set; }

        public ServerOptions AddListen(int port)
        {
            return AddListen(string.Empty, port);
        }

        public ServerOptions AddListen<PACKET>(int port)
        where PACKET : IPacket, new()
        {
            AddListen(new PACKET(), null, port);
            return this;
        }
        public ServerOptions AddListen<PACKET>(string host, int port)
        where PACKET : IPacket, new()
        {
            AddListen(new PACKET(), host, port);
            return this;
        }
        public ServerOptions AddListen(IPacket packet, int port)
        {
            AddListen(packet, null, port);
            return this;
        }

        public ServerOptions AddListen(IPacket packet, string host, int port)
        {
            ListenHandler listenOptions = new ListenHandler
            {
                Host = host,
                Port = port,
                Packet = packet
            };
            Listens.Add(listenOptions);
            return this;
        }
        public ServerOptions AddListen(string host, int port)
        {
            ListenHandler listenOptions = new ListenHandler
            {
                Host = host,
                Port = port
            };
            Listens.Add(listenOptions);
            return this;
        }


        public ServerOptions AddListenSSL<PACKET>(string certificateFile, string password, int port = 443)
            where PACKET : IPacket, new()
        {
            return AddListenSSL(new PACKET(), certificateFile, password, null, null, port);
        }

        public ServerOptions AddListenSSL<PACKET>(string certificateFile, string password, SslProtocols? sslProtocols, string host, int port = 443)
            where PACKET : IPacket, new()
        {
            return AddListenSSL(new PACKET(), certificateFile, password, sslProtocols, host, port);
        }

        public ServerOptions AddListenSSL(string certificateFile, string password, int port = 443)
        {
            return AddListenSSL(certificateFile, password, null, null, port);
        }

        public ServerOptions AddListenSSL(string certificateFile, string password, SslProtocols? sslProtocols, string host, int port = 443)
        {
            ListenHandler listenOptions = new ListenHandler
            {
                Host = host,
                Port = port,
                SSL = true,
                CertificateFile = certificateFile,
                CertificatePassword = password
            };
            if (sslProtocols != null)
                listenOptions.SslProtocols = sslProtocols.Value;
            Listens.Add(listenOptions);
            return this;
        }
        public ServerOptions AddListenSSL(IPacket packet, string certificateFile, string password, SslProtocols? sslProtocols, string host, int port = 443)
        {
            ListenHandler listenOptions = new ListenHandler
            {
                Host = host,
                Port = port,
                SSL = true,
                Packet = packet,
                CertificateFile = certificateFile,
                CertificatePassword = password
            };
            if (sslProtocols != null)
                listenOptions.SslProtocols = sslProtocols.Value;
            Listens.Add(listenOptions);
            return this;
        }

        public int BufferPoolMaxMemory { get; set; }

        public int BufferPoolSize { get; set; }

        public int BufferPoolGroups { get; set; }

        public bool LittleEndian
        {
            get; set;
        }

        public System.Text.Encoding Encoding
        {
            get; set;
        }

        public bool IOQueueEnabled { get; set; }

        public int BufferSize { get; set; }

        public int MaxConnections { get; set; }

        public int MaxAcceptQueue { get; set; }

        public bool ExecutionContextEnabled { get; set; }

        public int PrivateBufferPoolSize { get; set; } = 1024 * 1024;

        private LogWriter mLogwriter;

        public LogWriter GetLogWriter()
        {
            if (mLogwriter == null)
                lock (this)
                {
                    if (mLogwriter == null)
                    {
                        mLogwriter = new LogWriter(LogTag);
                    }
                }
            return mLogwriter;
        }

    }
}
