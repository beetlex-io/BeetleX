using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.HttpExtend
{
    public enum LoadedState
    {
        None,
        Method,
        Header,
        Completed
    }
    public class HttpRequest
    {
        public HttpRequest(ISession session, IBodySerializer formater)
        {
            Header = new Header();
            this.Session = session;
            mState = LoadedState.None;
            Serializer = formater;
        }

        private LoadedState mState;

        private int mLength;

        public bool KeepAlive { get; set; }

        public Header Header { get; private set; }

        public string Method { get; set; }

        public string Url { get; set; }

        public string HttpVersion { get; set; }

        public ISession Session { get; private set; }

        public IBodySerializer Serializer { get; set; }

        public LoadedState Read(PipeStream stream)
        {
            LoadMethod(stream);
            LoadHeaer(stream);
            LoadBody(stream);
            return mState;
        }

        private void LoadMethod(PipeStream stream)
        {
            string line;
            if (mState == LoadedState.None)
            {
                if (stream.TryReadLine(out line))
                {
                    string[] values = line.Split(' ');
                    Method = values[0];
                    Url = values[1];
                    HttpVersion = values[2];
                    mState = LoadedState.Method;
                }
            }
        }

        private void LoadHeaer(PipeStream stream)
        {
            if (mState == LoadedState.Method)
            {
                if (this.Header.Read(stream))
                {
                    mState = LoadedState.Header;
                    int.TryParse(Header[Header.CONTENT_LENGTH], out mLength);
                    KeepAlive = Header[Header.CONNECTION] == "keep-alive";
                }
            }
        }

        private void LoadBody(PipeStream stream)
        {
            if (mState == LoadedState.Header)
            {
                if (mLength == 0)
                {
                    mState = LoadedState.Completed;
                }
                else
                {
                    object data;
                    if (Serializer.TryDeserialize(stream, mLength, out data))
                    {
                        mState = LoadedState.Completed;
                    }

                }
            }
        }

        public object Body { get; set; }

        public HttpResponse CreateResponse()
        {
            HttpResponse response = new HttpResponse(this.Serializer);
            
            response.HttpVersion = this.HttpVersion;
            response.Serializer = this.Serializer;
            
            if (this.KeepAlive)
                response.Header[Header.CONNECTION] = "keep-alive";
            response.Header[Header.HOST] = Header[Header.HOST];
            return response;
        }
    }
}
