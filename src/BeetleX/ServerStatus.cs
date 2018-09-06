using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX
{
    public enum ServerStatus
    {
        None,
        Start,
        Stop,
        StartError,
        AcceptError,
        Accepting,
        Accepted,
        Closed
    }
}
