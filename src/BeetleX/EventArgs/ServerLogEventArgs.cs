using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public class ServerLogEventArgs : ServerEventArgs
    {
        public ISession Session { get; internal set; }

        public LogType Type { get; set; }

        public string Message
        {
            get;
            internal set;
        }
    }
}
