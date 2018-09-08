using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
namespace BeetleX
{

    public interface IPacket : IDisposable
    {

        EventHandler<EventArgs.PacketDecodeCompletedEventArgs> Completed
        {
            get; set;
        }

        IPacket Clone();

        void Decode(ISession session, Buffers.IBinaryReader reader);

        void Encode(object data, ISession session, Buffers.IBinaryWriter writer);

        byte[] Encode(object data, IServer server);

        ArraySegment<byte> Encode(object data, IServer server, byte[] buffer);
    }

}
