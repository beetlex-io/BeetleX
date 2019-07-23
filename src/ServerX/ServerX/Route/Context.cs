using System;
using System.IO;
using System.Diagnostics;
using BeetleX;

namespace ServerX.Route
{
    /// <summary>
    /// 请求上下文
    /// </summary>
    public sealed class Context
    {
        static RoutingTables _tables;
        static object _instanceLock = new object();
        RequestMessage _reqmeta;
        /// <summary>
        /// 实例化一个请求上下文
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="session"></param>
        internal Context(IServer server, ISession session, RequestMessage req)
        {
            _reqmeta = req;
            Server = server;
            Session = session;
            if (_tables == null)
            {
                lock (_instanceLock)
                {
                    if (_tables == null)
                    {
                        _tables = new RoutingTables();
                        _tables.Init();
                    }
                }
            }
        }
        public IServer Server { get; }
        public ISession Session { get; }
        internal void Process()
        {
            var action = _tables.Route(_reqmeta.Uri);
            if (action == null)
            {
                var response = new Response(Session);
                response.Write("无效地址", 404);
                response.Flush();
                return;
            }
            Controller controller = null;
            ILifetimeScope scope = null;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                scope = _autofac.BeginLifetimeScope();
                controller = scope.ResolveOptional(action.ControllerType) as Controller;

                controller.Context = this;

                controller.Request = new Request { Uri = _reqmeta.Uri, Version = _reqmeta.Version, Stream = CreateStreamReader(_reqmeta.Data) };
                controller.Response = new Response(Session);
                if (!controller.OnActionExecuting(action)) return;
                if (action.OutArgumentType.IsGenericType && action.OutArgumentType.GetGenericTypeDefinition() == typeof(ActionResult<>))
                {
                    var returnData = GetMethodReturn(controller, action);
                    var d = action.OutArgumentType.GetMethod("Result").Invoke(returnData, null);
                    controller.Response.Write(d.GetType(), d);
                }
                else
                {
                    GetMethodReturn(controller, action);
                }
            }
            catch (Exception ex)
            {
                Server.Logger.LogError(ex.ToString());
                if (!Server.RaiseUnCatchException(ex, action))
                {
                    if (ex.InnerException != null)
                    {
                        controller.Response.Write("服务器内部异常" + Environment.NewLine + ex.InnerException.Message, 500);
                    }
                    else
                        controller.Response.Write("服务器内部异常" + Environment.NewLine + ex.Message, 500);
                }
            }
            controller.Response.Flush();
            controller.Dispose();
            if (scope != null)
            {
                scope.Dispose();
            }
            stopwatch.Stop();
            Server.Logger.LogInformation($"client: {Session.SocketSession.RemoteEndPoint.ToString()} request uri:{_reqmeta.Uri} usetime: {stopwatch.ElapsedMilliseconds.ToString()} ms");
        }
        private object GetMethodReturn(Controller controller, RouteAction action)
        {
            if (action.InArgumentType != null)
            {
                var argu = Server.Serializer.Deserialize(action.InArgumentType, controller.Request.Stream);
                return action.CurrentMethod.Invoke(controller, new object[] { argu });
            }
            else
            {
                return action.CurrentMethod.Invoke(controller, null);
            }
        }
        private StreamReader CreateStreamReader(BufferBytesReadInfo data)
        {
            MemoryStream stream;
            if (Server.ServerConfig.EnableSsl) stream = new MemoryStream(DesHelper.DeEncrypt(data, Session.SecretKey)); //解密
            else stream = new MemoryStream(data.ToBytes(), data.OffSet, data.Count);
            var reader = new StreamReader(stream, Server.Charset);
            return reader;
        }
    }
}
