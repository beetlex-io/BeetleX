using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public class SessionReceiveEventArgs : SessionEventArgs
    {
        public System.IO.Stream Stream { get; internal set; }
    }
}
