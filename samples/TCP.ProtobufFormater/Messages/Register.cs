using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Messages
{
    [ProtoContract]
    [BeetleX.Packets.MessageType(1)]
    public class Register
    {
        [ProtoMember(1)]
        public string EMail { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public string PassWord { get; set; }
        [ProtoMember(4)]
        public string City { get; set; }
        [ProtoMember(5)]
        public DateTime DateTime { get; set; }
    }

}
