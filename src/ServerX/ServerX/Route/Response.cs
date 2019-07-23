using System;
using ML.Net.TcpServer.Abstract;
using ML.Net.TcpServer.Protocol;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ServerX.Route
{
    public class Response
    {
        private AppSession session;
        private AppServer server;
        private List<ArraySegment<byte>> _sendseg;
        public Response(AppSession session)
        {
            this.session = session;
            server = session.AppServer as AppServer;
            _sendseg = new List<ArraySegment<byte>>();
        }
        public void Flush()
        {
            if (_sendseg.Count == 0) return;
            session.Logger.LogTrace($"Send  to {session.SocketSession.RemoteEndPoint.ToString()}");
            session.SocketSession.TrySend(_sendseg);
            _sendseg.Clear();
        }
        public void Write<T>(T info)
        {
            var bytes = MLProtocolAnalysis.CreateResponseBytes(server.Serializer, session.SecretKey, 200, info, server.ServerConfig.EnableSsl);
            _sendseg.Add(new ArraySegment<byte>(bytes));
            UpdateSendCount(typeof(T));
        }
        internal void Write<T>(T info, short statuscode) where T : class
        {
            var bytes = MLProtocolAnalysis.CreateResponseBytes(server.Serializer, session.SecretKey, statuscode, info, false);
            _sendseg.Add(new ArraySegment<byte>(bytes));
            UpdateSendCount(typeof(T));
        }
        internal void Write(Type type, object info)
        {
            var bytes = MLProtocolAnalysis.CreateResponseBytes(server.Serializer, session.SecretKey, 200, type, info, server.ServerConfig.EnableSsl);
            _sendseg.Add(new ArraySegment<byte>(bytes));
            UpdateSendCount(info.GetType());
        }
        void UpdateSendCount(Type info)
        {
            var count = System.Threading.Interlocked.Increment(ref session.SendCount);
            session.Logger.LogTrace($"Add1 the {count.ToString()} package {info.FullName}  to {session.SocketSession.RemoteEndPoint.ToString()} ");
        }
    }
}
