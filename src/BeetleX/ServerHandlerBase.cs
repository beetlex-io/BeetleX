﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BeetleX.EventArgs;

namespace BeetleX
{
    public class ServerHandlerBase : IServerHandler
    {
        public virtual void Connected(IServer server, ConnectedEventArgs e)
        {
            if (server.Options.SessionTimeOut > 0)
                server.UpdateSession(e.Session);
            if (server.EnableLog(LogType.Info))
                server.Log(LogType.Info, null, "session connected from {0}@{1}", e.Session.RemoteEndPoint, e.Session.ID);
        }

        public virtual void Connecting(IServer server, ConnectingEventArgs e)
        {
            if (server.EnableLog(LogType.Info))
                server.Log(LogType.Info, null, "connect from {0}", e.Socket.RemoteEndPoint);
        }

        public virtual void Disconnect(IServer server, SessionEventArgs e)
        {
            if (server.EnableLog(LogType.Info))
                server.Log(LogType.Info, null, "session {0}@{1} disconnected", e.Session.RemoteEndPoint, e.Session.ID);
        }

        public virtual void Error(IServer server, ServerErrorEventArgs e)
        {
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


        protected virtual void WriteLog(IServer server, ServerLogEventArgs e)
        {

            if (e.Session == null)
            {
                server.GetLogWriter().Add(null, e.Type, e.Message);
            }
            else
            {
                var endPoint = e.Session?.RemoteEndPoint?.ToString();

                var localPoint = e.Session?.Socket?.LocalEndPoint?.ToString();

                server.GetLogWriter().Add($"{endPoint}/{localPoint}", e.Type, e.Message);
            }


        }

        public virtual void Log(IServer server, ServerLogEventArgs e)
        {
            OnLogToConsole(server, e);
        }

        private object mLockConsole = new object();

        public IServer Server { get; set; }

        protected virtual void OnLogToConsole(IServer server, ServerLogEventArgs e)
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

        public virtual void SessionDetection(IServer server, SessionDetectionEventArgs e)
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

        protected virtual void OnReceiveMessage(IServer server, ISession session, object message)
        {

        }

        public virtual void SessionPacketDecodeCompleted(IServer server, PacketDecodeCompletedEventArgs e)
        {
            OnReceiveMessage(server, e.Session, e.Message);
        }

        public virtual void SessionReceive(IServer server, SessionReceiveEventArgs e)
        {

        }

        public virtual void Opened(IServer server)
        {

        }
    }
}
