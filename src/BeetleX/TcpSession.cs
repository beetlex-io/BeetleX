using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BeetleX.Buffers;

namespace BeetleX
{
    class TcpSession : ISession
    {
        static long mId;

        private bool mIsDisposed = false;


        private Buffers.PipeStream mBaseNetStream;

        private SslStream mSslStream;

        private System.Collections.Concurrent.ConcurrentQueue<object> mSendMessages = new System.Collections.Concurrent.ConcurrentQueue<object>();

        private EventArgs.SessionReceiveEventArgs mReceiveArgs = new EventArgs.SessionReceiveEventArgs();

        private int mSendStatus = 0;

        public TcpSession()
        {
            ID = System.Threading.Interlocked.Increment(ref mId);

        }

        public void Initialization(IServer server, Action<ISession> setting)
        {
            Server = server;
            mBaseNetStream = new Buffers.PipeStream(this.BufferPool, server.Config.LittleEndian, server.Config.Encoding);
            mBaseNetStream.Encoding = Server.Config.Encoding;
            mBaseNetStream.LittleEndian = server.Config.LittleEndian;
            mBaseNetStream.FlashCompleted = OnWriterFlash;
            mBaseNetStream.Socket = this.Socket;
            Authentication = AuthenticationType.None;
            if (setting != null)
            {
                setting(this);
            }
        }

        private Dictionary<string, object> mProperties = new Dictionary<string, object>();

        public object this[string key]
        {
            get
            {
                object value = null;
                mProperties.TryGetValue(key, out value);
                return value;
            }

            set
            {
                mProperties[key] = value;
            }
        }

        public long ID
        {
            get;
            internal set;
        }

        public string Name
        {
            get;
            set;
        }

        public IServer Server
        {
            get;
            internal set;
        }

        public Buffers.BufferPool BufferPool { get; set; }

        public Socket Socket
        {
            get;
            internal set;
        }

        public bool IsDisposed
        {
            get
            {
                return mIsDisposed;
            }
        }

        public object Tag
        {
            get;
            set;
        }

        private int mCount;

        public int Count => mCount;

        private void EnqueueSendMessage(object data)
        {
            mSendMessages.Enqueue(data);
            System.Threading.Interlocked.Increment(ref mCount);
        }

        private object DequeueSendMessage()
        {
            object result;
            if (mSendMessages.TryDequeue(out result))
            {
                System.Threading.Interlocked.Decrement(ref mCount);
            }
            return result;
        }

        protected virtual void OnDispose()
        {
            try
            {
                object data = DequeueSendMessage();
                while (data != null)
                {
                    if (data is IBuffer)
                        ((IBuffer)data).Free();
                    data = DequeueSendMessage();
                }
                mReceiveArgs.Server = null;
                mReceiveArgs.Session = null;
                mBaseNetStream.Dispose();
                if (mSslStream != null)
                    mSslStream.Dispose();
                Server.CloseSession(this);
                Server = null;
                ReceiveDispatcher = null;
                if (Packet != null)
                    Packet.Dispose();
                mProperties.Clear();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                if (!mIsDisposed)
                {
                    mIsDisposed = true;
                    OnDispose();
                }
            }
        }

        public void Receive(IBuffer buffer)
        {
            if (!mIsDisposed)
            {
                mBaseNetStream.Import(buffer);
                InvokeReceiveEvent();
            }
            else
            {
                buffer.Free();
            }
        }

        internal Dispatchs.SingleThreadDispatcher<SocketAsyncEventArgsX> ReceiveDispatcher
        {
            get;
            set;
        }

        internal Dispatchs.SingleThreadDispatcher<ISession> SendDispatcher
        {
            get;
            set;
        }

        public double ActiveTime
        {
            get;
            set;
        }

        public LinkedListNode<IDetector> DetectorNode
        {
            get;
            set;
        }

        public EndPoint RemoteEndPoint
        {
            get;
            internal set;
        }

        public bool LittleEndian
        {
            get;
            set;
        }

        internal void InvokeReceiveEvent()
        {
            if (!mIsDisposed)
            {
                mReceiveArgs.Server = this.Server;
                mReceiveArgs.Session = this;
                mReceiveArgs.Stream = this.Stream;
                Server.SessionReceive(mReceiveArgs);
            }
        }

        internal void ProcessSendMessages()
        {
            IBuffer[] items;
            if (IsDisposed)
                return;
            if (System.Threading.Interlocked.CompareExchange(ref mSendStatus, 1, 0) == 0)
            {
                BufferLink bufferLink = new BufferLink();
                object data = DequeueSendMessage();
                if (data == null)
                {
                    System.Threading.Interlocked.Exchange(ref mSendStatus, 0);
                    return;
                }
                PipeStream pipStream = Stream.ToPipeStream();
                while (data != null)
                {
                    if (data is IBuffer)
                    {
                        bufferLink.Import((IBuffer)data);
                    }
                    else if (data is IBuffer[])
                    {
                        items = (IBuffer[])data;
                        for (int i = 0; i < items.Length; i++)
                        {
                            bufferLink.Import(items[i]);
                        }
                    }
                    else if (data is IEnumerable<IBuffer>)
                    {
                        foreach (IBuffer item in (IEnumerable<IBuffer>)data)
                        {
                            bufferLink.Import(item);
                        }
                    }
                    else
                    {

                        WriterData(data, pipStream);

                    }
                    data = DequeueSendMessage();
                }
                if (SSL && pipStream.CacheLength > 0)
                {
                    pipStream.Flush();
                }
                IBuffer streamBuffer = mBaseNetStream.GetWriteCacheBufers();
                bufferLink.Import(streamBuffer);
                if (bufferLink.First != null)
                {
                    CommitBuffer(bufferLink.First);
                }
                else
                {
                    System.Threading.Interlocked.Exchange(ref mSendStatus, 0);
                }
            }
        }

        internal void CommitBuffer(IBuffer buffer)
        {
            try
            {
                ((Buffers.Buffer)buffer).AsyncTo(this);
            }
            catch (Exception e_)
            {
                Buffers.Buffer.Free(buffer);
                if (Server.EnableLog(EventArgs.LogType.Error))
                    Server.Error(e_, this, "{0} session send data error {1}!", this.RemoteEndPoint, e_.Message);
            }
        }

        internal void SendCompleted()
        {
            System.Threading.Interlocked.Exchange(ref mSendStatus, 0);
            ProcessSendMessages();
        }

        private void WriterData(object data, System.IO.Stream stream)
        {
            if (data is byte[])
            {
                byte[] bytes = (byte[])data;
                stream.Write(bytes, 0, bytes.Length);
            }
            else if (data is ArraySegment<byte>)
            {
                ArraySegment<byte> segment = (ArraySegment<byte>)data;
                stream.Write(segment.Array, segment.Offset, segment.Count);
            }
            else
            {
                Packet.Encode(data, this, stream);
            }
        }

        public bool Send(object data)
        {
            if (IsDisposed || (int)this.Authentication < 2)
            {
                return false;
            }
            EnqueueSendMessage(data);
            if (Server.Config.SendQueueEnabled)
            {
                SendDispatcher.Enqueue(this);
            }
            else
            {
                ProcessSendMessages();
            }
            return true;
        }

        private void OnWriterFlash(Buffers.IBuffer data)
        {
            if (data != null)
                Send(data);
        }

        public IPacket Packet
        {
            get;
            internal set;
        }

        public ISessionSocketProcessHandler SocketProcessHandler
        {
            get;
            set;
        }

        public AuthenticationType Authentication
        {
            get;
            set;
        }

        public System.IO.Stream Stream
        {
            get
            {
                if (SSL)
                    return mSslStream;
                return mBaseNetStream;
            }
        }

        public bool SSL { get; internal set; }

        public void CreateSSL(AsyncCallback asyncCallback)
        {
            try
            {
                mSslStream = new SslStreamX(this.BufferPool, Server.Config.Encoding,
                    Server.Config.LittleEndian, mBaseNetStream, false);
#if(NETSTANDARD2_0)
                mSslStream.BeginAuthenticateAsServer(Server.Certificate, new AsyncCallback(asyncCallback),
                    new Tuple<TcpSession, SslStream>(this, this.mSslStream));
#else
                mSslStream.BeginAuthenticateAsServer(Server.Certificate, false, true, new AsyncCallback(asyncCallback),
                     new Tuple<TcpSession, SslStream>(this, this.mSslStream));
#endif
            }
            catch (Exception e_)
            {
                if (Server.EnableLog(EventArgs.LogType.Error))
                    Server.Error(e_, this, "{1} create session ssl error {0}", e_.Message, this.RemoteEndPoint);
                this.Dispose();
            }
        }
    }
}

