using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public class ServerEventArgs : System.EventArgs
    {
        public IServer Server
        {
            get;
            internal set;
        }

    }
}
