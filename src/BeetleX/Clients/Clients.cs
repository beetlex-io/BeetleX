using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using BeetleX.Buffers;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace BeetleX.Clients
{

    public delegate void EventClientConnected(IClient c);

    public class ClientReceiveArgs : System.EventArgs
    {
        public System.IO.Stream Stream { get; set; }
    }

    public class ClientErrorArgs : System.EventArgs
    {
        public Exception Error { get; set; }

        public string Message { get; set; }
    }

    public delegate void EventClientReceive(IClient c, ClientReceiveArgs reader);

    public delegate void EventClientError(IClient c, ClientErrorArgs e);

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

        object Token { get; set; }

        System.IO.Stream Stream { get; }

        void Init(string host, int port, IClientPacket packet);

        bool SSL { get; set; }

        string SslServiceName { get; set; }

    }

    public class TcpClient : IClient, IDisposable
    {
        public TcpClient()
        {
            TimeOut = 1000 * 8;
            Encoding = Encoding.UTF8;
            this.LittleEndian = true;
            this.SSL = false;
            BufferPool = BufferPoolGroup.DefaultGroup.Next();
        }

        public void Init(string host, int port, IClientPacket packet)
        {
            var talk = Dns.GetHostAddressesAsync(host);
            talk.Wait();
            IPAddress[] ips = talk.Result;
            if (ips.Length == 0)
                throw new BXException("get host's address error");
            foreach (IPAddress item in ips)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                {
                    mIPAddress = item;
                    break;
                }
            }
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

        private SslStreamX mSslStream;

        private PipeStream mBaseNetworkStream;

        public object Token { get; set; }

        public EventClientConnected Connected
        {
            get;
            set;
        }

        public IBufferPool BufferPool { get; set; }

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

        public EndPoint LocalEndPoint { get; set; }

        public System.IO.Stream Stream
        {
            get
            {
                Connect();
                if (SSL)
                    return mSslStream;
                else
                    return mBaseNetworkStream;
            }
        }

        public bool SSL { get; set; }

        public string SslServiceName { get; set; }

        public static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            return false;
        }

        public TcpClient Send(Action<PipeStream> handler)
        {
            Connect();
            PipeStream stream = this.Stream.ToPipeStream();
            if (handler != null)
            {
                handler(stream);
                if (stream.CacheLength > 0)
                    this.Stream.Flush();
            }
            return this;
        }

        private void OnConnect()
        {

            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (LocalEndPoint != null)
                mSocket.Bind(LocalEndPoint);
            mSocket.Connect(mIPAddress, mPort);
            if (LocalEndPoint == null)
                LocalEndPoint = mSocket.LocalEndPoint;
            mSocket.ReceiveTimeout = TimeOut;
            mSocket.SendTimeout = TimeOut;
            if (mBaseNetworkStream != null)
            {
                mBaseNetworkStream.Dispose();
                mBaseNetworkStream = null;
            }
            if (mSslStream != null)
            {
                mSslStream.Dispose();
                mSslStream = null;
            }
            mBaseNetworkStream = new Buffers.PipeStream(BufferPool, this.LittleEndian, this.Encoding);
            mBaseNetworkStream.Socket = mSocket;
            mBaseNetworkStream.Encoding = this.Encoding;
            mBaseNetworkStream.LittleEndian = this.LittleEndian;
            mBaseNetworkStream.FlashCompleted = OnWriterFlash;
            if (this.Packet != null)
            {
                this.Packet = this.Packet.Clone();
                this.Packet.Completed = this.OnPacketCompleted;
            }
            if (SSL)
            {
                mBaseNetworkStream.SSL = true;
                mSslStream = new SslStreamX(BufferPool, this.Encoding, this.LittleEndian, mBaseNetworkStream, new RemoteCertificateValidationCallback(ValidateServerCertificate));
                var task = mSslStream.AuthenticateAsClientAsync(SslServiceName);
                task.Wait();
                mBaseNetworkStream.SSLConfirmed = true;
                mSslStream.SyncData();
            }
            mConnected = true;
            if (mConnected)
                this.Connected?.Invoke(this);
        }

        public bool Connect()
        {
            lock (this)
            {
                if (!IsConnected)
                {
                    OnConnect();
                }
            }
            return mConnected;
        }

        public void DisConnect()
        {
            mConnected = false;
            try
            {
                Token = null;
                if (mSocket != null)
                {
                    CloseSocket(mSocket);
                    mSocket = null;
                }
                if (mSslStream != null)
                {
                    mSslStream.Dispose();
                    mSslStream = null;
                }
                if (mBaseNetworkStream != null)
                {
                    mBaseNetworkStream.Dispose();
                    mBaseNetworkStream = null;
                }

            }
            catch
            {
            }
        }

        public TcpClient SendMessage(object msg)
        {
            IBuffer[] items;
            Connect();
            lock (mBaseNetworkStream)
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
                        PipeStream pipeStream = Stream.ToPipeStream();
                        WriterData(msg, pipeStream);
                        if (SSL)
                            pipeStream.Flush();
                    }
                }
                IBuffer writeBuffer = mBaseNetworkStream.GetWriteCacheBufers();
                bufferLink.Import(writeBuffer);
                if (bufferLink.First != null)
                    OnWriterFlash(bufferLink.First);
            }
            return this;
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
                if (Packet == null)
                    throw new BXException("message formater is null!");
                Packet.Encode(data, null, stream);
            }
        }

        public PipeStream Receive()
        {
            GetReader();
            return this.Stream.ToPipeStream();
        }

        public T ReceiveMessage<T>()
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

        private System.IO.Stream GetReader()
        {
            Connect();
            IBuffer buffer = this.BufferPool.Pop();
            try
            {
                ((Buffers.Buffer)buffer).From(Socket);
                if (buffer.Length == 0)
                    throw new SocketException((int)SocketError.Shutdown);
                buffer.Postion = 0;
                System.Threading.Interlocked.Add(ref mReceiveBytes, buffer.Length);
                System.Threading.Interlocked.Increment(ref mReceiveQuantity);
                mBaseNetworkStream.Import(buffer);
                return Stream;
            }
            catch (SocketException e_)
            {
                buffer.Free();
                DisConnect();
                throw e_;
            }
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
                    var freebuf = buffer;
                    buffer = (Buffers.Buffer)buffer.Next;
                    freebuf.Free();
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

        public void Dispose()
        {
            DisConnect();
        }
    }

    public interface IClientSocketProcessHandler
    {
        void ReceiveCompleted(IClient client, SocketAsyncEventArgs e);

        void SendCompleted(IClient client, SocketAsyncEventArgs e);
    }

    public class AsyncTcpClient : IClient, IDisposable
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

        public EventClientConnected Disconnected
        {
            get; set;
        }

        public EventClientReceive DataReceive
        {
            get;
            set;
        }

        public AsyncTcpClient()
        {
            this.Encoding = Encoding.UTF8;
            LittleEndian = true;
            ExecutionContextEnabled = false;
            ReceiveBufferPool = BufferPoolGroup.DefaultGroup.Next(); //BufferPool.ReceiveDefault;
            SendBufferPool = BufferPoolGroup.DefaultGroup.Next();
            TimeOut = 10000;
            mTimeWatch.Start();
        }

        private System.Diagnostics.Stopwatch mTimeWatch = new System.Diagnostics.Stopwatch();

        private Buffers.SocketAsyncEventArgsX mReceiveEventArgs;

        private Buffers.SocketAsyncEventArgsX mSendEventArgs;

        public IBufferPool ReceiveBufferPool { get; set; }

        public IBufferPool SendBufferPool { get; set; }

        public Encoding Encoding
        {
            get;
            set;
        }

        public object Token { get; set; }

        public EndPoint LocalEndPoint { get; set; }

        public bool LittleEndian
        {
            get;
            set;
        }

        public void Init(string host, int port, IClientPacket packet)
        {
            var talk = Dns.GetHostAddressesAsync(host);
            talk.Wait(10);
            IPAddress[] ips = talk.Result;
            if (ips.Length == 0)
                throw new BXException("get host's address error");
            foreach (IPAddress item in ips)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                {
                    mIPAddress = item;
                    break;
                }
            }
            mPort = port;
            if (packet != null)
            {
                mPacket = packet;
                mPacket.Completed = OnPacketCompleted;
            }
        }

        //private object mLockSend = new object();

        //private bool EnterSend()
        //{
        //    lock (mLockSend)
        //    {
        //        if (!mSendStatus)
        //        {
        //            mSendStatus = true;
        //            return true;
        //        }
        //        else
        //            return false;
        //    }
        //}

        //private void FreeSend()
        //{
        //    lock (mLockSend)
        //    {
        //        mSendStatus = false;
        //    }
        //}

        private int mSendStatus = 0;

        public bool SendStatus => mSendStatus == 1;

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

        private PipeStream mBaseNetworkStream = null;

        private SslStreamX mSslStream = null;

        private AwaitObject awaitPipeStream = new AwaitObject();

        private AwaitObject mReadMessageAwait = new AwaitObject();

        private int mPort;

        public void ProcessError(Exception e_, string message = null)
        {
            mLastError = e_;
            ClientErrorArgs e = new ClientErrorArgs();
            e.Error = e_;
            e.Message = message;
            try
            {
                if (awaitPipeStream.Pending)
                    awaitPipeStream.Error(e_);
                if (mReadMessageAwait.Pending)
                    mReadMessageAwait.Error(e_);
                ClientError?.Invoke(this, e);
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
                    OnDisconnected();
                    Token = null;
                    if (mSocket != null)
                    {
                        TcpClient.CloseSocket(mSocket);
                        mSocket = null;
                    }
                    mReceiveEventArgs.Dispose();
                    mReceiveEventArgs = null;
                    mSendEventArgs.Dispose();
                    mSendEventArgs = null;
                    mProperties.Clear();
                    if (mBaseNetworkStream != null)
                    {
                        mBaseNetworkStream.Dispose();
                        mBaseNetworkStream = null;
                    }
                    if (mSslStream != null)
                    {
                        mSslStream.Dispose();
                        mSslStream = null;
                    }
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
            if (awaitPipeStream.Pending)
            {
                awaitPipeStream.Error(new SocketException((int)SocketError.ConnectionAborted));
            }
            if (mReadMessageAwait.Pending)
            {
                mReadMessageAwait.Error(new SocketException((int)SocketError.ConnectionAborted));
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
            mBaseNetworkStream.Import(buffer);
            if (SSL && mSslStream.SyncDataError != null)
            {
                ProcessError(mSslStream.SyncDataError, $"sync SslStream data error {mSslStream.SyncDataError.Message}");
                DisConnect();
                return;
            }
            if (Packet != null)
            {
                try
                {
                    Packet.Decode(this, this.Stream);
                }
                catch (Exception e_)
                {
                    ProcessError(e_, "client packet decoding error!");
                }
            }
            else
            {
                try
                {
                    mReceiveArgs.Stream = this.Stream;
                    if (awaitPipeStream.Pending)
                        awaitPipeStream.Success(this.Stream.ToPipeStream());
                    else
                        DataReceive?.Invoke(this, mReceiveArgs);

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
                        buffer.AsyncTo(tcpclient.mSendEventArgs, tcpclient.Socket);
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
                Buffers.Buffer buffer = (Buffers.Buffer)ReceiveBufferPool.Pop();
                buffer.UserToken = this;
                try
                {
                    buffer.AsyncFrom(mReceiveEventArgs, mSocket);
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

        public long GetTime()
        {
            return mTimeWatch.ElapsedMilliseconds;
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

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            return false;
        }

        public AwaitObject ReceiveMessage()
        {
            mReadMessageAwait.Reset();
            Connect();
            if (Packet == null)
                ProcessError(new BXException("packet is empty be cannot receive messages!"), "packet is empty be cannot receive messages");
            return mReadMessageAwait;
        }

        public AwaitStruct<T> ReceiveMessage<T>()
        {
            return new AwaitStruct<T>(ReceiveMessage());
        }

        public AwaitStruct<PipeStream> Receive()
        {
            awaitPipeStream.Reset();
            Connect();
            BeginReceive();
            return new AwaitStruct<PipeStream>(awaitPipeStream);
        }

        public bool Connect()
        {
            if (IsConnected)
                return true;
            try
            {
                lock (this)
                {
                    if (!IsConnected)
                    {
                        mLastError = null;
                        if (mBaseNetworkStream != null)
                        {
                            mBaseNetworkStream.Dispose();
                            mBaseNetworkStream = null;
                        }
                        if (mSslStream != null)
                        {
                            mSslStream.Dispose();
                            mSslStream = null;
                        }
                        mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        if (LocalEndPoint != null)
                            mSocket.Bind(LocalEndPoint);
                        mSocket.Connect(mIPAddress, mPort);
                        if (LocalEndPoint == null)
                            LocalEndPoint = mSocket.LocalEndPoint;
                        mSocket.ReceiveTimeout = TimeOut;
                        mSocket.SendTimeout = TimeOut;
                        mConnected = true;
                        mLastError = null;
                        mBaseNetworkStream = new PipeStream(SendBufferPool, this.LittleEndian, this.Encoding);
                        mBaseNetworkStream.Socket = mSocket;
                        mBaseNetworkStream.Encoding = this.Encoding;
                        mBaseNetworkStream.LittleEndian = this.LittleEndian;
                        mBaseNetworkStream.FlashCompleted = OnWriterFlash;
                        mSendStatus = 0;
                        mReceiveArgs = new ClientReceiveArgs();
                        if (mReceiveEventArgs != null)
                            mReceiveEventArgs.Dispose();
                        mReceiveEventArgs = new SocketAsyncEventArgsX();
                        mReceiveEventArgs.Completed += IO_Completed;
                        if (mSendEventArgs != null)
                            mSendEventArgs.Dispose();
                        mSendEventArgs = new SocketAsyncEventArgsX();
                        mSendEventArgs.Completed += IO_Completed;
                        if (this.Packet != null)
                        {
                            this.Packet = this.Packet.Clone();
                            this.Packet.Completed = this.OnPacketCompleted;
                        }
                        if (SSL)
                        {
                            mBaseNetworkStream.SSL = true;
                            mSslStream = new SslStreamX(ReceiveBufferPool, this.Encoding, this.LittleEndian, mBaseNetworkStream, new RemoteCertificateValidationCallback(ValidateServerCertificate));
                            var task = mSslStream.AuthenticateAsClientAsync(SslServiceName);
                            task.Wait();
                            mBaseNetworkStream.SSLConfirmed = true;
                            mSslStream.SyncData();
                        }
                        BeginReceive();
                        if (IsConnected)
                            OnConnected();
                    }
                }
            }
            catch (Exception e_)
            {
                mConnected = false;
                ProcessError(e_, "client connect to server error!");
            }
            return mConnected;
        }

        private void OnDisconnected()
        {
            try
            {
                Disconnected?.Invoke(this);
            }
            catch (Exception e_)
            {
                ProcessError(e_, "client process disconnected to server event error!");
            }

        }

        private void OnConnected()
        {
            try
            {
                Connected?.Invoke(this);
            }
            catch (Exception e_)
            {
                ProcessError(e_, "client process connected to server event error!");
            }
        }

        private System.Collections.Concurrent.ConcurrentQueue<object> mSendMessageQueue = new System.Collections.Concurrent.ConcurrentQueue<object>();

        private int mCount;

        public int Count => mCount;

        private void EnqueueSendMessage(object data)
        {
            mSendMessageQueue.Enqueue(data);
            System.Threading.Interlocked.Increment(ref mCount);
        }

        private object DequeueSendMessage()
        {
            object result;
            if (mSendMessageQueue.TryDequeue(out result))
            {
                System.Threading.Interlocked.Decrement(ref mCount);
            }
            return result;

        }



        public AsyncTcpClient Send(Action<PipeStream> writeHandler)
        {
            Connect();
            if (!IsConnected)
                throw mLastError;
            if (writeHandler != null)
            {
                PipeStream stream = this.Stream.ToPipeStream();
                writeHandler(stream);
                if (stream.CacheLength > 0)
                    this.Stream.Flush();
            }
            return this;
        }

        public AsyncTcpClient BatchSend(System.Collections.IEnumerable items)
        {
            Connect();
            if (!IsConnected)
                throw mLastError;
            foreach (object item in items)
            {
                EnqueueSendMessage(item);
            }
            ProcessSendMessages();
            return this;
        }

        public AsyncTcpClient Send(object data)
        {
            if (Connect())
            {
                EnqueueSendMessage(data);
                ProcessSendMessages();
            }
            return this;
        }

        private void SendCompleted()
        {
            System.Threading.Interlocked.Exchange(ref mSendStatus, 0);
            ProcessSendMessages();
        }

        private void ProcessSendMessages()
        {
            IBuffer[] items;
            if (System.Threading.Interlocked.CompareExchange(ref mSendStatus, 1, 0) == 0)
            {
                object data = DequeueSendMessage();
                BufferLink bufferLink = new BufferLink();
                PipeStream pipeStream = Stream.ToPipeStream();
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
                        WriterData(data, pipeStream);
                    }
                    data = DequeueSendMessage();
                }
                if (SSL && pipeStream.CacheLength > 0)
                    pipeStream.Flush();
                IBuffer mstreambuffer = mBaseNetworkStream.GetWriteCacheBufers();
                bufferLink.Import(mstreambuffer);
                if (bufferLink.First != null)
                {
                    CommitBuffer(bufferLink.First);
                }
                else
                {
                    System.Threading.Interlocked.Exchange(ref mSendStatus, 0);
                    if (!mSendMessageQueue.IsEmpty)
                        ProcessSendMessages();
                }
            }
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
                if (Packet == null)
                    throw new BXException("message formater is null!");
                Packet.Encode(data, null, stream);
            }
        }

        internal void CommitBuffer(IBuffer buffer)
        {
            try
            {

                Buffers.Buffer bf = (Buffers.Buffer)buffer;
                bf.UserToken = this;
                ((Buffers.Buffer)buffer).AsyncTo(this.mSendEventArgs, this.Socket);
            }
            catch (Exception e_)
            {
                Buffers.Buffer.Free(buffer);
                DisConnect();
                ProcessError(e_, "session send data error!");
            }
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
            private set
            {
                mPacket = value;
            }
        }

        private void OnPacketCompleted(IClient client, object message)
        {
            try
            {
                if (mReadMessageAwait.Pending)
                    mReadMessageAwait.Success(message);
                else
                    PacketReceive?.Invoke(this, message);
            }
            catch (Exception e_)
            {
                ProcessError(e_, "client message process error!");
            }
        }

        public void Dispose()
        {
            DisConnect();
        }

        public EventClientPacketCompleted PacketReceive
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

        public System.IO.Stream Stream
        {
            get
            {
                Connect();
                if (SSL)
                    return mSslStream;
                return mBaseNetworkStream;
            }
        }

        public bool SSL { get; set; }

        public string SslServiceName { get; set; }

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
