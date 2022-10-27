using BeetleX.Buffers;
using BeetleX.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX
{

    public class ServerBuilder<Packet, Token> : ServerBuilder<Token>
    where Token : new()
        where Packet : IPacket, new()
    {
        protected override IServer CreateServer()
        {
            return SocketFactory.CreateTcpServer(this, new Packet(), this.ServerOptions);
        }

        private System.Collections.Concurrent.ConcurrentDictionary<Type, Delegate> mMessageHandlers = new System.Collections.Concurrent.ConcurrentDictionary<Type, Delegate>();


        public new ServerBuilder<Packet, Token> SetOptions(Action<ServerOptions> handler)
        {
            handler(this.ServerOptions);
            return this;
        }

        public ServerBuilder<Packet, Token> OnMessageReceive<Message>(Action<ISession, Message, Token> handler)
        {
            mMessageHandlers[typeof(Message)] = handler;
            return this;
        }

        private Action<ISession, object, Token> onMessageReceive;

        public ServerBuilder<Packet, Token> OnMessageReceive(Action<ISession, object, Token> handler)
        {
            onMessageReceive = handler;
            return this;
        }
        protected override void OnSessionMessageReceive(ISession session, object message, Token token)
        {
            Type msgtype = message.GetType();
            if (mMessageHandlers.TryGetValue(msgtype, out Delegate handler))
            {
                handler.DynamicInvoke(session, message, token);
            }
            else
            {
                if (onMessageReceive != null)
                {
                    onMessageReceive(session, message, token);
                }
            }
        }
    }

    public class ServerBuilder<Token> : IServerHandler
        where Token : new()
    {
        IServer IServerHandler.Server { get; set; }

        public bool ConsoleOutputLog { get; set; } = true;

        public ServerOptions ServerOptions { get; private set; } = new ServerOptions();

        public ServerBuilder<Token> SetOptions(Action<ServerOptions> handler)
        {
            handler(this.ServerOptions);
            return this;
        }


        private Action<ISession, Token> onConnected;

        public ServerBuilder<Token> OnConnected(Action<ISession, Token> handler)
        {
            this.onConnected = handler;
            return this;
        }

        void IServerHandler.Connected(IServer server, ConnectedEventArgs e)
        {
            e.Session.Tag = new Token();
            onConnected?.Invoke(e.Session, (Token)e.Session.Tag);
        }

        private Action<IServer, ConnectingEventArgs> onConnecting;

        public ServerBuilder<Token> OnConnecting(Action<IServer, ConnectingEventArgs> handler)
        {
            onConnecting = handler;
            return this;
        }

        void IServerHandler.Connecting(IServer server, ConnectingEventArgs e)
        {
            onConnecting?.Invoke(server, e);
        }

        private Action<ISession, Token> onDisconnect;

        public ServerBuilder<Token> OnDisconnect(Action<ISession, Token> handler)
        {
            this.onDisconnect = handler;
            return this;
        }

        void IServerHandler.Disconnect(IServer server, SessionEventArgs e)
        {
            this.onDisconnect?.Invoke(e.Session, (Token)e.Session.Tag);
        }


        private Action<IServer, ServerErrorEventArgs> onError;


        public ServerBuilder<Token> OnError(Action<IServer, ServerErrorEventArgs> handler)
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

        public ServerBuilder<Token> OnLog(Action<IServer, ServerLogEventArgs> handler)
        {
            onLog = handler;
            return this;
        }

        void IServerHandler.Log(IServer server, ServerLogEventArgs e)
        {
            onLog?.Invoke(server, e);

            if (ConsoleOutputLog)
            {
                OnConsoleOutputLog(server, e);
            }
        }
        private object mLockConsole = new object();
        protected virtual void OnConsoleOutputLog(IServer server, ServerLogEventArgs e)
        {
            lock (mLockConsole)
            {
                Console.Write($"[{ DateTime.Now.ToString("HH:mmm:ss")}] ");
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
                Console.WriteLine(e.Message);
            }
        }


        private Action<IServer> onOpened;

        public ServerBuilder<Token> OnOpened(Action<IServer> handler)
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
                        server.Log(LogType.Info, session, "{0} session time out", session.RemoteEndPoint);
                    }
                }
            }
        }


        void IServerHandler.SessionPacketDecodeCompleted(IServer server, PacketDecodeCompletedEventArgs e)
        {
            OnSessionMessageReceive(e.Session, e.Message, (Token)e.Session.Tag);
        }

        protected virtual void OnSessionMessageReceive(ISession session, object message, Token token)
        {

        }

        protected virtual IServer CreateServer()
        {

            return SocketFactory.CreateTcpServer(this, null, this.ServerOptions);
        }


        private Action<ISession, PipeStream, Token> onStreamReceive;

        public ServerBuilder<Token> OnStreamReceive(Action<ISession, PipeStream, Token> handler)
        {
            onStreamReceive = handler;
            return this;
        }



        void IServerHandler.SessionReceive(IServer server, SessionReceiveEventArgs e)
        {
            if (onStreamReceive != null)
            {
                Token t = (Token)e.Session.Tag;
                var stream = e.Stream.ToPipeStream();
                onStreamReceive(e.Session, stream, t);
            }
        }

        private System.Threading.Tasks.TaskCompletionSource<IList<ListenHandler>> mRunSource;

        public Task<IList<ListenHandler>> Run()
        {
            mRunSource = new TaskCompletionSource<IList<ListenHandler>>();
            var server = CreateServer();
            server.Open();
            return mRunSource.Task;

        }
    }
}
