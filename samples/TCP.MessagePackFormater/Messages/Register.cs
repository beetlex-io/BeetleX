using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Messages
{
    [MessagePackObject]
    [BeetleX.Packets.MessageType(1)]
    public class Register
    {
        [Key(1)]
        public string EMail { get; set; }
        [Key(2)]
        public string Name { get; set; }
        [Key(3)]
        public string PassWord { get; set; }
        [Key(4)]
        public string City { get; set; }
        [Key(5)]
        public DateTime DateTime { get; set; }
    }

}
