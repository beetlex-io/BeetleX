using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX
{

    public interface IWriteHandler
    {
        Action<IWriteHandler> Completed { get; set; }

        void Write(System.IO.Stream stream);
    }

    public class BytesHandler : IWriteHandler
    {
        public BytesHandler(byte[] data)
        {
            mData = data;
            mCount = mData.Length;
        }

        public BytesHandler(byte[] data, int offset, int count)
        {
            mData = data;
            mOffset = offset;
            mCount = count;
        }

        public BytesHandler(ArraySegment<byte> data)
        {
            mData = data.Array;
            mOffset = data.Offset;
            mCount = data.Count;
        }

        public BytesHandler(string data, Encoding encoding)
        {
            mData = encoding.GetBytes(data);
            mOffset = 0;
            mCount = mData.Length;
        }


        private int mOffset = 0;

        private int mCount = 0;

        private byte[] mData = null;

        public Byte[] Data => mData;

        public int Offset => mOffset;

        public int Count => mCount;

        public Action<IWriteHandler> Completed { get; set; }

        public void Write(System.IO.Stream stream)
        {
            stream.Write(mData, mOffset, mCount);
        }

        public static implicit operator ArraySegment<byte>(BytesHandler d) => new ArraySegment<byte>(d.mData, d.mOffset, d.mCount);

        public static implicit operator BytesHandler(byte[] data) => new BytesHandler(data);

        public static implicit operator BytesHandler(ArraySegment<byte> data) => new BytesHandler(data);

        public static implicit operator BytesHandler(string data) => new BytesHandler(data, Encoding.UTF8);

        public void To(ISession session)
        {
            session.Send(this);
        }

        public void To(Clients.TcpClient client)
        {
            var pipestream = client.Stream.ToPipeStream();
            Write(pipestream);
            client.Stream.Flush();
        }

        public void To(Clients.AsyncTcpClient client)
        {
            client.Send(this);
        }

        public void To(IServer server, params ISession[] sessions)
        {
            server.Send(this, sessions);
        }
    }
}
