using BeetleX;
using BeetleX.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerX
{
    public class ServerX : ServerHandlerBase
    {
        public ServerX()
        {

        }
        protected override void OnReceiveMessage(IServer server, ISession session, object message)
        {
            var request = (RequestMessage)message;
            if (request==RequestMessage.HeartBeat) ProcessHeartbeating(session);
            else if (request==RequestMessage.PubKey) ProcessSSLPubKeyReq(session);
            else if (url == "/createkey") ProcessGetConnectSecret(session);
            else
            {
                if (ServerConfig.EnableSsl)
                {
                    var session = arg as AppSession;
                    if (string.IsNullOrEmpty(session.SecretKey))
                    {
                        Send("", 300, new byte[0], arg.SocketSession);
                        Logger.LogWarning($"Received Unnormal Request with ip {arg.SocketSession.Client.RemoteEndPoint.ToString()}");
                        return false;
                    }
                }
                var context = new Context(meta, arg, _autofac);
                context.Process();
            }
        }

        private void ProcessSSLPubKeyReq(ISession session)
        {
            throw new NotImplementedException();
        }

        private void ProcessHeartbeating(ISession session)
        {
            throw new NotImplementedException();
        }
    }
}
