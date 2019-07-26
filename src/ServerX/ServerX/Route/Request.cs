using System;
using BeetleX;
using Autofac;

namespace ServerX.Route
{
    /// <summary>
    /// 请求上下文
    /// </summary>
    public class Request
    {
        ILifetimeScope _autofac;
        Response _response;
        /// <summary>
        /// 实例化一个请求上下文
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="session"></param>
        internal Request(IServer server, ISession session, ILifetimeScope lifetime)
        {
            _autofac = lifetime;
            _response = new Response(server, session);
        }
        internal void Process(RequestMessage meta)
        {
            var action = meta.RouteInfo;
            if (action == null)
            {
                _response.Error("invaild message", 40);
                return;
            }
            if (action.Controller != null)
            {
                InvokeController(action.Controller, meta);
                return;
            }
            ILifetimeScope scope = _autofac.BeginLifetimeScope();
            try
            {
                var controller = (Controller)scope.ResolveOptional(action.ControllerType);
                InvokeController(controller, meta);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null) _response.Error(ex.InnerException.Message, 50);
                else _response.Error(ex.Message, 50);
            }
            finally
            {
                scope.Dispose();
            }
        }
        void InvokeController(Controller controller, RequestMessage meta)
        {
            var route = meta.RouteInfo;
            controller.Request = this;
            controller.Response = _response;
            if (!controller.OnActionExecuting(route)) return;
            if (route.OutArgumentType != null)
            {
                var obj = route.CurrentMethod.Invoke(controller, new object[] { meta.Token });
                controller.Response.OK(obj);
            }
            else route.CurrentMethod.Invoke(controller, new object[] { meta.Token });
        }
    }
}
