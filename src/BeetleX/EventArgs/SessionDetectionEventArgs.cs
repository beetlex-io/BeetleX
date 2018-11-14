using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public class SessionDetectionEventArgs : ServerEventArgs
    {
        public IList<IDetector> Sesions
        {
            get;
            internal set;
        }
    }
}
