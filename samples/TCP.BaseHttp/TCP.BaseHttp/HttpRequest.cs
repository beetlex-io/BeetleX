using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeetleX;

namespace TCP.BaseHttp
{
    class HttpRequest
    {

        public string HttpVersion { get; set; }

        public string Method { get; set; }

        public string BaseUrl { get; set; }

        public string ClientIP { get; set; }

        public string Path { get; set; }

        public string QueryString { get; set; }

        public string Url { get; set; }

        public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

        public byte[] Body { get; set; }

        public int ContentLength { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.None;

    }

    class HttpResponse : IWriteHandler
    {

        public HttpResponse()
        {
            Headers["Content-Type"] = "text/html";
        }

        public string HttpVersion { get; set; } = "HTTP/1.1";

        public int Status { get; set; }

        public string StatusMessage { get; set; } = "OK";

        public string Body { get; set; }

        public Dictionary<string, string> Headers = new Dictionary<string, string>();


        public void Write(Stream stream)
        {
            var pipeStream = stream.ToPipeStream();
            pipeStream.WriteLine($"{HttpVersion} {Status} {StatusMessage}");
            foreach (var item in Headers)
                pipeStream.WriteLine($"{item.Key}: {item.Value}");
            byte[] bodyData = null;
            if (!string.IsNullOrEmpty(Body))
            {
                bodyData = Encoding.UTF8.GetBytes(Body);
            }
            if (bodyData != null)
            {
                pipeStream.WriteLine($"Content-Length: {bodyData.Length}");
            }

            pipeStream.WriteLine("");
            if (bodyData != null)
            {
                pipeStream.Write(bodyData, 0, bodyData.Length);
            }
            Completed?.Invoke(this);
        }

        public Action<IWriteHandler> Completed { get; set; }
    }


    public enum RequestStatus
    {
        None,
        LoadingHeader,
        LoadingBody,
        Completed
    }

}
