using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BeetleX.Buffers;

namespace BeetleX
{
    class TcpSession : ISession
    {
        static long mId;

        private bool mIsDisposed = false;


        private Buffers.PipeStream mNetStream;

        private System.Collections.Concurrent.ConcurrentQueue<object> mSendMessages = new System.Collections.Concurrent.ConcurrentQueue<object>();

        private System.Collections.Concurrent.ConcurrentQueue<object> mReceiveMessages = new System.Collections.Concurrent.ConcurrentQueue<object>();

        private EventArgs.SessionReceiveEventArgs mReceiveArgs = new EventArgs.SessionReceiveEventArgs();

        private int mSendStatus = 0;

        public TcpSession()
        {
            ID = System.Threading.Interlocked.Increment(ref mId);

        }

        public void Initialization(IServer server, Action<ISession> setting)
        {

            Server = server;
            mNetStream = new Buffers.PipeStream(Server.BufferPool, server.Config.LittleEndian, server.Config.Encoding);
            mNetStream.Encoding = Server.Config.Encoding;
            mNetStream.LittleEndian = server.Config.LittleEndian;
            mNetStream.FlashCompleted = OnWriterFlash;

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

        private void EnqueueSendMessage(object data)
        {
            mSendMessages.Enqueue(data);
        }

        private object DequeueSendMessage()
        {
            object result;
            mSendMessages.TryDequeue(out result);
            return result;

        }

        public object DequeueReceiveMessage()
        {
            object result;
            mReceiveMessages.TryDequeue(out result);
            return result;
        }

        public void EnqueueReceiveMessage(object data)
        {
            mReceiveMessages.Enqueue(data);
        }

        private EventArgs.PacketDecodeCompletedEventArgs mDecodeCompletedArgs = new EventArgs.PacketDecodeCompletedEventArgs();

        public EventArgs.PacketDecodeCompletedEventArgs GetDecodeCompletedArgs()
        {
            mDecodeCompletedArgs.Session = this;
            mDecodeCompletedArgs.Server = this.Server;
            mDecodeCompletedArgs.Message = DequeueReceiveMessage();
            return mDecodeCompletedArgs;
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
                mNetStream.Dispose();
                Server.CloseSession(this);
                Server = null;
                ReceiveDispatcher = null;
                if (Packet != null)
                    Packet.Dispose();
                mProperties.Clear();
                mReceiveMessages.Clear();
                mDecodeCompletedArgs = null;
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
                mNetStream.Import(buffer);
                InvokeReceiveEvent();
            }
            else
            {
                buffer.Free();
            }
        }

        internal Dispatchs.Dispatcher<SocketAsyncEventArgsX> ReceiveDispatcher
        {
            get;
            set;
        }

        internal Dispatchs.Dispatcher<ISession> SendDispatcher
        {
            get;
            set;
        }

        public double ActiveTime
        {
            get;
            set;
        }

        public LinkedListNode<IDetectorItem> DetectorNode
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
                mReceiveArgs.Reader = mNetStream;
                Server.SessionReceive(mReceiveArgs);
            }
        }

        internal void ProcessSendMessages()
        {
            IBuffer[] items;
            if (IsDisposed || mSendMessages.Count == 0)
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
                        WriterData(data, mNetStream);
                    }
                    data = DequeueSendMessage();
                }
                IBuffer streamBuffer = mNetStream.GetWriteCacheBufers();
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
                Server.Error(e_, this, "session send data error!");
            }
        }


        internal void SendCompleted()
        {
            System.Threading.Interlocked.Exchange(ref mSendStatus, 0);
            ProcessSendMessages();
        }

        private void WriterData(object data, IBinaryWriter writer)
        {
            if (data is byte[])
            {
                byte[] bytes = (byte[])data;
                writer.Write(bytes, 0, bytes.Length);
            }
            else if (data is ArraySegment<byte>)
            {
                ArraySegment<byte> segment = (ArraySegment<byte>)data;
                writer.Write(segment.Array, segment.Offset, segment.Count);
            }
            else
            {
                Packet.Encode(data, this, writer);
            }
        }

        public bool Send(object data)
        {
            if (IsDisposed)
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

        public PipeStream NetStream => mNetStream;
    }
}

