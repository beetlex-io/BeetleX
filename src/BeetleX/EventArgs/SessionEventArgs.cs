using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public class SessionEventArgs : ServerEventArgs
    {
        public ISession Session { get; internal set; }
     
    }
}
