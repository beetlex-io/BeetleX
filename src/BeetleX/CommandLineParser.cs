using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BeetleX
{
    public class CommandLineParser
    {
        private Dictionary<string, string> mProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<Type, ObjectOptionsBuilder> mBuilders = new Dictionary<Type, ObjectOptionsBuilder>();

        private CommandLineParser()
        {

        }

        private ObjectOptionsBuilder GetBuilder(Type type)
        {
            if (mBuilders.TryGetValue(type, out ObjectOptionsBuilder result))
            {
                return result;
            }
            result = new ObjectOptionsBuilder(type);
            result.Init();
            mBuilders[type] = result;
            return result;
        }

        public T GetOption<T>() where T : new()
        {
            var builder = GetBuilder(typeof(T));
            return (T)builder.Builder(mProperties);

        }

        public static CommandLineParser GetCommandLineParser(string[] args, int start = 1)
        {
            CommandLineParser result = new CommandLineParser();
            for (int i = start; i < args.Length; i = i + 2)
            {
                result.mProperties[args[i]] = args[i + 1];
            }
            var evn = System.Environment.GetEnvironmentVariables();
            foreach (var key in evn.Keys)
            {
                result.mProperties["env_" + key] = evn[key].ToString();
            }
            return result;
        }

        public static CommandLineParser GetCommandLineParser()
        {
            return GetCommandLineParser(System.Environment.GetCommandLineArgs());
        }


        public string Help<T>()
        {

            var builder = GetBuilder(typeof(T));

            return builder.ToString();
        }

    }

    public class ParserException : Exception
    {
        public ParserException(string error) : base(error)
        {

        }
    }

    class ObjectOptionsBuilder
    {
        public ObjectOptionsBuilder(Type type)
        {
            mObjectType = type;
        }

        private Type mObjectType { get; set; }

        public List<OptionAttribute> Options { get; set; } = new List<OptionAttribute>();

        public void Init()
        {
            foreach (var item in mObjectType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var option = item.GetCustomAttribute<OptionAttribute>(false);
                if (option != null)
                {
                    option.Property = item;
                    Options.Add(option);
                    if (string.IsNullOrEmpty(option.ShortName))
                    {
                        option.ShortName = item.Name;
                        option.LongName = item.Name;
                    }
                    if (string.IsNullOrEmpty(option.LongName))
                    {
                        option.LongName = option.ShortName;
                    }
                }
            }
            if (Options.Count == 0)
            {
                throw new ParserException($"{mObjectType.Name} object does not have a configuration option!");
            }
        }

        public object Builder(Dictionary<string, string> args)
        {
            object result = Activator.CreateInstance(mObjectType);
            foreach (var p in Options)
            {
                p.SetValue(args, result);
            }

            return result;
        }

        public override string ToString()
        {
            string result = "";
            foreach (var p in Options)
            {
                result += p + "\r\n";
            }
            return result;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class OptionAttribute : Attribute
    {
        public OptionAttribute()
        {

        }
        public OptionAttribute(string shortName)
        {
            this.ShortName = shortName;
        }

        public string Describe { get; set; }

        public string ShortName { get; set; }

        public string LongName { get; set; }

        public bool Required { get; set; } = true;

        internal PropertyInfo Property { get; set; }

        internal void SetValue(Dictionary<string, string> args, object source)
        {
            string value=null;
            string envshort = "env_" + ShortName;
            string envlong = "env_" + LongName;

            if (args.ContainsKey(envshort))
            {
                value = args[envshort];
            }

            if (args.ContainsKey(envlong))
            {
                value = args[envlong];
            }
            if (args.ContainsKey(ShortName))
            {
                value = args[ShortName];
            }
            if (args.ContainsKey(LongName))
            {
                value = args[LongName];
            }

            if (!Required && string.IsNullOrEmpty(value))
                return;
            if (Required && string.IsNullOrEmpty(value))
            {
                throw new ParserException($"{ShortName} Parameter required!");
            }

            object data = null;
            try
            {
                data = Convert.ChangeType(value, Property.PropertyType);
            }
            catch (Exception e_)
            {
                throw new ParserException($"{ShortName} convert data error  {e_.Message}!");
            }
            Property.SetValue(source, data);
        }

        public override string ToString()
        {
            if (Required)
                return $"<{ShortName}|{LongName}>\t{Describe}";
            else
                return $"[{ShortName}|{LongName}]\t{Describe}";
        }
    }

}
