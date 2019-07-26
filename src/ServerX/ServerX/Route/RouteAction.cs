using System;
using System.Reflection;

namespace ServerX.Route
{
    public class RouteAction
    {
        public RouteAction(Controller controller = null)
        {
            Controller = controller;
        }
        public string RequestUri { get; set; }
        public Type ControllerType { get; set; }
        public Controller Controller { get; }
        public MethodInfo CurrentMethod { get; set; }
        public Type InArgumentType { get; set; }
        public Type OutArgumentType { get; set; }
    }
}
