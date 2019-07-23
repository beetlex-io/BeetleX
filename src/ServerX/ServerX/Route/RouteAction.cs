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
    }
}
