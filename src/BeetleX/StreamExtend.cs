using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BeetleX
{


    public static class StreamHelper
    {

        public static PipeStream ToPipeStream(this Stream stream)
        {
            PipeStream result = stream as PipeStream;
            if (result != null)
            {
                return result;
            }
            else
            {
                SslStreamX sslStramX = stream as SslStreamX;
                if (sslStramX != null)
                    return sslStramX.GetPipeStream();
                throw new BXException("invalid cast to PipeStream!");
            }
        }

        #region write

        public static void Write(System.IO.Stream stream, byte value)
        {
            stream.WriteByte(value);
        }
        public static void Write(System.IO.Stream stream, bool value)
        {
            if (value)
            {
                stream.WriteByte((byte)1);
            }
            else
            {
                stream.WriteByte((byte)0);
            }
        }

        static int mCachedLength = 512;

        [ThreadStatic]
        static byte[] mBytesCached;
        static void CreateByteCached()
        {
            if (mBytesCached == null)
                mBytesCached = new byte[mCachedLength];
        }

        [ThreadStatic]
        static char[] mCharsCached;
        static void CreateCharCached()
        {
            if (mCharsCached == null)
                mCharsCached = new char[mCachedLength];
        }

        public unsafe static void Write(Stream stream, short value, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                pipeStream.Write(value);
            }
            else
            {
                CreateByteCached();
                if (!littleEndian)
                    value = BitHelper.SwapInt16(value);
                BitHelper.Write(mBytesCached, value);
                stream.Write(mBytesCached, 0, 2);
            }
        }

        public unsafe static void Write(Stream stream, int value, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                pipeStream.Write(value);
            }
            else
            {
                CreateByteCached();
                if (!littleEndian)
                    value = BitHelper.SwapInt32(value);
                BitHelper.Write(mBytesCached, value);
                stream.Write(mBytesCached, 0, 4);
            }
        }

        public unsafe static void Write(Stream stream, long value, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                pipeStream.Write(value);
            }
            else
            {
                CreateByteCached();
                if (!littleEndian)
                    value = BitHelper.SwapInt64(value);
                BitHelper.Write(mBytesCached, value);
                stream.Write(mBytesCached, 0, 8);
            }
        }

        public unsafe static void Write(Stream stream, ushort value, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                pipeStream.Write(value);
            }
            else
            {
                CreateByteCached();
                if (!littleEndian)
                    value = BitHelper.SwapUInt16(value);
                BitHelper.Write(mBytesCached, value);
                stream.Write(mBytesCached, 0, 2);
            }
        }

        public unsafe static void Write(Stream stream, uint value, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                pipeStream.Write(value);
            }
            else
            {
                CreateByteCached();
                if (!littleEndian)
                    value = BitHelper.SwapUInt32(value);
                BitHelper.Write(mBytesCached, value);
                stream.Write(mBytesCached, 0, 4);
            }
        }

        public unsafe static void Write(Stream stream, ulong value, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                pipeStream.Write(value);
            }
            else
            {
                CreateByteCached();
                if (!littleEndian)
                    value = BitHelper.SwapUInt64(value);
                BitHelper.Write(mBytesCached, value);
                stream.Write(mBytesCached, 0, 8);
            }
        }

        public static void Write(Stream stream, DateTime value, bool littleEndian)
        {
            Write(stream, value.Ticks, littleEndian);
        }

        public unsafe static void Write(Stream stream, char value, bool littleEndian)
        {
            short num = (short)value;
            Write(stream, num, littleEndian);
        }

        public unsafe static void Write(Stream stream, float value, bool littleEndian)
        {
            int num = *(int*)(&value);
            Write(stream, num, littleEndian);
        }

        public unsafe static void Write(Stream stream, double value, bool littleEndian)
        {
            long num = *(long*)(&value);
            Write(stream, num, littleEndian);
        }

        public static int Write(Stream stream, Encoding encoding, string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                return pipeStream.Write(value);
            }
            else
            {
                CreateByteCached();
                CreateCharCached();
                int count = 0;
                int offset = 0;
                int rlength = 0;
                int length;
                int maxSubLength = mCachedLength / encoding.GetMaxByteCount(1);
                if (value.Length <= maxSubLength)
                {
                    length = encoding.GetBytes(value, 0, value.Length, mBytesCached, 0);
                    stream.Write(mBytesCached, 0, length);
                    return length;
                }

                while (true)
                {
                    rlength = value.Length - offset;
                    if (rlength > maxSubLength)
                        rlength = maxSubLength;
                    length = encoding.GetBytes(value, offset, rlength, mBytesCached, 0);
                    stream.Write(mBytesCached, 0, length);
                    count += length;
                    offset += rlength;
                    if (offset >= value.Length)
                        break;
                }
                return count;
            }

        }

        public static int Write(Stream stream, Encoding encoding, string value, params object[] parameters)
        {
            return Write(stream, encoding, string.Format(value, parameters));
        }

        public static unsafe void WriteShortUTF(Stream stream, Encoding encoding, string value, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                pipeStream.WriteShortUTF(value);
            }
            else
            {
                CreateByteCached();
                int maxSubLength = mCachedLength / encoding.GetMaxByteCount(1);
                if (maxSubLength > value.Length)
                {
                    short count = (short)encoding.GetBytes(value, 0, value.Length, mBytesCached, 0);
                    if (!littleEndian)
                        count = BitHelper.SwapInt16(count);
                    byte[] lendata = BitConverter.GetBytes(count);
                    stream.Write(lendata, 0, 2);
                    stream.Write(mBytesCached, 0, count);

                }
                else
                {
                    PipeStream pipetream = new PipeStream(BufferPoolGroup.DefaultGroup.Next(), littleEndian, encoding);
                    pipetream.WriteShortUTF(value);
                    Copy(pipeStream, stream);
                    pipetream.Dispose();
                }
            }
        }

        public static unsafe void WriteUTF(Stream stream, Encoding encoding, string value, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                pipeStream.WriteUTF(value);
            }
            else
            {
                CreateByteCached();
                int maxSubLength = mCachedLength / encoding.GetMaxByteCount(1);
                if (maxSubLength > value.Length)
                {
                    int count = (int)encoding.GetBytes(value, 0, value.Length, mBytesCached, 0);
                    if (!littleEndian)
                        count = BitHelper.SwapInt32(count);
                    byte[] lendata = BitConverter.GetBytes(count);
                    stream.Write(lendata, 0, 4);
                    stream.Write(mBytesCached, 0, count);
                }
                else
                {
                    PipeStream pipetream = new PipeStream(BufferPoolGroup.DefaultGroup.Next(), littleEndian, encoding);
                    pipetream.WriteUTF(value);
                    Copy(pipeStream, stream);
                    pipetream.Dispose();
                }
            }
        }
        #endregion


        public static void WriteBuffer(System.IO.Stream stream, IBuffer buffer)
        {
            IBuffer wbuffer = buffer;
            while (wbuffer != null)
            {
                stream.Write(wbuffer.Data, 0, wbuffer.Length);
                wbuffer = wbuffer.Next;
            }
            Buffers.Buffer.Free(buffer);
        }

        public static void Copy(PipeStream source, Stream dest)
        {
            IBuffer buff = source.GetWriteCacheBufers();
            WriteBuffer(dest, buff);
        }

        public static void ToPipeStream(Stream source, PipeStream dest)
        {
            ToPipeStream(source, (int)source.Length, dest);
        }

        public static void ToPipeStream(Stream source, int length, PipeStream dest)
        {
            int rlen = 0;
            while (true)
            {
                IBuffer buffer = BufferPoolGroup.DefaultGroup.Next().Pop();
                int offset = 0;
                int size = buffer.Size;
                int bufferlen = 0;
            NEXT:
                if (length > size)
                {
                    rlen = source.Read(buffer.Data, offset, size);
                }
                else
                {
                    rlen = source.Read(buffer.Data, offset, length);
                }
                if (rlen == 0)
                {
                    if (bufferlen > 0)
                    {
                        buffer.SetLength(bufferlen);
                        dest.Import(buffer);
                    }
                    else
                    {
                        buffer.Free();
                    }
                    return;
                }
                bufferlen += rlen;
                offset += rlen;
                size -= rlen;
                length -= rlen;
                if (length == 0)
                {
                    buffer.SetLength(bufferlen);
                    dest.Import(buffer);
                    return;
                }
                else
                {
                    if (size > 0)
                        goto NEXT;
                    else
                    {
                        buffer.SetLength(bufferlen);
                        dest.Import(buffer);
                    }
                }
            }
        }


        #region read
        public static bool TryRead(Stream stream, int count)
        {
            return stream.Length >= count;
        }

        public static byte ReadByte(Stream stream)
        {
            return (byte)stream.ReadByte();
        }

        public static bool ReadBool(Stream stream)
        {
            int data = stream.ReadByte();
            return data == 0 ? false : true;
        }

        public static unsafe short ReadInt16(Stream stream, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                return pipeStream.ReadInt16();
            }
            else
            {
                short result;
                CreateByteCached();
                stream.Read(mBytesCached, 0, 2);
                result = BitConverter.ToInt16(mBytesCached, 0);
                if (!littleEndian)
                    result = BitHelper.SwapInt16(result);
                return result;
            }
        }

        public static unsafe int ReadInt32(Stream stream, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                return pipeStream.ReadInt32();
            }
            else
            {
                int result;
                CreateByteCached();
                stream.Read(mBytesCached, 0, 4);
                result = BitConverter.ToInt32(mBytesCached, 0);
                if (!littleEndian)
                    result = BitHelper.SwapInt32(result);
                return result;
            }
        }

        public static unsafe long ReadInt64(Stream stream, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                return pipeStream.ReadInt64();
            }
            else
            {
                long result;
                CreateByteCached();
                stream.Read(mBytesCached, 0, 8);
                result = BitConverter.ToInt64(mBytesCached, 0);
                if (!littleEndian)
                    result = BitHelper.SwapInt64(result);
                return result;
            }
        }

        public static unsafe ushort ReadUInt16(Stream stream, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                return pipeStream.ReadUInt16();
            }
            else
            {
                ushort result;
                CreateByteCached();
                stream.Read(mBytesCached, 0, 2);
                result = BitConverter.ToUInt16(mBytesCached, 0);
                if (!littleEndian)
                    result = BitHelper.SwapUInt16(result);
                return result;
            }
        }

        public static unsafe uint ReadUInt32(Stream stream, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                return pipeStream.ReadUInt32();
            }
            else
            {
                uint result;
                CreateByteCached();
                stream.Read(mBytesCached, 0, 4);
                result = BitConverter.ToUInt32(mBytesCached, 0);
                if (!littleEndian)
                    result = BitHelper.SwapUInt32(result);
                return result;
            }
        }

        public static unsafe ulong ReadUInt64(Stream stream, bool littleEndian)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                return pipeStream.ReadUInt64();
            }
            else
            {
                ulong result;
                CreateByteCached();
                stream.Read(mBytesCached, 0, 8);
                result = BitConverter.ToUInt64(mBytesCached, 0);
                if (!littleEndian)
                    result = BitHelper.SwapUInt64(result);
                return result;
            }
        }

        public static char ReadChar(Stream stream, bool littleEndian)
        {
            return (char)ReadInt16(stream, littleEndian);
        }

        public static DateTime ReadDateTime(Stream stream, bool littleEndian)
        {
            return new DateTime(ReadInt64(stream, littleEndian));
        }

        public static unsafe float ReadFloat(Stream stream, bool littleEndian)
        {
            int num;
            num = ReadInt32(stream, littleEndian);
            return *(float*)(&num);
        }

        public static unsafe double ReadDouble(Stream stream, bool littleEndian)
        {
            long num;
            num = ReadInt64(stream, littleEndian);
            return *(double*)(&num);
        }

        public static string ReadString(Stream stream, Decoder decoder, int length)
        {
            PipeStream pipeStream = stream as PipeStream;
            if (pipeStream != null)
            {
                return pipeStream.ReadString(length);
            }
            else
            {
                if (length <= 0)
                    return null;
                int rlength = 0;
                int rcharlength = 0;
                int count = 0;
                CreateByteCached();
                CreateCharCached();
                if (length <= mBytesCached.Length)
                {
                    count = stream.Read(mBytesCached, 0, length);
                    count = decoder.GetChars(mBytesCached, 0, count, mCharsCached, 0);
                    return new string(mCharsCached, 0, count);
                }
                StringBuilder sb = new StringBuilder(length);
                count = 0;
                while (true)
                {
                    rlength = length - count;
                    if (rlength > mBytesCached.Length)
                        rlength = mBytesCached.Length;
                    stream.Read(mBytesCached, 0, rlength);
                    rcharlength = decoder.GetChars(mBytesCached, 0, rlength, mCharsCached, 0);
                    sb.Append(mCharsCached, 0, rcharlength);
                    count += rlength;
                    if (count >= length)
                        break;
                }
                return sb.ToString();
            }
        }

        public static string ReadUTF(Stream stream, Decoder decoder, bool littleEndian)
        {
            int count = ReadInt32(stream, littleEndian);
            return ReadString(stream, decoder, count);
        }

        public static string ReadShortUTF(Stream stream, Decoder decoder, bool littleEndian)
        {
            int count = ReadInt16(stream, littleEndian);
            return ReadString(stream, decoder, count);
        }
        #endregion
    }
}
