using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServerX.Route
{
    internal class RoutingTables
    {
        /// <summary>
        /// Controller字典，Controller名称=参数类型
        /// </summary>
        public Dictionary<string, Type> ControllersDic = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
        /// <summary>
        /// Action名称字典 Action名称=methodInfo
        /// </summary>
        public Dictionary<string, MethodInfo> ActionsDic = new Dictionary<string, MethodInfo>(StringComparer.InvariantCultureIgnoreCase);
        /// <summary>
        /// Action参数字典 Action名称=参数类型
        /// </summary>
        public Dictionary<string, Type> ActionParamsTypeDic = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
        /// <summary>
        /// Action反回字典 Action名称=返回类型
        /// </summary>
        public Dictionary<string, Type> ActionReturnTypeDic = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
        public void Init()
        {
            LoadControlers();
        }

        private void LoadControlers()
        {
            var ass = Assembly.GetEntryAssembly();
            foreach (var t in ass.GetTypes())
            {
                if (t.IsSubclassOf(typeof(Controller)))
                {
                    var length = t.Name.ToLower().LastIndexOf("controller", StringComparison.Ordinal);
                    if (length == -1) continue;
                    string controllerPrefix = t.Name.ToLower().Substring(0, length);
                    ControllersDic.Add(controllerPrefix, t);
                    MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var m in methods)
                    {
                        ActionsDic.Add(controllerPrefix + "/" + m.Name.ToLower(), m);
                        ActionParamsTypeDic.Add(controllerPrefix + "/" + m.Name.ToLower(), m.GetParameters().Length > 0 ? m.GetParameters()[0].ParameterType : null);
                        ActionReturnTypeDic.Add(controllerPrefix + "/" + m.Name.ToLower(), m.ReturnType);
                    }
                }
            }
        }

        public RouteAction Route(string uri)
        {
            var actions = uri.ToLower().Split('/');
            var length = actions.Length;
            var action = new RouteAction
            {
                RequestUri = uri
            };
            if (!ControllersDic.TryGetValue(actions[0], out Type controlType)) return null;
            action.ControllerType = controlType;
            var keyname = actions[0].ToLower() + "/" + actions[1].ToLower();
            if (!ActionsDic.TryGetValue(keyname, out MethodInfo method)) return null;
            action.CurrentMethod = method;
            if (!ActionParamsTypeDic.TryGetValue(keyname, out Type intype)) return null;
            action.InArgumentType = intype;
            if (!ActionReturnTypeDic.TryGetValue(keyname, out Type outtype)) return null;
            action.OutArgumentType = outtype;
            return action;
        }
    }
}
