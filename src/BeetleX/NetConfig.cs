using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX
{
    public class NetConfig
    {
        public NetConfig()
        {
            Port = 9090;

            SendQueues = 2;
            SendQueueEnabled = false;
            MaxConnections = 0;
            MaxAcceptQueue = 0;
            BufferSize = 1024 * 8;
            BufferPoolSize = 128;
            LittleEndian = true;
            Encoding = System.Text.Encoding.UTF8;
            ExecutionContextEnabled = false;
            Combined = 0;
            Statistical = true;
            SSL = false;
            LogLevel = EventArgs.LogType.Warring;
            IOQueueEnabled = false;
            UseIPv6 = true;
            DetectionTime = 0;
        }


        public int DetectionTime { get; set; }

        public bool UseIPv6 { get; set; }

        public EventArgs.LogType LogLevel { get; set; }

        public string CertificateFile { get; set; }

        public string CertificatePassword { get; set; }

        public bool Statistical { get; set; }

        public int Combined
        {
            get; set;
        }

        public int BufferPoolSize { get; set; }

        public bool LittleEndian
        {
            get; set;
        }

        public System.Text.Encoding Encoding
        {
            get; set;
        }

        public int Port { get; set; }

        public string Host { get; set; }

        public bool IOQueueEnabled { get; set; }

        public bool SendQueueEnabled { get; set; }

        public int BufferSize { get; set; }

        public int SendQueues { get; set; }

        public int MaxConnections { get; set; }

        public int MaxAcceptQueue { get; set; }

        public bool ExecutionContextEnabled { get; set; }

        public bool SSL { get; set; }

    }
}
