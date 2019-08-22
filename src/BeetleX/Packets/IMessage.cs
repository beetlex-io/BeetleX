using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.Packets
{
    public interface IMessage
    {
        void Load(IBinaryReader reader);

        void Save(IBinaryWriter writer);
    } 
}
