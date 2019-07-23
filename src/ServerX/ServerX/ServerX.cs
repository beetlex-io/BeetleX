using BeetleX;
using BeetleX.EventArgs;
using ServerX.Route;
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
            var context = new Context(server, session, request);
            context.Process();
        }
    }
}
}
