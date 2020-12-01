using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace BeetleX.Dispatchs
{
    //Class for ConcurrentQueue. Pass through in netstandard2.1/net5.0 and adding support in netstandard2.0 for Clear() by using the code from Net Core 2.1.

#if !NETSTANDARD2_0
    public class ConcurrentQueueX<T> : ConcurrentQueue<T>
    {
    }
#else
    //Use code from BeetleX.Compat
#endif
}
