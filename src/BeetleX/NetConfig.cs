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
            ReceiveQueueEnabled = false;
            ReceiveQueues = 2;
            SendQueues = 2;
            SendQueueEnabled = false;
            MaxConnections = 20000;
            MaxAcceptQueue = 10000;
            BufferSize = 1024 * 8;
            LittleEndian = true;
            Encoding = System.Text.Encoding.UTF8;
            ExecutionContextEnabled = false;
            Combined = 0;
            Statistical = true;
        }

        public bool Statistical { get; set; }

        public int Combined
        {
            get; set;
        }

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

        public bool ReceiveQueueEnabled { get; set; }

        public bool SendQueueEnabled { get; set; }

        public int BufferSize { get; set; }

        public int ReceiveQueues { get; set; }

        public int SendQueues { get; set; }

        public int MaxConnections { get; set; }

        public int MaxAcceptQueue { get; set; }

        public bool ExecutionContextEnabled { get; set; }

    }
}
