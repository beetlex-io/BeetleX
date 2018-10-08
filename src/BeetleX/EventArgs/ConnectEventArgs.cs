using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public class ConnectingEventArgs : ServerEventArgs
    {
       
        public System.Net.Sockets.Socket Socket
        {
            get;
            internal set;
        }
        public bool Cancel
        {
            get;
            set;
        }
    }

    public class ConnectedEventArgs : ServerEventArgs
    {
        public ISession Session { get; internal set; }
    }
}
