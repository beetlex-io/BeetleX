using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX
{
    public interface ISessionToken : IDisposable
    {
        void Init(ISession session);
    }
}
