using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace BeetleX.Buffers
{
    public class SocketAsyncEventArgsX : SocketAsyncEventArgs
    {
        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            base.OnCompleted(e);
        }

        public IBuffer BufferX
        {
            get;
            internal set;
        }

        public bool IsReceive
        {
            get;
            set;
        }


        public ISession Session { get; internal set; }


        public void InvokeCompleted()
        {
            OnCompleted(this);
        }
    }
}
