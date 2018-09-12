using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.HttpExtend
{
    public class HttpResponse
    {
        internal HttpResponse(IBodySerializer formater)
        {
            Header = new Header();
            Header[Header.SERVER] = "BeetleX-HttpExtend";
            Header[Header.CONTENT_TYPE] = formater.ContentType;
            Serializer = formater;
        }

        private string mCode = "200";

        private string mCodeMsg = "OK";

        public Header Header { get; set; }

        public IBodySerializer Serializer { get; set; }


        public void Error500(Exception e)
        {
            mCode = "500";
            mCodeMsg = "Internal Server Error";
        }

        public void Error404()
        {
            mCode = "404";
            mCodeMsg = "Not found";
        }


        public object Body { get; set; }

        public string HttpVersion { get; set; }

        public void SetStatus(string code, string msg)
        {
            mCode = code;
            mCodeMsg = msg;
        }

        internal void Write(PipeStream stream)
        {
            string stateLine = string.Concat(HttpVersion, " ", mCode, " ", mCodeMsg);
            stream.WriteLine(stateLine);
            Header.Write(stream);
            MemoryBlockCollection contentLength = stream.Allocate(28);
            stream.WriteLine("");
            int count = Serializer.Serialize(stream, Body);
            contentLength.Full("Content-Length: " + count.ToString().PadRight(10) + "\r\n", stream.Encoding);

        }

    }
}
