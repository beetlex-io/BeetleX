using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using BeetleX.Buffers;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using System.Net.Http.Headers;
using System.Threading;

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

        Task<ConnectStatus> Connect();

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


    public interface IClientSocketProcessHandler
    {
        void ReceiveCompleted(IClient client, SocketAsyncEventArgs e);

        void SendCompleted(IClient client, SocketAsyncEventArgs e, bool end);
    }

    public struct ConnectStatus
    {

        public bool Connected;

        public bool NewConnection;
    }


    public class AsyncTcpClient : IClient, IDisposable
    {

        static AsyncTcpClient()
        {
            AwaiterDispatchCenter = new Dispatchs.DispatchCenter<(TaskCompletionSource<PipeStream>, object)>(OnProcess);
        }

        public static Dispatchs.DispatchCenter<(TaskCompletionSource<PipeStream>, object)> AwaiterDispatchCenter { get; set; }

        private static void OnProcess((TaskCompletionSource<PipeStream>, object) e)
        {
            if (e.Item2 is Exception error)
            {
                e.Item1.TrySetException(error);
            }
            else
            {
                e.Item1.TrySetResult((PipeStream)e.Item2);
            }
        }

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

        private bool mIsUnixDomainSocket = false;

        public IBufferPool ReceiveBufferPool { get; set; }

        public IBufferPool SendBufferPool { get; set; }

        public bool AutoReceive { get; set; } = true;

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
#if !NETSTANDARD2_0
            var unixSocket = SocketFactory.GetUnixSocketUrl(host);
            if (unixSocket.IsUnixSocket)
            {
                mPort = port;
                if (packet != null)
                {
                    mPacket = packet;
                    mPacket.Completed = OnPacketCompleted;
                }
                mIsUnixDomainSocket = true;
                mEndPoint = new UnixDomainSocketEndPoint(unixSocket.SockFile);
                return;
            }
#endif
            var talk = Dns.GetHostAddressesAsync(host);
            talk.Wait(10000);
            IPAddress[] ips = talk.Result;
            if (ips.Length == 0)
                throw new BXException("get host's address error");
            foreach (IPAddress item in ips)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork
                     || item.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    mIPAddress = item;
                    break;
                }
            }
            mPort = port;
            mEndPoint = new IPEndPoint(mIPAddress, mPort);
            if (packet != null)
            {
                mPacket = packet;
                mPacket.Completed = OnPacketCompleted;
            }
        }

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

        private EndPoint mEndPoint;

        private PipeStream mBaseNetworkStream = null;

        private SslStreamX mSslStream = null;

        private long mReceiveVersion = 0;

        private object mLockReceive = new object();

        private TaskCompletionSource<PipeStream> mReceiveCompletionSource;

        private long mAwaitReceive = 0;

        private int mPort;

        public void ProcessError(Exception e_, string message = null)
        {
            mLastError = e_;
            ClientErrorArgs e = new ClientErrorArgs();
            e.Error = e_;
            e.Message = message;
            try
            {
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
                        SocketFactory.CloseSocket(mSocket);
                        mSocket = null;
                    }
                    mReceiveEventArgs?.Clear();
                    mReceiveEventArgs?.Dispose();
                    mReceiveEventArgs = null;
                    mSendEventArgs?.Clear();
                    mSendEventArgs?.Dispose();
                    mSendEventArgs = null;
                    mProperties.Clear();
                    mBaseNetworkStream?.Dispose();
                    mBaseNetworkStream = null;
                    mSslStream?.Dispose();
                    mSslStream = null;
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

        private void OnReceive()
        {
            var error = mSslStream?.SyncDataError;
            if (SSL && error != null)
            {
                ProcessError(error, $"sync SslStream data error {error.Message}@{error.StackTrace}");
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
                    TaskCompletionSource<PipeStream> awaiter = null;
                    lock (mLockReceive)
                    {
                        mReceiveVersion++;
                        if (mReceiveCompletionSource != null)
                        {
                            mAwaitReceive = mReceiveVersion;
                            awaiter = mReceiveCompletionSource;
                            mReceiveCompletionSource = null;

                        }
                    }
                    if (awaiter != null)
                    {
                        AwaiterDispatchCenter.Get(this).Enqueue((awaiter, Stream.ToPipeStream()));
                    }
                    else
                    {
                        mReceiveArgs.Stream = this.Stream;
                        DataReceive?.Invoke(this, mReceiveArgs);
                    }
                }
                catch (Exception e_)
                {
                    ProcessError(e_, "client  buffer process error!");
                }

            }
        }

        private void ImportReceive(IBuffer buffer)
        {
            mBaseNetworkStream.Import(buffer);
            if (!SSL)
                OnReceive();
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
                    tcpclient.ImportReceive(ex.BufferX);
                    if (tcpclient.AutoReceive)
                        tcpclient.BeginReceive();
                }
                else
                {
                    ex.BufferX?.Free();
                    tcpclient.DisConnect();
                    tcpclient.ProcessError(new SocketException((int)e.SocketError), $"Receive error {e.SocketError} BytesTransferred {e.BytesTransferred}");

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
                        try
                        {
                            buffer.Completed?.Invoke(buffer);
                        }
                        catch
                        {

                        }
                        buffer.Free();
                        if (tcpclient.SocketProcessHandler != null)
                        {
                            try
                            {
                                tcpclient.SocketProcessHandler.SendCompleted(tcpclient, e, nextbuf == null);
                            }
                            catch { }
                        }
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
                    tcpclient.DisConnect();
                    tcpclient.ProcessError(new SocketException((int)e.SocketError), $"Send error {e.SocketError}");

                }

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


        public int ConnectTimeOut { get; set; } = 0;

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

        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        protected virtual bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (CertificateValidationCallback != null)
                return CertificateValidationCallback(sender, certificate, chain, sslPolicyErrors);
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            return false;
        }



        private Task CreateSocket()
        {
#if !NETSTANDARD2_0
            if (mIsUnixDomainSocket)
            {
                mSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                return mSocket.ConnectAsync(mEndPoint);
            }
#endif
            mSocket = new Socket(mIPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (LocalEndPoint != null)
                mSocket.Bind(LocalEndPoint);
            return mSocket.ConnectAsync(mEndPoint);

        }


        private SemaphoreSlim mConnectSemaphoreSlim = new SemaphoreSlim(1, 5);

        public async Task<ConnectStatus> Connect()
        {
            ConnectStatus result = new ConnectStatus();
            result.Connected = false;
            result.NewConnection = false;
            if (IsConnected)
            {
                result.Connected = true;
                return result;
            }
            bool completed = false;
            try
            {
                completed = await mConnectSemaphoreSlim.WaitAsync(5000);
                if (!completed)
                {
                    if (IsConnected)
                    {
                        result.Connected = true;
                        return result;
                    }
                    throw new TimeoutException($"wait tcp connecting timeout!");
                }
                if (!IsConnected)
                {
                    mLastError = null;
                    mBaseNetworkStream?.Dispose();
                    mBaseNetworkStream = null;
                    mSslStream?.Dispose();
                    mSslStream = null;
                    if (ConnectTimeOut > 0)
                    {
                        var task = Task.Run(async () => await CreateSocket());
                        if (!task.Wait(ConnectTimeOut))
                        {
                            mSocket?.Dispose();
                            throw new TimeoutException($"connect {mIPAddress}@{mPort} timeout! task status:{task.Status}");
                        }
                    }
                    else
                    {
                        await CreateSocket();
                    }
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
                    mReceiveEventArgs?.Dispose();
                    mReceiveEventArgs = new SocketAsyncEventArgsX();
                    mReceiveEventArgs.Completed += IO_Completed;
                    mSendEventArgs?.Dispose();
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
                        await OnSslAuthenticate(mSslStream);
                        mBaseNetworkStream.SSLConfirmed = true;
                        mSslStream.SyncData(OnReceive);
                    }
                    if (AutoReceive)
                        BeginReceive();
                    if (IsConnected)
                        OnConnected();
                    result.NewConnection = true;
                }

            }
            catch (Exception e_)
            {
                mConnected = false;
                ProcessError(e_, $"client connect to server error {e_.Message}!");
            }
            finally
            {
                if (completed)
                    mConnectSemaphoreSlim.Release();
            }
            result.Connected = mConnected;
            return result;
        }
        public SslProtocols? SslProtocols { get; set; } = System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12;

        public X509CertificateCollection CertificateCollection { get; private set; } = new X509CertificateCollection();

        protected virtual async Task OnSslAuthenticate(SslStream sslStream)
        {
            if (SslProtocols == null)
                SslProtocols = System.Security.Authentication.SslProtocols.Tls | System.Security.Authentication.SslProtocols.Tls11 |
                     System.Security.Authentication.SslProtocols.Tls12;
            await sslStream.AuthenticateAsClientAsync(SslServiceName, CertificateCollection.Count > 0 ? CertificateCollection : null, SslProtocols.Value, false);

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



        public async Task<AsyncTcpClient> Send(Action<PipeStream> writeHandler)
        {
            var status = await Connect();
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

        public async Task<AsyncTcpClient> BatchSend(System.Collections.IEnumerable items)
        {
            var status = await Connect();
            if (!IsConnected)
                throw mLastError;
            foreach (object item in items)
            {
                EnqueueSendMessage(item);
            }
            ProcessSendMessages();
            return this;
        }

        public async Task<AsyncTcpClient> Send(object data)
        {
            var status = await Connect();
            if (status.Connected)
            {
                EnqueueSendMessage(data);
                ProcessSendMessages();
            }
            else
            {
                if (data is IBuffer buffer)
                {
                    Buffers.Buffer.Free(buffer);
                }
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
            if (data is IWriteHandler handler)
            {
                handler.Write(stream);
                handler.Completed?.Invoke(handler);
            }
            else
            {
                if (Packet == null)
                    throw new BXException("message formater is null!");
                Packet.Encode(data, null, stream);
                if (data is IMessageSubmitHandler action)
                    action.Execute(this, data);
            }
        }

        internal void CommitBuffer(IBuffer buffer)
        {
            try
            {

                Buffers.Buffer bf = (Buffers.Buffer)buffer;
                bf.Postion = 0;
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
                //if (mReadMessageAwait.Pending)
                //    mReadMessageAwait.Success(message);
                //else
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
                var task = Connect();
                if (!task.Wait(10000))
                {
                    throw new BXException("connect timeout!");
                }
                if (task.Result.Connected)
                {
                    if (SSL)
                        return mSslStream;
                    return mBaseNetworkStream;
                }
                else
                {
                    throw LastError;
                }
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
