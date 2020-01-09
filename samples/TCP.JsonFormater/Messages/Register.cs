using System;
using System.Collections.Generic;
using System.Text;

namespace Messages
{
    
    [BeetleX.Packets.MessageType(1)]
    public class Register
    {
      
        public string EMail { get; set; }
      
        public string Name { get; set; }
       
        public string PassWord { get; set; }
      
        public string City { get; set; }
       
        public DateTime DateTime { get; set; }
    }

}
