using System;
using System.Reflection;

namespace ServerX.Route
{
    public class RouteAction
    {
        public string RequestUri { get; set; }
        public Type ControllerType { get; set; }
        public MethodInfo CurrentMethod { get; set; }
        public Type InArgumentType { get; set; }
        public Type OutArgumentType { get; set; }

        public bool IsHeartBeat()
        {
            return string.Compare(RequestUri, "beat") == 0;
        }
        public bool IsPubKeyReq()
        {
            return string.Compare(RequestUri, "pubkey") == 0;
        }
        public bool IsCreateKeyReq()
        {
            return string.Compare(RequestUri, "key") == 0;
        }
    }
}
