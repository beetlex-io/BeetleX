using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX
{
    public class ServerOptions
    {
        public ServerOptions()
        {
            MaxAcceptQueue = 0;
            BufferSize = 1024 * 4;
            BufferPoolSize = 10;
            LittleEndian = true;
            Encoding = System.Text.Encoding.UTF8;
            ExecutionContextEnabled = false;
            Combined = 0;
            Statistical = true;
            LogLevel = EventArgs.LogType.Warring;
            IOQueueEnabled = false;
            UseIPv6 = false;
            SessionTimeOut = 0;
            BufferPoolMaxMemory = 100;
            Listens = new List<ListenHandler>();
            Listens.Add(new ListenHandler() { Port = 9090 });
        }

        public int SessionTimeOut { get; set; }

        public bool UseIPv6 { get; set; }

        public EventArgs.LogType LogLevel { get; set; }

        public bool Statistical { get; set; }

        public int Combined
        {
            get; set;
        }

        public ListenHandler DefaultListen => Listens[0];

        internal IList<ListenHandler> Listens { get; private set; }

        public ServerOptions AddListen(int port)
        {
            return AddListen(null, port);
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

        public ServerOptions AddListenSSL(string certificateFile, string password, int port = 443)
        {
            return AddListenSSL(certificateFile, password, null, port);
        }

        public ServerOptions AddListenSSL(string certificateFile, string password, string host, int port = 443)
        {
            ListenHandler listenOptions = new ListenHandler
            {
                Host = host,
                Port = port,
                SSL = true,
                CertificateFile = certificateFile,
                CertificatePassword = password
            };
            Listens.Add(listenOptions);
            return this;
        }

        public int BufferPoolMaxMemory { get; set; }

        public int BufferPoolSize { get; set; }

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


        public int MaxConnections { get; set; } = 1000;

        public int MaxAcceptQueue { get; set; }

        public bool ExecutionContextEnabled { get; set; }

    }
}
