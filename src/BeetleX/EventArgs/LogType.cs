using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public enum LogType
    {
        None = 1,
        Debug = 2,
        Error = 4,
        Info = 8,
        Warring = 16
    }
}
