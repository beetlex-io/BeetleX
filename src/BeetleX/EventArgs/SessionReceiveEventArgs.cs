using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public class SessionReceiveEventArgs:SessionEventArgs
    {
        public Buffers.IBinaryReader Reader { get; internal set; }
    }
}
