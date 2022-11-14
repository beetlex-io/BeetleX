using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX
{
    public interface ISessionToken : IDisposable
    {
        void Init(IServer server, ISession session);
    }
}
