using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Buffers
{
    public class SocketAsyncEventArgsX : SocketAsyncEventArgs
    {
        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            base.OnCompleted(e);
            if (e.SocketError != SocketError.Success)
            {
                LastSocket = null;
            }
        }

        public IBuffer BufferX
        {
            get;
            internal set;
        }

        public void Clear()
        {
            this.BufferX = null;
        }

        public bool IsReceive
        {
            get;
            set;
        }

        public ISession Session { get; internal set; }

        [ThreadStatic]
        private static System.Net.Sockets.Socket LastSocket;

        [ThreadStatic]
        private static int LoopCount;

        public void InvokeCompleted()
        {
            OnCompleted(this);
        }

        public void AsyncFrom(System.Net.Sockets.Socket socket, object useToken, int size)
        {
            this.IsReceive = true;
            this.UserToken = useToken;
            this.SetBuffer(BufferX.Memory);
            var lastSocket = LastSocket;
            LastSocket = socket;
            if (!socket.ReceiveAsync(this))
            {
                if (lastSocket == socket)
                {
                    LoopCount++;
                }
                else
                {
                    LoopCount = 0;
                }
                if (LoopCount > 50)
                {
                    LoopCount = 0;
                    Task.Run(() => { this.InvokeCompleted(); });
                }
                else
                {
                    this.InvokeCompleted();
                }
            }
            else
            {
                LastSocket = null;
                LoopCount = 0;
            }
        }

        public void AsyncFrom(ISession session, object useToken, int size)
        {
            this.Session = session;
            AsyncFrom(session.Socket, useToken, size);
        }

        public void AsyncTo(System.Net.Sockets.Socket socket, object userToken, int length)
        {
            this.IsReceive = false;
            this.UserToken = userToken;
            this.SetBuffer(BufferX.Memory.Slice(BufferX.Postion, length));
            var lastSocket = LastSocket;
            LastSocket = socket;
            if (!socket.SendAsync(this))
            {
                if (lastSocket == socket)
                {
                    LoopCount++;
                }
                else
                {
                    LoopCount = 0;
                }
                if (LoopCount > 50)
                {
                    LoopCount = 0;
                    Task.Run(() => { this.InvokeCompleted(); });
                }
                else
                {
                    this.InvokeCompleted();
                }
            }
            else
            {
                LastSocket = null;
                LoopCount = 0;
            }
        }

        public void AsyncTo(ISession session, object userToken, int length)
        {
            this.Session = session;
            AsyncTo(Session.Socket, userToken, length);
        }
    }
}
