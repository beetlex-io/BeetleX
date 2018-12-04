using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Blocks
{
    public class EventAsyncMessageErrorArgs : System.EventArgs
    {
        public Exception Error { get; set; }

        public string Message { get; set; }

        public int Code { get; set; }

        public Object Source { get; set; }

    }

    public class EventAwaiterNotFoundArgs : System.EventArgs
    {
        public Object Source { get; set; }
    }
}
