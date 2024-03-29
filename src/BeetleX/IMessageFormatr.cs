﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
namespace BeetleX
{

    public interface IPacket : IDisposable
    {

        string Name { get; }
        EventHandler<EventArgs.PacketDecodeCompletedEventArgs> Completed
        {
            get; set;
        }

        IPacket Clone();

        void Decode(ISession session, System.IO.Stream stream);

        void Encode(object data, ISession session, System.IO.Stream stream);

        byte[] Encode(object data, IServer server);

        ArraySegment<byte> Encode(object data, IServer server, byte[] buffer);
    }

    public interface IMessageSubmitHandler
    {
        void Execute(object sender, object message);
    }

}
