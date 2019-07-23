using System;
using System.IO;

namespace ServerX.Route
{
    public class Request : IDisposable
    {
        public string Uri { get; internal set; }
        public Version Version { get; internal set; }
        public StreamReader Stream { get; internal set; }

        public void Dispose()
        {
            if (Stream != null) Stream.Close();
        }
    }
}
