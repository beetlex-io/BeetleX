using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using BeetleX.Buffers;
using System.Threading.Tasks;

namespace BeetleX.Clients
{

    public delegate void EventClientConnected(IClient c);

    public class ClientReceiveArgs : System.EventArgs
    {
        public IBinaryReader Reader { get; internal set; }

        public IBinaryWriter Writer { get; internal set; }
    }

    public delegate void EventClientReceive(IClient c, ClientReceiveArgs reader);

    public delegate void EventClientError(IClient c, Exception e, string message);

    public interface IClient
    {
        bool LittleEndian
        {
            get;
            set;
        }

        bool IsConnected
        { get; }

        Encoding Encoding { get; set; }

        IClientPacket Packet
        {
            get;

        }

        long SendQuantity { get; }

        long ReceiveQuantity { get; }

        long ReceivBytes
        {
            get;
        }

        long SendBytes
        { get; }

        void DisConnect();

        bool Connect();

        int TimeOut
        {
            get;
            set;
        }

        Socket Socket
        {
            get;

        }

        PipeStream NetStream { get; }

        void Init(string host, int port, IClientPacket packet);

    }


    public class TcpClient : IClient
    {
        public TcpClient()
        {
            TimeOut = 1000 * 8;
            Encoding = Encoding.UTF8;
            this.LittleEndian = true;
        }

        public void Init(string host, int port, IClientPacket packet)
        {
            var talk = Dns.GetHostAddressesAsync(host);
            talk.Wait();
            IPAddress[] ips = talk.Result;
            if (ips.Length == 0)
                throw new BXException("get host's address error");
            mIPAddress = ips[0];

            mPort = port;
            if (packet != null)
            {
                Packet = packet;
                Packet.Completed = OnPacketCompleted;
            }

        }

        private IPAddress mIPAddress;

        private int mPort;

        private bool mConnected = false;

        private long mSendQuantity;

        private long mReceiveQuantity;

        private long mReceiveBytes;

        private long mSendBytes;

        private Socket mSocket;

        private object mReceiveMessage;

        private PipeStream mNetStream;

        public bool LittleEndian { get; set; }

        public bool IsConnected => mConnected;

        public IClientPacket Packet { get; private set; }

        public long SendQuantity => mSendQuantity;

        public long ReceiveQuantity => mReceiveQuantity;

        public long ReceivBytes => mReceiveBytes;

        public long SendBytes => mSendBytes;

        public int TimeOut { get; set; }

        public Socket Socket => mSocket;

        public Encoding Encoding { get; set; }

        public PipeStream NetStream { get { Connect(); return mNetStream; } }

        public bool Connect()
        {
            lock (this)
            {

                if (!IsConnected)
                {
                    mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    mSocket.Connect(mIPAddress, mPort);
                    mSocket.ReceiveTimeout = TimeOut;
                    mSocket.SendTimeout = TimeOut;
                    mNetStream = new Buffers.PipeStream();
                    mNetStream.Encoding = this.Encoding;
                    mNetStream.LittleEndian = this.LittleEndian;
                    mNetStream.FlashCompleted = OnWriterFlash;
                    mConnected = true;
                }
                return mConnected;
            }
        }

        public void DisConnect()
        {
            mConnected = false;
            try
            {
                if (mSocket != null)
                {
                    CloseSocket(mSocket);
                    mSocket = null;
                }

                if (mNetStream != null)
                    mNetStream.Dispose();
            }
            catch
            {
            }
        }

        public void SendMessage(object msg)
        {
            IBuffer[] items;
            Connect();
            lock (mNetStream)
            {
                BufferLink bufferLink = new BufferLink();
                if (msg != null)
                {
                    if (msg is IBuffer)
                    {
                        bufferLink.Import((IBuffer)msg);
                    }
                    else if (msg is IBuffer[])
                    {
                        items = (IBuffer[])msg;
                        for (int i = 0; i < items.Length; i++)
                        {
                            bufferLink.Import(items[i]);
                        }
                    }
                    else
                    {
                        WriterData(msg, mNetStream);
                    }
                }
                IBuffer writeBuffer = mNetStream.GetWriteCacheBufers();
                bufferLink.Import(writeBuffer);
                if (bufferLink.First != null)
                    OnWriterFlash(bufferLink.First);

            }
        }

        private void WriterData(object data, Buffers.IBinaryWriter writer)
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
                if (Packet == null)
                    throw new BXException("message formater is null!");
                Packet.Encode(data, null, writer);
            }
        }

        public IBinaryReader Read()
        {
            return GetReader();
        }

        public T ReadMessage<T>()
        {
            Connect();
            mReceiveMessage = null;
            while (mReceiveMessage == null)
            {
                try
                {
                    Packet.Decode(this, GetReader());
                }
                catch (Exception e_)
                {
                    DisConnect();
                    throw new BXException("buffer decoding error", e_);
                }
            }
            return (T)mReceiveMessage;
        }

        private IBinaryReader GetReader()
        {
            Connect();
            IBuffer buffer = BufferPool.Default.Pop();
            try
            {
                ((Buffers.Buffer)buffer).From(Socket);
                if (buffer.Length == 0)
                    throw new SocketException((int)SocketError.Shutdown);
                buffer.Postion = 0;
                System.Threading.Interlocked.Add(ref mReceiveBytes, buffer.Length);
                System.Threading.Interlocked.Increment(ref mReceiveQuantity);
                mNetStream.Import(buffer);
                return mNetStream;
            }
            catch (SocketException e_)
            {
                buffer.Free();
                DisConnect();
                throw e_;
            }
        }

        public Task<IBinaryReader> ReadAsync()
        {
            return Task.Run<IBinaryReader>(new Func<IBinaryReader>(GetReader));
        }

        public Task<T> ReadMessageAsync<T>()
        {
            return Task.Run<T>(new Func<T>(ReadMessage<T>));
        }

        private void OnPacketCompleted(IClient client, object message)
        {
            mReceiveMessage = message;
        }

        private void OnWriterFlash(IBuffer data)
        {
            int index = 0;
            if (data == null)
                return;
            Buffers.Buffer buffer = (Buffers.Buffer)data;
            try
            {
                while (buffer != null)
                {
                    buffer.To(Socket);
                    System.Threading.Interlocked.Add(ref mSendBytes, buffer.Length);
                    System.Threading.Interlocked.Increment(ref mSendQuantity);
                    index++;
                    buffer = (Buffers.Buffer)buffer.Next;
                }
            }
            catch (ObjectDisposedException ode)
            {
                Buffers.Buffer.Free(buffer);
                DisConnect();
                throw ode;
            }
            catch (SocketException se)
            {
                Buffers.Buffer.Free(buffer);
                DisConnect();
                throw se;
            }
            catch (Exception e_)
            {
                Buffers.Buffer.Free(buffer);
                throw e_;
            }

        }

        internal static void CloseSocket(System.Net.Sockets.Socket socket)
        {
            try
            {
                socket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            }
            catch
            {
            }
            try
            {
                socket.Dispose();

            }
            catch
            {

            }

        }

    }


    public interface IClientSocketProcessHandler
    {
        void ReceiveCompleted(IClient client, SocketAsyncEventArgs e);

        void SendCompleted(IClient client, SocketAsyncEventArgs e);
    }


    class ClientBufferPool
    {
        static ClientBufferPool()
        {
            mPool = new BufferPool(1024 * 8, 100, null);
        }
        private static BufferPool mPool;
        public static BufferPool Pool
        {
            get
            {
                return mPool;
            }
        }
    }

    public class AsyncTcpClient : IClient
    {



        public IClientSocketProcessHandler SocketProcessHandler
        {
            get;
            set;
        }

        public EventClientConnected Connected
        {
            get;
            set;
        }

        public EventClientReceive Receive
        {
            get;
            set;
        }

        public AsyncTcpClient()
        {
            this.Encoding = Encoding.UTF8;
            LittleEndian = true;
            ExecutionContextEnabled = false;
        }

        public Encoding Encoding
        {
            get;
            set;
        }

        public bool LittleEndian
        {
            get;
            set;
        }

        public void Init(string host, int port, IClientPacket packet)
        {
            var talk = Dns.GetHostAddressesAsync(host);
            talk.Wait();
            IPAddress[] ips = talk.Result;
            if (ips.Length == 0)
                throw new BXException("get host's address error");
            mIPAddress = ips[0];
            mPort = port;
            if (packet != null)
            {
                mPacket = packet;
                mPacket.Completed = OnPacketCompleted;
            }
        }

        private int mSendStatus = 0;

        private IClientPacket mPacket;

        private long mReceivBytes;

        private long mSendQuantity;

        private long mReceiveQuantity;

        private long mSendBytes;

        private System.Collections.Concurrent.ConcurrentQueue<IBuffer> mSendBuffers = new System.Collections.Concurrent.ConcurrentQueue<IBuffer>();

        private ClientReceiveArgs mReceiveArgs;

        private Dictionary<string, object> mProperties = new Dictionary<string, object>();

        private bool mConnected = false;

        private Socket mSocket;

        private Exception mLastError;

        private IPAddress mIPAddress;

        private PipeStream mNetStream = null;

        private int mPort;

        public void ProcessError(Exception e_, string message = null)
        {
            mLastError = e_;
            try
            {
                ClientError?.Invoke(this, e_, message);
            }
            catch
            {
            }
        }

        public void DisConnect()
        {
            lock (this)
            {
                mConnected = false;
                try
                {
                    if (mSocket != null)
                    {
                        TcpClient.CloseSocket(mSocket);
                        mSocket = null;
                    }
                    mProperties.Clear();
                    if (mNetStream != null)
                        mNetStream.Dispose();
                    object item = DequeueSendMessage();
                    while (item != null)
                    {
                        if (item is IBuffer)
                        {
                            Buffers.Buffer.Free(((IBuffer)item));
                        }
                        else if (item is IBuffer[])
                        {
                            foreach (IBuffer b in (IBuffer[])item)
                            {
                                Buffers.Buffer.Free(b);
                            }
                        }
                        item = DequeueSendMessage();
                    }
                }
                catch
                {
                }
            }
        }

        private static void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            Buffers.SocketAsyncEventArgsX ex = (Buffers.SocketAsyncEventArgsX)e;
            AsyncTcpClient tcpclient = (AsyncTcpClient)e.UserToken;
            if (ex.IsReceive)
            {
                ReceiveCompleted(e);
            }
            else
            {
                SendCompleted(e);
            }
        }

        private void OnReceive(IBuffer buffer)
        {
            mNetStream.Import(buffer);
            if (Packet != null)
            {
                try
                {
                    Packet.Decode(this, mNetStream);
                }
                catch (Exception e_)
                {
                    ProcessError(e_, "client  buffer decoding error!");
                }
            }
            else
            {
                try
                {
                    Receive?.Invoke(this, mReceiveArgs);
                }
                catch (Exception e_)
                {
                    ProcessError(e_, "client  buffer process error!");
                }
            }
        }

        private static void ReceiveCompleted(SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgsX ex = (SocketAsyncEventArgsX)e;
            ISession session = ex.Session;
            AsyncTcpClient tcpclient = (AsyncTcpClient)e.UserToken;
            try
            {
                if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
                {
                    System.Threading.Interlocked.Increment(ref tcpclient.mReceiveQuantity);
                    tcpclient.mReceivBytes += e.BytesTransferred;
                    ex.BufferX.Postion = 0;
                    ex.BufferX.SetLength(e.BytesTransferred);
                    tcpclient.OnReceive(ex.BufferX);
                    tcpclient.BeginReceive();
                }
                else
                {
                    ex.BufferX.Free();
                    tcpclient.DisConnect();
                    tcpclient.mLastError = new SocketException((int)e.SocketError);

                }
                if (tcpclient.SocketProcessHandler != null)
                    tcpclient.SocketProcessHandler.ReceiveCompleted(tcpclient, e);
            }
            catch (Exception e_)
            {

                tcpclient.ProcessError(e_, "client network receive error!");
            }
        }

        private static void SendCompleted(SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgsX ex = (SocketAsyncEventArgsX)e;
            Buffers.Buffer buffer = (Buffers.Buffer)ex.BufferX;
            AsyncTcpClient tcpclient = (AsyncTcpClient)e.UserToken;
            try
            {
                if (e.SocketError == SocketError.IOPending || e.SocketError == SocketError.Success)
                {
                    System.Threading.Interlocked.Increment(ref tcpclient.mSendQuantity);
                    tcpclient.mSendBytes += e.BytesTransferred;
                    if (e.BytesTransferred < e.Count)
                    {
                        buffer.Postion = buffer.Postion + e.BytesTransferred;
                        buffer.SetLength(buffer.Length - e.BytesTransferred);
                        buffer.AsyncTo(tcpclient.Socket);
                    }
                    else
                    {
                        IBuffer nextbuf = buffer.Next;
                        buffer.Free();
                        if (nextbuf != null)
                        {
                            tcpclient.CommitBuffer(nextbuf);
                        }
                        else
                        {
                            tcpclient.SendCompleted();
                        }

                    }
                }
                else
                {
                    Buffers.Buffer.Free(ex.BufferX);
                    tcpclient.mLastError = new SocketException((int)e.SocketError);
                    tcpclient.DisConnect();
                }
                if (tcpclient.SocketProcessHandler != null)
                    tcpclient.SocketProcessHandler.SendCompleted(tcpclient, e);
            }
            catch (Exception e_)
            {

                tcpclient.ProcessError(e_, "client network send error!");
            }

        }

        private void BeginReceive()
        {
            if (IsConnected)
            {
                Buffers.Buffer buffer = (Buffers.Buffer)BufferPool.ReceiveDefault.Pop();
                if (!buffer.BindIOCompleted)
                    buffer.BindIOEvent(IO_Completed);
                buffer.UserToken = this;
                try
                {
                    buffer.AsyncFrom(mSocket);
                }
                catch (Exception e_)
                {

                    buffer.Free();
                    DisConnect();
                    ProcessError(e_, "client begin network receive error!");

                }
            }

        }

        public Exception LastError
        {
            get
            {
                return mLastError;
            }
        }

        public int TimeOut
        {
            get;
            set;
        }

        public Socket Socket
        {
            get
            {
                return mSocket;
            }

        }

        public bool IsConnected
        {
            get
            {
                return mConnected;
            }
        }

        public bool Connect()
        {
            lock (this)
            {
                mLastError = null;
                if (!IsConnected)
                {
                    try
                    {
                        if (mNetStream != null)
                            mNetStream.Dispose();
                        mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        mSocket.Connect(mIPAddress, mPort);
                        mSocket.ReceiveTimeout = TimeOut;
                        mSocket.SendTimeout = TimeOut;
                        mConnected = true;
                        mLastError = null;
                        mNetStream = new PipeStream(ClientBufferPool.Pool, this.LittleEndian, this.Encoding);
                        mNetStream.Encoding = this.Encoding;
                        mNetStream.LittleEndian = this.LittleEndian;
                        mNetStream.FlashCompleted = OnWriterFlash;
                        mSendStatus = 0;
                        mReceiveArgs = new ClientReceiveArgs();
                        mReceiveArgs.Reader = mNetStream;
                        mReceiveArgs.Writer = mNetStream;
                        BeginReceive();
                    }
                    catch (Exception e_)
                    {
                        mConnected = false;

                        ProcessError(e_, "client connect to server error!");
                    }
                    if (IsConnected)
                        OnConnected();
                }
                return mConnected;
            }

        }

        private void OnConnected()
        {
            try
            {
                if (Connected != null)
                    Connected(this);
            }
            catch (Exception e_)
            {
                ProcessError(e_, "client process connected to server event error!");
            }
        }

        private System.Collections.Concurrent.ConcurrentQueue<object> mSendMessageQueue = new System.Collections.Concurrent.ConcurrentQueue<object>();

        private void EnqueueSendMessage(object data)
        {
            mSendMessageQueue.Enqueue(data);
        }

        private object DequeueSendMessage()
        {
            object result;
            mSendMessageQueue.TryDequeue(out result);
            return result;

        }


        public void Send(object data)
        {
            Connect();
            if (!IsConnected)
                throw mLastError;
            EnqueueSendMessage(data);
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
                if (Packet == null)
                    throw new BXException("message formater is null!");
                Packet.Encode(data, null, writer);
            }
        }

        private void ProcessSendMessages()
        {
            IBuffer[] items;
            if (mSendMessageQueue.Count == 0)
                return;
            if (System.Threading.Interlocked.CompareExchange(ref mSendStatus, 1, 0) == 0)
            {
                object data = DequeueSendMessage();
                if (data == null)
                {
                    System.Threading.Interlocked.Exchange(ref mSendStatus, 0);
                    return;
                }
                BufferLink bufferLink = new BufferLink();
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
                IBuffer mstreambuffer = mNetStream.GetWriteCacheBufers();
                bufferLink.Import(mstreambuffer);

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

                Buffers.Buffer bf = (Buffers.Buffer)buffer;
                bf.UserToken = this;
                bf.BindIOEvent(IO_Completed);
                ((Buffers.Buffer)buffer).AsyncTo(this.Socket);
            }
            catch (Exception e_)
            {
                Buffers.Buffer.Free(buffer);
                DisConnect();
                ProcessError(e_, "session send data error!");
            }
        }

        private void SendCompleted()
        {
            System.Threading.Interlocked.Exchange(ref mSendStatus, 0);
            ProcessSendMessages();
        }

        private void OnWriterFlash(IBuffer data)
        {
            if (data != null)
                Send(data);
        }
        public IClientPacket Packet
        {
            get
            {
                return mPacket;
            }
        }

        private void OnPacketCompleted(IClient client, object message)
        {
            try
            {
                if (PacketCompleted != null)
                    PacketCompleted(this, message);
            }
            catch (Exception e_)
            {
                ProcessError(e_, "client message process error!");
            }


        }
        public EventClientPacketCompleted PacketCompleted
        {
            get; set;
        }

        public EventClientError ClientError
        {
            get;
            set;
        }

        public bool ExecutionContextEnabled
        {
            get;
            set;
        }

        public long ReceivBytes
        {
            get
            {
                return mReceivBytes;
            }
        }

        public long SendBytes
        {
            get
            {
                return mSendBytes;
            }
        }

        public long SendQuantity
        {
            get
            {
                return mSendQuantity;
            }
        }

        public long ReceiveQuantity
        {
            get
            {
                return mReceiveQuantity;
            }
        }

        public PipeStream NetStream { get { Connect(); return mNetStream; } }

        public object this[string key]
        {
            get
            {
                object result = null;
                mProperties.TryGetValue(key, out result);
                return result;
            }
            set
            {
                mProperties[key] = value;
            }
        }
    }
}
