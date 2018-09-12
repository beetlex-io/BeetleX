using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.HttpExtend
{
    public interface IBodySerializer
    {
        int Serialize(PipeStream stream, object data);

        bool TryDeserialize(PipeStream stream, int length, out object data);

        int Write404(PipeStream stream, HttpRequest request);

        int Write500(PipeStream stream, Exception e, HttpRequest request);

        string ContentType { get; set; }
    }

    public class StringSerializer : IBodySerializer
    {

        public StringSerializer()
        {
            ContentType = "text/html; charset=utf-8";
        }

        public string ContentType { get; set; }

        public bool TryDeserialize(PipeStream stream, int length, out object data)
        {
            data = null;
            if (stream.Length >= length)
            {
                data = stream.ReadString(length);
                return true;
            }
            return false;
        }

        public int Serialize(PipeStream stream, object data)
        {
            int length = stream.CacheLength;
            stream.Write((string)data);
            return stream.CacheLength - length;
        }

        public int Write404(PipeStream stream, HttpRequest request)
        {
            int length = stream.CacheLength;
            stream.Write(request.Url + " not found ");
            return stream.CacheLength - length;
        }

        public int Write500(PipeStream stream, Exception e, HttpRequest request)
        {
            int length = stream.CacheLength;
            stream.Write(request.Url + " Internal Server Error\r\n");
            stream.WriteLine(e.Message);
            return stream.CacheLength - length;
        }
    }
}
