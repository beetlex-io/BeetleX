using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.Clients
{


    public delegate void EventClientPacketCompleted(IClient client, object message);


    public interface IClientPacket : IDisposable
    {

        EventClientPacketCompleted Completed
        {
            get;
            set;
        }

        IClientPacket Clone();

        void Decode(IClient client, Buffers.IBinaryReader reader);

        void Encode(object data, IClient client, Buffers.IBinaryWriter writer);
    }


}
