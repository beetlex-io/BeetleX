using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public enum LogType : int
    {

        Debug = 1,
        None = 2,
        Info = 4,
        Warring = 8,
        Error = 16,
    }
}
