using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BeetleX.Buffers;
using BeetleX.EventArgs;
using System.Collections.Concurrent;
using System.Runtime;
using System.Net;
using System.Reflection;

namespace BeetleX
{
    class TcpServer : IServer
    {

        public TcpServer()
        {
            Options = new ServerOptions();
            Name = "TCP-SERVER-" + Guid.NewGuid().ToString("N");
        }

        public TcpServer(ServerOptions options)
        {
            if (options == null)
                options = new ServerOptions();
            Options = options;
            Name = "TCP-SERVER-" + Guid.NewGuid().ToString("N");
        }

        private int mCount;

        private Dispatchs.DispatchCenter<SocketAsyncEventArgsX> mReceiveDispatchCenter = null;

        private long mVersion = 0;

        private bool mInitialized = false;

        private ConcurrentDictionary<long, ISession> mSessions;

        private long mReceivBytes;

        private long mSendQuantity;

        private long mReceiveQuantity;

        private long mSendBytes;

        private Buffers.BufferPoolGroup mReceiveBufferPoolGroup;

        private Buffers.BufferPoolGroup mSendBufferPoolGroup;

        private System.Threading.Timer mDetectionTimer;

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

        public ServerOptions Options
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            set;
        }

        public IPacket Packet
        {
            get;
            set;
        }

        public ServerStatus Status
        {
            get;
            set;
        }

        public long Version
        {
            get
            {
                return mVersion;
            }
        }

        public Buffers.BufferPoolGroup SendBufferPool
        {
            get
            {
                return mSendBufferPoolGroup;
            }
        }

        public BufferPoolGroup ReceiveBufferPool
        {
            get
            {
                return mReceiveBufferPoolGroup;
            }
        }

        public IServerHandler Handler
        {
            get;
            set;
        }

        public int Count
        {
            get
            {
                if (mSessions == null)
                    return 0;
                return mCount;
            }
        }

        private void AddSession(ISession session)
        {
            mSessions[session.ID] = session;
            System.Threading.Interlocked.Increment(ref mCount);
            System.Threading.Interlocked.Increment(ref mVersion);
        }

        private void ClearSession()
        {
            ISession[] sessions = GetOnlines();
            foreach (ISession item in sessions)
                item.Dispose();
        }

        private void RemoveSession(ISession session)
        {
            ISession value;
            if (mSessions.TryRemove(session.ID, out value))
            {
                System.Threading.Interlocked.Decrement(ref mCount);
                System.Threading.Interlocked.Increment(ref mVersion);
            }
        }

        #region server init start stop

        private int mTimeOutCheckTime = 1000 * 60;

        private void ToInitialize()
        {
            if (!mInitialized)
            {
                mReceiveDispatchCenter = new Dispatchs.DispatchCenter<SocketAsyncEventArgsX>(ProcessReceiveArgs, Options.IOQueues);
                int maxBufferSize;
                if (Options.BufferPoolMaxMemory == 0)
                {
                    Options.BufferPoolMaxMemory = 500;
                }
                maxBufferSize = (int)(((long)Options.BufferPoolMaxMemory * 1024 * 1024) / Options.BufferSize / Options.BufferPoolGroups);
                if (maxBufferSize < Options.BufferPoolSize)
                    maxBufferSize = Options.BufferPoolSize;
                mReceiveBufferPoolGroup = new BufferPoolGroup(Options.BufferSize, Options.BufferPoolSize, maxBufferSize, Options.BufferPoolGroups);
                mSendBufferPoolGroup = new BufferPoolGroup(Options.BufferSize, Options.BufferPoolSize, maxBufferSize, Options.BufferPoolGroups);
                mSessions = new ConcurrentDictionary<long, ISession>();
                mInitialized = true;
                mAcceptDispatcher = new Dispatchs.DispatchCenter<AcceptSocketInfo>(AcceptProcess, Math.Min(Environment.ProcessorCount, 16));
                if (Options.SessionTimeOut > 0)
                {
                    if (Options.SessionTimeOut * 1000 < mTimeOutCheckTime)
                        mTimeOutCheckTime = Options.SessionTimeOut * 1000;
                    else
                        mTimeOutCheckTime = Options.SessionTimeOut * 1000;

                    if (mDetectionTimer != null)
                        mDetectionTimer.Dispose();
                    mDetectionTimer = new System.Threading.Timer(OnDetectionHandler, null,
                        mTimeOutCheckTime, mTimeOutCheckTime);
                    if (EnableLog(LogType.Info))
                        Log(LogType.Info, null, "detection sessions timeout with {0}s", Options.SessionTimeOut);
                }
            }
        }

        private void OnDetectionHandler(object state)
        {
            mDetectionTimer.Change(-1, -1);
            try
            {
                List<ISession> sessions = new List<ISession>();
                double time = GetRunTime();
                foreach (var item in GetOnlines())
                {
                    if (item.TimeOut < time)
                    {
                        sessions.Add(item);
                    }
                }
                if (sessions.Count > 0)
                    OnSessionTimeout(sessions);
                if (EnableLog(LogType.Info))
                {
                    Log(LogType.Info, null, "detection sessions completed");
                }
            }
            catch (Exception e_)
            {
                Error(e_, null, "detection sessions error");
            }
            finally
            {
                mDetectionTimer.Change(mTimeOutCheckTime, mTimeOutCheckTime);
            }
        }

        private void OnListenAcceptCallBack(AcceptSocketInfo e)
        {
            Task.Run(() => AcceptProcess(e));
        }

        public bool Open()
        {
            try
            {

                ToInitialize();
                Status = ServerStatus.Start;
                foreach (ListenHandler item in this.Options.Listens)
                {
                    item.SyncAccept = Options.SyncAccept;
                    item.Run(this, OnListenAcceptCallBack);
                }
                if (!GCSettings.IsServerGC)
                {
                    if (EnableLog(LogType.Warring))
                        Log(LogType.Warring, null, "no serverGC mode,please enable serverGC mode!");
                }
                //Log(LogType.Info, null,
                //   $"BeetleX [V:{typeof(TcpServer).Assembly.GetName().Version}]");
                //Log(LogType.Info, null,
                //    $"Environment [ServerGC:{GCSettings.IsServerGC}][IOQueue:{Options.IOQueueEnabled}|n:{Options.IOQueues}][Threads:{Environment.ProcessorCount}][Private Buffer:{Options.PrivateBufferPool}|{Options.PrivateBufferPoolSize/1024}KB]");
                if (WriteLogo != null)
                    WriteLogo();
                else
                    OnWriteLogo();
                return true;
            }
            catch (Exception e_)
            {
                Status = ServerStatus.StartError;
                if (EnableLog(LogType.Error))
                    Error(e_, null, "server start error!");
            }
            return false;
        }

        public Action WriteLogo { get; set; }

        private void OnWriteLogo()
        {
            AssemblyCopyrightAttribute productAttr = typeof(BeetleX.BXException).Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            var logo = "\r\n";
            logo += " -----------------------------------------------------------------------------\r\n";
            logo +=
@"          ____                  _     _         __   __
         |  _ \                | |   | |        \ \ / /
         | |_) |   ___    ___  | |_  | |   ___   \ V / 
         |  _ <   / _ \  / _ \ | __| | |  / _ \   > <  
         | |_) | |  __/ |  __/ | |_  | | |  __/  / . \ 
         |____/   \___|  \___|  \__| |_|  \___| /_/ \_\ 

                                            tcp framework   

";
            logo += " -----------------------------------------------------------------------------\r\n";
            logo += $" {productAttr.Copyright}\r\n";
            logo += $" ServerGC [{GCSettings.IsServerGC}]\r\n";
            logo += $" Version  [{typeof(BeetleX.BXException).Assembly.GetName().Version}]\r\n";
            logo += " -----------------------------------------------------------------------------\r\n";
            foreach (var item in this.Options.Listens)
            {
                logo += $" {item}\r\n";
            }
            logo +=" -----------------------------------------------------------------------------\r\n";
            Log(LogType.Info, null, logo);
        }

        public void Resume()
        {
            Status = ServerStatus.Start;
        }

        public bool Pause()
        {
            Status = ServerStatus.Stop;
            return true;
        }

        #endregion

        #region session accept

        private void SslAuthenticateAsyncCallback(IAsyncResult ar)
        {
            Tuple<TcpSession, SslStream> state = (Tuple<TcpSession, SslStream>)ar.AsyncState;
            ISession session = state.Item1;
            TcpServer server = (TcpServer)session.Server;
            try
            {
                if (server.EnableLog(LogType.Info))
                    server.Log(LogType.Info, session, "{0} end ssl Authenticate", session.RemoteEndPoint);
                SslStream sslStream = state.Item2;
                sslStream.EndAuthenticateAsServer(ar);
                EventArgs.ConnectedEventArgs cead = new EventArgs.ConnectedEventArgs();
                cead.Server = server;
                cead.Session = session;
                server.OnConnected(cead);
                ((TcpSession)session).SyncSSLData();
                server.BeginReceive(state.Item1);

            }
            catch (Exception e_)
            {
                if (this.EnableLog(LogType.Warring))
                    this.Log(LogType.Warring, state?.Item1, $"create session ssl authenticate callback error {e_.Message}@{e_.StackTrace}");
                if (session != null)
                    session.Dispose();
            }
        }

        private void ConnectedProcess(AcceptSocketInfo e)
        {
            TcpSession session = new TcpSession();
            session.MaxWaitMessages = Options.MaxWaitMessages;
            session.Socket = e.Socket;
            session.Server = this;
            session.Host = e.Listen.Host;
            session.Port = e.Listen.Port;
            session.ReceiveBufferPool = this.ReceiveBufferPool.Next();
            session.SendBufferPool = this.SendBufferPool.Next();
            session.SSL = e.Listen.SSL;
            session.Initialization(this, null);
            session.SendEventArgs.Completed += IO_Completed;
            session.ReceiveEventArgs.Completed += IO_Completed;
            session.LittleEndian = Options.LittleEndian;
            session.RemoteEndPoint = e.Socket.RemoteEndPoint;
            if (this.Packet != null)
            {
                session.Packet = this.Packet.Clone();
                session.Packet.Completed = OnPacketDecodeCompleted;
            }

            session.ReceiveDispatcher = mReceiveDispatchCenter.Next();
            AddSession(session);
            if (!e.Listen.SSL)
            {
                EventArgs.ConnectedEventArgs cead = new EventArgs.ConnectedEventArgs();
                cead.Server = this;
                cead.Session = session;
                OnConnected(cead);
                BeginReceive(session);

            }
            else
            {
                if (EnableLog(LogType.Info))
                    Log(LogType.Info, session, "{0} begin ssl Authenticate", session.RemoteEndPoint);
                session.CreateSSL(SslAuthenticateAsyncCallback, e.Listen, this);
            }
        }

        private Dispatchs.DispatchCenter<AcceptSocketInfo> mAcceptDispatcher;

        private void AcceptProcess(AcceptSocketInfo e)
        {
            try
            {
                EventArgs.ConnectingEventArgs cea = new EventArgs.ConnectingEventArgs();
                cea.Server = this;
                cea.Socket = e.Socket;
                EndPoint endPoint = e.Socket.RemoteEndPoint;
                OnConnecting(cea);
                if (cea.Cancel)
                {
                    if (EnableLog(LogType.Debug))
                        Log(LogType.Debug, null, $"cancel {endPoint} connect");
                    CloseSocket(e.Socket);

                }
                else
                {
                    if (e.Socket.Connected)
                    {
                        ConnectedProcess(e);
                        if (EnableLog(LogType.Debug))
                            Log(LogType.Debug, null, $" {endPoint} connected");
                    }
                    else
                    {
                        if (EnableLog(LogType.Info))
                            Log(LogType.Info, null, $"Connected process {endPoint} is disconnected");
                    }
                }
            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Error))
                    Error(e_, null, "accept socket process error");
            }

        }



        #endregion

        #region session send and receive
        private void SessionSendData(ISession session)
        {
            ((TcpSession)session).ProcessSendMessages();
        }

        private void BeginReceive(ISession session)
        {
            if (EnableLog(LogType.Info))
                Log(LogType.Info, session, "{0} begin receive", session.RemoteEndPoint);
            if (session.IsDisposed)
            {
                if (EnableLog(LogType.Info))
                    Log(LogType.Info, session, $"{session.RemoteEndPoint} begin receive cancel connection disposed");
                return;
            }
            Buffers.Buffer buffer=null;
            try
            {
                buffer = (Buffers.Buffer)session.ReceiveBufferPool.Pop();
                buffer.AsyncFrom(session.ReceiveEventArgs, session);
            }
            catch (Exception e_)
            {
                buffer?.Free();
                if (EnableLog(LogType.Warring))
                    Log(LogType.Warring, session, $"{session.RemoteEndPoint} receive data error {e_.Message}@{e_.StackTrace}");
                session.Dispose();
            }
        }

        private void IO_Completed(object sender, System.Net.Sockets.SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgsX ex = (SocketAsyncEventArgsX)e;
            if (ex.IsReceive)
            {
                ReceiveCompleted(e);
            }
            else
            {
                SendCompleted(e);
            }
        }

        private void ProcessReceiveArgs(SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgsX ex = (SocketAsyncEventArgsX)e;
            ISession session = ex.Session;
            try
            {
                if (EnableLog(LogType.Info))
                    Log(LogType.Info, session, $"{session.RemoteEndPoint} receive {e.BytesTransferred} length completed");

                if (EnableLog(LogType.Trace))
                {
                    Log(LogType.Trace, session, "{0} receive hex:{1}", session.RemoteEndPoint,
                         BitConverter.ToString(ex.BufferX.Data, 0, e.BytesTransferred).Replace("-", string.Empty).ToLower()
                        );
                }
                if (this.Options.Statistical)
                {
                    System.Threading.Interlocked.Increment(ref mReceiveQuantity);
                    System.Threading.Interlocked.Add(ref mReceivBytes, e.BytesTransferred);
                }
                ex.BufferX.Postion = 0;
                ex.BufferX.SetLength(e.BytesTransferred);
                session.Receive(ex.BufferX);
                if (session.SocketProcessHandler != null)
                    session.SocketProcessHandler.ReceiveCompleted(session, e);
            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Warring))
                    Log(LogType.Warring, session, $"{session.RemoteEndPoint} receive data completed socket error {e.SocketError} {e_.Message}@{e_.StackTrace}");
            }
            finally
            {
                BeginReceive(session);
            }
        }

        private void ReceiveCompleted(SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgsX ex = (SocketAsyncEventArgsX)e;
            ISession session = ex.Session;
            try
            {
                if (e.SocketError == System.Net.Sockets.SocketError.Success && e.BytesTransferred > 0)
                {
                    if (session.Server.Options.IOQueueEnabled)
                    {
                        ((TcpSession)session).ReceiveDispatcher.Enqueue(ex);
                    }
                    else
                    {
                        ProcessReceiveArgs(e);
                    }

                }
                else
                {
                    ex.BufferX?.Free();
                    if (EnableLog(LogType.Debug))
                        Log(LogType.Debug, session, $"{session.RemoteEndPoint} receive close error {e.SocketError} receive:{e.BytesTransferred}");
                    session.Dispose();

                }
            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Warring))
                    Log(LogType.Warring, session, $"{session.RemoteEndPoint} receive data completed socket error {e.SocketError} {e_.Message}@{e_.StackTrace}");
            }

        }

        private void SendCompleted(SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgsX ex = (SocketAsyncEventArgsX)e;
            Buffers.Buffer buffer = (Buffers.Buffer)ex.BufferX;
            ISession session = ex.Session;
            try
            {
                if (e.SocketError == SocketError.IOPending || e.SocketError == SocketError.Success)
                {
                    if (session.Server.EnableLog(LogType.Trace))
                    {
                        session.Server.Log(LogType.Trace, session, "{0} send hex:{1}", session.RemoteEndPoint,
                             BitConverter.ToString(ex.BufferX.Data, 0, e.BytesTransferred).Replace("-", string.Empty).ToLower()
                            );
                    }
                    if (this.Options.Statistical)
                    {
                        System.Threading.Interlocked.Increment(ref mSendQuantity);
                        System.Threading.Interlocked.Add(ref mSendBytes, e.BytesTransferred);
                    }
                    if (e.BytesTransferred < e.Count)
                    {
                        buffer.Postion = (buffer.Postion + e.BytesTransferred);
                        buffer.SetLength(buffer.Length - e.BytesTransferred);
                        buffer.AsyncTo(session.SendEventArgs, session);
                    }
                    else
                    {
                        try
                        {
                            buffer.Completed?.Invoke(buffer);
                        }
                        catch
                        {

                        }
                        IBuffer nextbuf = buffer.Next;
                        buffer.Free();
                        if (nextbuf != null)
                        {
                            ((TcpSession)session).CommitBuffer(nextbuf);
                        }
                        else
                        {
                            ((TcpSession)session).SendCompleted();
                            if (session.SocketProcessHandler != null)
                            {
                                try
                                {
                                    session.SocketProcessHandler.SendCompleted(session, e);
                                }
                                catch (Exception ce_)
                                {
                                    if (EnableLog(LogType.Error))
                                        Error(ce_, ex.Session, "{0} send data completed process handler error {1}!", ex.Session.RemoteEndPoint, ce_.Message);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (EnableLog(LogType.Debug))
                        Log(LogType.Debug, session, $"{session.RemoteEndPoint} send close error {e.SocketError} receive:{e.BytesTransferred}");
                    Buffers.Buffer.Free(buffer);
                    session.Dispose();
                }
            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Warring))
                    Log(LogType.Warring, session, $"{session.RemoteEndPoint} send data completed socket error {e.SocketError} {e_.Message}@{e_.StackTrace}");
            }

        }
        #endregion

        #region server and session event

        private void OnSessionTimeout(IList<ISession> items)
        {
            try
            {
                if (Handler != null)
                    Handler.SessionDetection(this, new EventArgs.SessionDetectionEventArgs { Server = this, Sesions = items });
            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Error))
                    Error(e_, null, "detection session  process error");
            }
        }

        public void SessionReceive(SessionReceiveEventArgs e)
        {
            if (e.Session == null || e.Session.IsDisposed)
                return;
            if (e.Session.Packet != null)
            {
                try
                {
                    e.Session.Packet.Decode(e.Session, e.Stream);
                }
                catch (Exception e_)
                {
                    if (EnableLog(LogType.Error))
                        Error(e_, e.Session, "{0} session  buffer decoding error! ", e.Session.RemoteEndPoint);
                    e.Session.Dispose();
                }
            }
            else
            {
                try
                {
                    if (Handler != null)
                        Handler.SessionReceive(e.Server, e);
                }
                catch (Exception e_)
                {
                    if (EnableLog(LogType.Error))
                        Error(e_, e.Session, "{0} session  buffer process error! ", e.Session.RemoteEndPoint);
                }
            }

        }

        private void OnPacketDecodeCompleted(object server, PacketDecodeCompletedEventArgs e)
        {
            try
            {
                if (Handler != null)
                {
                    if (EnableLog(LogType.Debug))
                        Log(LogType.Debug, e.Session, "{0} session packet decode completed {1}", e.Session.RemoteEndPoint, e.Message.GetType());
                    Handler.SessionPacketDecodeCompleted(this, e);

                }
            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Error))
                    Error(e_, e.Session, "{0} session packet  message process error !", e.Session.RemoteEndPoint);
            }
        }

        private void OnConnecting(ConnectingEventArgs e)
        {
            try
            {
                if (Handler != null)
                    Handler.Connecting(this, e);
            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Error))
                    Error(e_, null, "Server session connecting process error!");
            }
        }

        private void OnConnected(ConnectedEventArgs e)
        {
            try
            {
                e.Session.Authentication = AuthenticationType.Connected;
                if (Handler != null)
                    Handler.Connected(this, e);

            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Error))
                    Error(e_, e.Session, "Server session connected process error!");
            }
        }

        public void Log(LogType type, ISession session, string message)
        {
            try
            {
                if (Handler != null)
                    Handler.Log(this, new ServerLogEventArgs() { Message = message, Server = this, Type = type, Session = session });
            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Error))
                    Error(e_, session, "Server log process error!");
            }

        }

        public void Log(LogType type, ISession session, string message, params object[] parameters)
        {
            Log(type, session, string.Format(message, parameters));

        }

        public void Error(Exception error, ISession session, string message)
        {
            try
            {
                if (Handler != null)
                    Handler.Error(this, new ServerErrorEventArgs() { Message = message, Server = this, Error = error, Session = session });
            }
            catch
            {
            }
        }

        public void Error(Exception error, ISession session, string message, params object[] parameters)
        {
            Error(error, session, string.Format(message, parameters));
        }

        public void CloseSession(ISession session)
        {
            try
            {
                RemoveSession(session);
                if (Handler != null)
                    Handler.Disconnect(this, new SessionEventArgs { Server = this, Session = session });
            }
            catch (Exception e_)
            {
                if (EnableLog(LogType.Error))
                    Error(e_, session, "close session error!");
            }
            finally
            {
                CloseSocket(session.Socket);
            }
        }

        internal static void CloseSocket(Socket socket)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            try
            {
                socket.Dispose();
            }
            catch { }
        }
        #endregion

        public void Dispose()
        {
            ClearSession();
            mInitialized = false;

            Status = ServerStatus.Closed;
            foreach (var item in Options.Listens)
                item.Dispose();
            if (mReceiveDispatchCenter != null)
                mReceiveDispatchCenter.Dispose();


        }

        public bool Send(object data, ISession session)
        {
            if (data == null)
                return false;
            return session.Send(data);
        }

        public bool[] Send(object message, params ISession[] sessions)
        {
            if (sessions != null && sessions.Length > 0)
                return Send(message, new ArraySegment<ISession>(sessions, 0, sessions.Length));
            return new bool[] { false };

        }

        private bool[] OnSend(object message, System.ArraySegment<ISession> sessions)
        {
            bool[] result = new bool[sessions.Count];
            for (int i = 0; i < sessions.Count; i++)
            {
                result[i] = Send(message, sessions.Array[sessions.Offset + i]);
            }
            return result;
        }

        public bool[] Send(object message, System.ArraySegment<ISession> sessions)
        {
            bool[] result = new bool[sessions.Count];
            if (message is IBuffer || message is IWriteHandler)
            {
                return OnSend(message, sessions);
            }
            else
            {
                if (this.Packet == null)
                {
                    if (EnableLog(LogType.Error))
                        Error(new BXException("server message formater is null!"), null, "server message formater is null!");
                }
                else
                {
                    if (Options.Combined > 0 && sessions.Count >= Options.Combined)
                    {
                        byte[] data = Packet.Encode(message, this);
                        return OnSend(data, sessions);
                    }
                    else
                    {
                        return OnSend(message, sessions);
                    }
                }
            }
            return result;
        }

        public long GetRunTime()
        {
            return TimeWatch.GetElapsedMilliseconds();
        }

        public void UpdateSession(ISession session)
        {
            if (Options.SessionTimeOut > 0)
                session.TimeOut = GetRunTime() + Options.SessionTimeOut * 1000;
            if (EnableLog(LogType.Debug))
            {
                Log(LogType.Debug, session, "{0} update active time", session.RemoteEndPoint);
            }
        }

        class OnlineSegment
        {

            public OnlineSegment()
            {
                Arrays = new ISession[0];
            }

            public ISession[] Arrays { get; set; }

            public long Version
            {
                get; set;
            }

        }

        private OnlineSegment mOnlines = new OnlineSegment();

        private int mGetOnlinesStatus = 0;

        public ISession[] GetOnlines()
        {
            if (mOnlines.Version != this.Version)
            {
                if (System.Threading.Interlocked.CompareExchange(ref mGetOnlinesStatus, 1, 0) == 0)
                {
                    try
                    {
                        if (mOnlines.Version != this.Version)
                        {
                            mOnlines.Arrays = mSessions.Values.ToArray();
                            mOnlines.Version = this.Version;
                        }
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref mGetOnlinesStatus, 0);
                    }
                }
            }
            return mOnlines.Arrays;
        }

        private long mLastSend;

        private long mLastReceive;

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (this.Status == ServerStatus.Start)
            {
                foreach (ListenHandler item in Options.Listens)
                {
                    sb.AppendFormat($"Listen @ {item.IPEndPoint}\r\n");
                }
                sb.AppendFormat("Connections:{0}    \r\n",
                    Count.ToString("###,###,##0").PadLeft(15));

                sb.AppendFormat("IO  Receive:{0}/s   Send:{1}/s\r\n",
                (ReceiveQuantity - mLastReceive).ToString("###,###,##0").PadLeft(15),
                (SendQuantity - mLastSend).ToString("###,###,##0").PadLeft(15));
                mLastReceive = ReceiveQuantity;
                mLastSend = SendQuantity;

                sb.AppendFormat("IO  Receive:{0}     Send:{1}\r\n",
                    ReceiveQuantity.ToString("###,###,##0").PadLeft(15),
                    SendQuantity.ToString("###,###,##0").PadLeft(15));


                sb.AppendFormat("BW  Receive:{0}KB   Send:{1}KB\r\n",
                    (ReceivBytes / 1024).ToString("###,###,##0").PadLeft(15),
                    (SendBytes / 1024).ToString("###,###,##0").PadLeft(15));
                sb.AppendLine("");
            }
            return sb.ToString();
        }

        public ISession GetSession(long id)
        {
            ISession result;
            mSessions.TryGetValue(id, out result);
            return result;
        }

        public bool EnableLog(LogType logType)
        {
            return (int)(this.Options.LogLevel) <= (int)logType;
        }

        public IServer Setting(Action<ServerOptions> handler)
        {
            if (handler != null)
                handler(this.Options);
            return this;
        }
    }
}
