using System;

namespace ServerX.Route
{
    public class Controller
    {
        public Request Request { get; internal set; }
        public Response Response { get; internal set; }

        public virtual bool OnActionExecuting(RouteAction action)
        {
            return true;
        }
    }
}
