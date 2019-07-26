using BeetleX.Buffers;
using BeetleX.Packets;
using ServerX.Route;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServerX
{
    public class TypeHandler : IMessageTypeHeader
    {
        public Dictionary<string, RouteAction> Mapper = new Dictionary<string, RouteAction>(StringComparer.InvariantCultureIgnoreCase);

        public Type ReadType(PipeStream stream)
        {
            throw new NotImplementedException();
        }
        private void LoadControlers(Assembly ass)
        {
            foreach (var t in ass.GetTypes())
            {
                if (t.IsSubclassOf(typeof(Controller)))
                {
                    var length = t.Name.LastIndexOf("controller", StringComparison.OrdinalIgnoreCase);
                    if (length == -1) continue;
                    string controllerPrefix = t.Name.Substring(0, length);
                    MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var m in methods)
                    {
                        var key = string.Concat(controllerPrefix, "/", m.Name);
                        var routeInfo = new RouteAction() { ControllerType = t.GetType(), CurrentMethod = m, RequestUri = key, OutArgumentType = m.ReturnType };
                        if (m.GetParameters().Length != 0) routeInfo.InArgumentType = m.GetParameters()[0].ParameterType;
                        Mapper.Add(key, routeInfo);
                    }
                }
            }
        }
        public void Register(params Assembly[] assemblies)
        {
            foreach (var ass in assemblies)
            {
                LoadControlers(ass);
            }
        }
        public void Add(Controller ctl, MethodInfo method, string url, bool set_instance = false)
        {
            MethodInfo[] methods = ctl.GetType().DeclaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var f = false;
            foreach (var m in methods)
            {
                if (m != method) continue;
                f = true;
                RouteAction routeInfo;
                if (set_instance) routeInfo = new RouteAction(ctl);
                else routeInfo = new RouteAction() { ControllerType = ctl.GetType().DeclaringType, CurrentMethod = method, RequestUri = url, OutArgumentType = method.ReturnType };
                if (m.GetParameters().Length != 0) routeInfo.InArgumentType = m.GetParameters()[0].ParameterType;
                Mapper.Add(url, routeInfo);
                break;
            }
            if (!f) throw new Exception("the controller doesnt contain the method");
        }
        public void WriteType(object data, PipeStream stream)
        {
            var typename = data.GetType().FullName;
            var l = (byte)typename.Length;
            stream.WriteByte(l);
            stream.Write(typename);
        }
        internal RouteAction GetRouteInfo(string url)
        {
            var f = Mapper.TryGetValue(url, out RouteAction action);
            if (f) return action;
            else return null;
        }
    }
}
