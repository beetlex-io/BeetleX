using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public class ServerErrorEventArgs : ServerEventArgs
    {
        public ServerErrorEventArgs()
        {

        }
        public ServerErrorEventArgs(string message, Exception e)
        {
            Message = message;
            Error = e;
        }
        public ISession Session
        {
            get;
            internal set;
        }

        public string Message
        { get; internal set; }

        public Exception Error
        {
            get;
            internal set;
        }

    }
}
