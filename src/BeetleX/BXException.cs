using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX
{
    public class BXException : Exception
    {
        public BXException()
        {
        }
        public BXException(string message) : base(message) { }

        public BXException(string message, params object[] parameters) : base(string.Format(message, parameters)) { }

        public BXException(string message, Exception baseError) : base(message, baseError) { }

        public BXException(Exception baseError, string message, params object[] parameters) : base(string.Format(message, parameters), baseError) { }
    }
}
