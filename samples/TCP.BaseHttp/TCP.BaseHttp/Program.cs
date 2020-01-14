using BeetleX;
using BeetleX.Buffers;
using BeetleX.EventArgs;
using System;
using System.Text;
namespace TCP.BaseHttp
{
    class Program : ServerHandlerBase
    {
        private static IServer server;
        public static void Main(string[] args)
        {
            server = SocketFactory.CreateTcpServer<Program>();
            //server.Options.DefaultListen.Port =9090;
            //server.Options.DefaultListen.Host = "127.0.0.1";
            server.Open();
            System.Threading.Thread.Sleep(-1);
        }
        public override void SessionReceive(IServer server, SessionReceiveEventArgs e)
        {
            var request = GetRequest(e.Session);
            var pipeStream = e.Stream.ToPipeStream();
            if (LoadRequest(request, pipeStream) == RequestStatus.Completed)
            {
                OnCompleted(request, e.Session);
            }
        }

        private RequestStatus LoadRequest(HttpRequest request, PipeStream stream)
        {
            LoadRequestLine(request, stream);
            LoadRequestHeader(request, stream);
            LoadRequestBody(request, stream);
            return request.Status;
        }

        private void LoadRequestLine(HttpRequest request, PipeStream stream)
        {
            if (request.Status == RequestStatus.None)
            {
                if (stream.TryReadLine(out string line))
                {
                    var subItem = line.SubLeftWith(' ', out string value);
                    request.Method = value;
                    subItem = subItem.SubLeftWith(' ', out value);
                    request.Url = value;
                    request.HttpVersion = subItem;

                    subItem = request.Url.SubRightWith('?', out value);
                    request.QueryString = value;
                    request.BaseUrl = subItem;
                    request.Path = subItem.SubRightWith('/', out value);
                    if (request.Path != "/")
                        request.Path += "/";
                    request.Status = RequestStatus.LoadingHeader;
                }
            }
        }

        private void LoadRequestHeader(HttpRequest request, PipeStream stream)
        {
            if (request.Status == RequestStatus.LoadingHeader)
            {
                while (stream.TryReadLine(out string line))
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        if (request.ContentLength == 0)
                        {
                            request.Status = RequestStatus.Completed;
                        }
                        else
                        {
                            request.Status = RequestStatus.LoadingBody;
                        }
                        return;
                    }
                    var name = line.SubRightWith(':', out string value);
                    if (String.Compare(name, "Content-Length", true) == 0)
                    {
                        request.ContentLength = int.Parse(value);
                    }
                    request.Headers[name] = value.Trim();
                }
            }
        }
        private void LoadRequestBody(HttpRequest request, PipeStream stream)
        {
            if (request.Status == RequestStatus.LoadingBody)
            {
                if (stream.Length >= request.ContentLength)
                {
                    var data = new byte[request.ContentLength]; ;
                    stream.Read(data, 0, data.Length);
                    request.Body = data;
                    request.Status = RequestStatus.Completed;
                }
            }
        }

        private void OnCompleted(HttpRequest request, ISession session)
        {
            HttpResponse response = new HttpResponse();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html>");
            sb.AppendLine("<body>");
            sb.AppendLine($"<p>Method:{request.Method}</p>");
            sb.AppendLine($"<p>Url:{request.Url}</p>");
            sb.AppendLine($"<p>Path:{request.Path}</p>");
            sb.AppendLine($"<p>QueryString:{request.QueryString}</p>");
            sb.AppendLine($"<p>ClientIP:{request.ClientIP}</p>");
            sb.AppendLine($"<p>Content-Length:{request.ContentLength}</p>");
            foreach (var item in request.Headers)
            {
                sb.AppendLine($"<p>{item.Key}:{item.Value}</p>");
            }
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            response.Body = sb.ToString();
            ClearRequest(session);
            session.Send(response);
        }

        private HttpRequest GetRequest(ISession session)
        {
            HttpRequest request = (HttpRequest)session["__request"];
            if (request == null)
            {
                request = new HttpRequest();
                request.ClientIP = session.RemoteEndPoint.ToString();
                session["__request"] = request;
            }
            return request;
        }

        private void ClearRequest(ISession session)
        {
            session["__request"] = null;
        }
    }
}
