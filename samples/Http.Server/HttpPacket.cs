using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeetleX.Buffers;
using BeetleX.EventArgs;

namespace BeetleX.HttpExtend
{
    public class HttpPacket : IPacket
    {

        public HttpPacket(IBodySerializer bodySerializer)
        {
            Serializer = bodySerializer;
        }

        public EventHandler<PacketDecodeCompletedEventArgs> Completed { get; set; }

        public IPacket Clone()
        {
            return new HttpPacket(this.Serializer);
        }

        private PacketDecodeCompletedEventArgs mCompletedArgs = new PacketDecodeCompletedEventArgs();

        private HttpRequest mRequest;

        public IBodySerializer Serializer { get; set; }

        public void Decode(ISession session, Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            START:
            if (mRequest == null)
            {
                mRequest = new HttpRequest(session, this.Serializer);
            }
            if (mRequest.Read(pstream) == LoadedState.Completed)
            {
                Completed?.Invoke(this, mCompletedArgs.SetInfo(session, mRequest));
                mRequest = null;
                if (pstream.Length == 0)
                    return;
                goto START;
            }
            else
            {
                return;
            }


        }

        public void Dispose()
        {

        }

        public void Encode(object data, ISession session, Stream stream)
        {
            OnEncode(session, data, stream);
        }

        private void OnEncode(ISession session, object data, System.IO.Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            HttpResponse response = (HttpResponse)data;
            response.Write(pstream);

        }

        public byte[] Encode(object data, IServer server)
        {
            byte[] result = null;
            using (Buffers.PipeStream stream = new PipeStream(server.BufferPool, server.Config.LittleEndian, server.Config.Encoding))
            {
                OnEncode(null, data, stream);
                stream.Position = 0;
                result = new byte[stream.Length];
                stream.Read(result, 0, result.Length);
            }
            return result;
        }

        public ArraySegment<byte> Encode(object data, IServer server, byte[] buffer)
        {
            using (Buffers.PipeStream stream = new PipeStream(server.BufferPool, server.Config.LittleEndian, server.Config.Encoding))
            {
                OnEncode(null, data, stream);
                stream.Position = 0;
                int count = (int)stream.Length;
                stream.Read(buffer, 0, count);
                return new ArraySegment<byte>(buffer, 0, count);
            }
        }
    }
}
