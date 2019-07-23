using System;

namespace ServerX.Route
{
    public class Controller : IDisposable
    {
        public Request Request { get; internal set; }
        public Response Response { get; internal set; }
        public Context Context { get; internal set; }

        public void Dispose()
        {
            if (Request != null) Request.Dispose();
            Request = null;
            Response = null;
            Context = null;
        }

        public virtual bool OnActionExecuting(RouteAction action)
        {
            return true;
        }
    }
}
