using BeetleX.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Clients
{
    public interface IClientLogHander
    {
        LogType LogType { get; set; }

        void Write(LogType LogType, IClient client, string message);
    }
}
