using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.EventArgs
{
    public class PacketDecodeCompletedEventArgs : SessionEventArgs
    {
        public PacketDecodeCompletedEventArgs()
        {


        }

        public PacketDecodeCompletedEventArgs SetInfo(ISession session, object message)
        {
            Server = session.Server;
            Session = session;
            Message = message;
            return this;
        }
        public object Message
        {
            get;
            internal set;
        }


    }
}
