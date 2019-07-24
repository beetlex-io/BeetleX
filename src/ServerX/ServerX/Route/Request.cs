using System;
using System.IO;
using System.Diagnostics;
using BeetleX;
using Autofac;
using BeetleX.EventArgs;

namespace ServerX.Route
{
    /// <summary>
    /// 请求上下文
    /// </summary>
    public class Request
    {
        static object _instanceLock = new object();
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
            if (meta.RouteInfo.IsHeartBeat()) _response.WriteHeartBeat();
            else
            {
                Controller controller = null;
                ILifetimeScope scope = _autofac.BeginLifetimeScope();
                try
                {
                    controller = scope.ResolveOptional(action.ControllerType) as Controller;
                    controller.Request = this;
                    controller.Response = _response;
                    if (!controller.OnActionExecuting(action)) return;
                    if (action.OutArgumentType != null)
                    {
                        var obj = action.CurrentMethod.Invoke(controller, new object[] { meta.Token });
                        controller.Response.OK(obj);
                    }
                    else action.CurrentMethod.Invoke(controller, new object[] { meta.Token });
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null) _response.Error(ex.InnerException.Message, 50);
                    else _response.Error(ex.Message, 50);
                }
                scope.Dispose();
            }
        }
    }
}
