using BeetleX.Buffers;
using BeetleX.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX
{

    public interface IApplication
    {
        void Init(IServer server);
    }


    public struct EventMessageReceiveArgs<APPLICATION, SESSION, MSG>
  where SESSION : ISessionToken
where APPLICATION : IApplication
    {
        public ISession NetSession { get; set; }

        public SESSION Session { get; set; }

        public APPLICATION Application { get; set; }

        public MSG Message { get; set; }

        public void Return(object message)
        {
            NetSession.Send(message);
        }
        public ILoger GetLoger(LogType type)
        {
            if ((int)(this.NetSession.Server.Options.LogLevel) <= (int)type)
            {
                return NetSession.Server;

            }
            return null;
        }
    }

    public struct EventStreamReceiveArgs<APPLICATION, SESSION>
      where SESSION : ISessionToken
    where APPLICATION : IApplication
    {
        public ISession NetSession { get; set; }

        public SESSION Session { get; set; }

        public APPLICATION Application { get; set; }

        public PipeStream Reader { get; set; }

        public PipeStream Writer { get; set; }

        public ILoger GetLoger(LogType type)
        {
            if ((int)(this.NetSession.Server.Options.LogLevel) <= (int)type)
            {
                return NetSession.Server;

            }
            return null;
        }
        public void Flush()
        {
            NetSession.Stream.Flush();
        }
    }

    class MessageProcessHandler<APPLICATION, SESSION, MESSAGE> : IMessageProcessHandler
         where SESSION : ISessionToken, new()
    where APPLICATION : IApplication, new()
    {
        public Action<EventMessageReceiveArgs<APPLICATION, SESSION, MESSAGE>> Handler { get; set; }
        public void Execute(object netsession, object application, object session, object message)
        {
            EventMessageReceiveArgs<APPLICATION, SESSION, MESSAGE> e = new EventMessageReceiveArgs<APPLICATION, SESSION, MESSAGE>();
            e.NetSession = (ISession)netsession;
            e.Application = (APPLICATION)application;
            e.Session = (SESSION)session;
            e.Message = (MESSAGE)message;
            Handler?.Invoke(e);
        }
    }

    interface IMessageProcessHandler
    {
        void Execute(object netsession, object application, object session, object message);
    }
    public class ServerBuilder<APPLICATION, SESSION, PACKET> : ServerBuilder<APPLICATION, SESSION>
    where SESSION : ISessionToken, new()
    where PACKET : IPacket, new()
    where APPLICATION : IApplication, new()
    {
        protected override IServer CreateServer()
        {
            return SocketFactory.CreateTcpServer(this, new PACKET(), this.ServerOptions);
        }

        private System.Collections.Concurrent.ConcurrentDictionary<Type, IMessageProcessHandler> mMessageHandlers = new System.Collections.Concurrent.ConcurrentDictionary<Type, IMessageProcessHandler>();

        public new ServerBuilder<APPLICATION, SESSION, PACKET> SetOptions(Action<ServerOptions> handler)
        {
            handler(this.ServerOptions);
            return this;
        }

    }

    public class ServerBuilder<APPLICATION, SESSION> : IServerHandler, IDisposable
        where SESSION : ISessionToken, new()
        where APPLICATION : IApplication, new()
    {
        IServer IServerHandler.Server { get; set; }

        public IServer AppServer => ((IServerHandler)this).Server;



        public ServerOptions ServerOptions { get; private set; } = new ServerOptions();

        public ServerBuilder<APPLICATION, SESSION> SetOptions(Action<ServerOptions> handler)
        {
            handler(this.ServerOptions);
            return this;
        }

        public virtual ServerBuilder<APPLICATION, SESSION> OnMessageReceive<Message>(Action<EventMessageReceiveArgs<APPLICATION, SESSION, Message>> handler)
        {
            MessageProcessHandler<APPLICATION, SESSION, Message> e = new MessageProcessHandler<APPLICATION, SESSION, Message>();
            e.Handler = handler;
            mMessageHandlers[typeof(Message)] = e;
            return this;
        }

        private Action<EventMessageReceiveArgs<APPLICATION, SESSION, Object>> onMessageReceive;

        public virtual ServerBuilder<APPLICATION, SESSION> OnMessageReceive(Action<EventMessageReceiveArgs<APPLICATION, SESSION, Object>> handler)
        {
            onMessageReceive = handler;
            return this;
        }

        private Action<ISession, SESSION> onConnected;

        private System.Collections.Concurrent.ConcurrentDictionary<Type, IMessageProcessHandler> mMessageHandlers = new System.Collections.Concurrent.ConcurrentDictionary<Type, IMessageProcessHandler>();

        public ServerBuilder<APPLICATION, SESSION> OnConnected(Action<ISession, SESSION> handler)
        {
            this.onConnected = handler;
            return this;
        }

        void IServerHandler.Connected(IServer server, ConnectedEventArgs e)
        {
            onConnected?.Invoke(e.Session, e.Session.Token<SESSION>());
        }

        private Action<IServer, ConnectingEventArgs> onConnecting;

        public ServerBuilder<APPLICATION, SESSION> OnConnecting(Action<IServer, ConnectingEventArgs> handler)
        {
            onConnecting = handler;
            return this;
        }

        void IServerHandler.Connecting(IServer server, ConnectingEventArgs e)
        {
            onConnecting?.Invoke(server, e);
        }

        private Action<ISession, SESSION> onDisconnect;

        public ServerBuilder<APPLICATION, SESSION> OnDisconnect(Action<ISession, SESSION> handler)
        {
            this.onDisconnect = handler;
            return this;
        }

        void IServerHandler.Disconnect(IServer server, SessionEventArgs e)
        {
            this.onDisconnect?.Invoke(e.Session, e.Session.Token<SESSION>());
        }


        private Action<IServer, ServerErrorEventArgs> onError;


        public ServerBuilder<APPLICATION, SESSION> OnError(Action<IServer, ServerErrorEventArgs> handler)
        {
            onError = handler;
            return this;
        }

        void IServerHandler.Error(IServer server, ServerErrorEventArgs e)
        {
            onError?.Invoke(server, e);
            if (server.EnableLog(LogType.Error))
            {
                if (e.Session == null)
                {
                    server.Log(LogType.Error, null, "server error {0}@{1}\r\n{2}", e.Message, e.Error.Message, e.Error.StackTrace);
                }
                else
                {
                    server.Log(LogType.Error, null, "session {2}@{3} error {0}@{1}\r\n{4}", e.Message, e.Error.Message, e.Session.RemoteEndPoint, e.Session.ID, e.Error.StackTrace);
                }
            }
        }

        private Action<IServer, ServerLogEventArgs> onLog;

        public ServerBuilder<APPLICATION, SESSION> OnLog(Action<IServer, ServerLogEventArgs> handler)
        {
            onLog = handler;
            return this;
        }

        protected virtual void OnWriteLog(IServer server, ServerLogEventArgs e)
        {

            if (e.Session == null)
            {
                ServerOptions.GetLogWriter().Add(null, e.Type, e.Message);
            }
            else
            {
                var endPoint = e.Session?.RemoteEndPoint?.ToString();

                var localPoint = e.Session?.Socket?.LocalEndPoint?.ToString();

                ServerOptions.GetLogWriter().Add($"{endPoint}/{localPoint}", e.Type, e.Message);
            }


        }

        void IServerHandler.Log(IServer server, ServerLogEventArgs e)
        {
            if (onLog != null)
            {
                onLog(server, e);
                return;
            }
            if (ServerOptions.WriteLog)
                OnWriteLog(server, e);


            if (ServerOptions.LogToConsole)
            {
                OnConsoleOutputLog(server, e);
            }
        }
        private object mLockConsole = new object();
        protected virtual void OnConsoleOutputLog(IServer server, ServerLogEventArgs e)
        {
            lock (mLockConsole)
            {
                Console.Write($"[{DateTime.Now.ToString("HH:mmm:ss")}] ");
                switch (e.Type)
                {
                    case LogType.Error:
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        break;
                    case LogType.Warring:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogType.Fatal:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogType.Info:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }
                Console.Write($"[{e.Type.ToString()}] ");
                Console.ForegroundColor = ConsoleColor.Gray;
                if (e.Session != null)
                    Console.Write($"[{e.Session.RemoteEndPoint}] ");
                else
                    Console.Write($"[SYSTEM] ");
                Console.WriteLine(e.Message);
            }
        }


        private Action<IServer> onOpened;

        public ServerBuilder<APPLICATION, SESSION> OnOpened(Action<IServer> handler)
        {
            onOpened = handler;
            return this;
        }

        void IServerHandler.Opened(IServer server)
        {
            onOpened?.Invoke(server);
            mRunSource.TrySetResult(ServerOptions.Listens);
        }

        void IServerHandler.SessionDetection(IServer server, SessionDetectionEventArgs e)
        {
            if (e.Sesions != null)
            {
                for (int i = 0; i < e.Sesions.Count; i++)
                {
                    ISession session = (ISession)e.Sesions[i];
                    session.Dispose();
                    if (server.EnableLog(LogType.Info))
                    {
                        server.Log(LogType.Info, session, "{0} disconnect session receive timeout!", session.RemoteEndPoint);
                    }
                }
            }
        }


        void IServerHandler.SessionPacketDecodeCompleted(IServer server, PacketDecodeCompletedEventArgs e)
        {
            OnSessionMessageReceive(e.Session, e.Message, e.Session.Token<SESSION>());
        }

        protected virtual void OnSessionMessageReceive(ISession session, object message, SESSION token)
        {
            Type msgtype = message.GetType();
            if (mMessageHandlers.TryGetValue(msgtype, out IMessageProcessHandler handler))
            {
                handler.Execute(session, session.Server.Tag, token, message);
            }
            else
            {
                if (onMessageReceive != null)
                {
                    EventMessageReceiveArgs<APPLICATION, SESSION, Object> e = new EventMessageReceiveArgs<APPLICATION, SESSION, Object>();
                    e.Application = (APPLICATION)session.Server.Tag;
                    e.Message = message;
                    e.NetSession = session;
                    e.Session = token;
                    onMessageReceive(e);
                }
            }
        }

        protected virtual IServer CreateServer()
        {

            return SocketFactory.CreateTcpServer(this, null, this.ServerOptions);
        }


        private Action<EventStreamReceiveArgs<APPLICATION, SESSION>> onStreamReceive;

        public ServerBuilder<APPLICATION, SESSION> OnStreamReceive(Action<EventStreamReceiveArgs<APPLICATION, SESSION>> handler)
        {
            onStreamReceive = handler;
            return this;
        }



        void IServerHandler.SessionReceive(IServer server, SessionReceiveEventArgs e)
        {
            if (onStreamReceive != null)
            {
                EventStreamReceiveArgs<APPLICATION, SESSION> args = new EventStreamReceiveArgs<APPLICATION, SESSION>();
                args.Session = e.Session.Token<SESSION>();
                args.NetSession = e.Session;
                args.Reader = e.Stream.ToPipeStream();
                args.Writer = e.Stream.ToPipeStream();
                args.Application = (APPLICATION)server.Tag;
                onStreamReceive(args);
            }
        }

        private System.Threading.Tasks.TaskCompletionSource<IList<ListenHandler>> mRunSource;

        public Task<IList<ListenHandler>> Run()
        {
            mRunSource = new TaskCompletionSource<IList<ListenHandler>>();
            mServer = CreateServer();
            mServer.Open();
            mServer.Tag = new APPLICATION();
            ((APPLICATION)mServer.Tag).Init(mServer);
            return mRunSource.Task;

        }

        private IServer mServer;

        public void Dispose()
        {
            mServer?.Dispose();
            mServer = null;
        }
    }
}
