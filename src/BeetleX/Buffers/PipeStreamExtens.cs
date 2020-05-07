using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Buffers
{

    public struct ByteBlock : IDisposable
    {
        public static ByteBlock Create(int count)
        {
            ByteBlock result = new ByteBlock();
            result.Data = System.Buffers.ArrayPool<byte>.Shared.Rent(count);
            result.Offset = 0;
            result.Count = count;
            return result;
        }

        public byte[] Data { get; set; }

        public int Offset { get; set; }

        public int Count { get; set; }

        public Span<byte> GetSpan()
        {
            return new Span<byte>(Data, Offset, Count);
        }

        public void Dispose()
        {
            if (Data != null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(Data);
                Data = null;
            }
        }
    }

    public struct CharBlock : IDisposable
    {
        public static CharBlock Create(int count)
        {
            CharBlock result = new CharBlock();
            result.Data = System.Buffers.ArrayPool<char>.Shared.Rent(count);
            result.Offset = 0;
            result.Count = count;
            return result;
        }

        public char[] Data { get; set; }

        public int Offset { get; set; }

        public int Count { get; set; }

        public Span<char> GetSpan()
        {
            return new Span<char>(Data, Offset, Count);
        }

        public void Dispose()
        {
            if (Data != null)
            {
                System.Buffers.ArrayPool<char>.Shared.Return(Data);
                Data = null;
            }
        }
    }

    public partial class PipeStream
    {
        public int ReadTo(StringBuilder sb, int count, Encoding encoding = null)
        {
            using (var block = ReadChars(count, encoding))
            {
                sb.Append(block.Data, block.Offset, block.Count);
                block.Dispose();
                return block.Count;
            }
        }

        public bool TryReadLine(out CharBlock value, Encoding encoding, bool returnEof = false)
        {

            if (encoding == null)
                encoding = Encoding;
            return TryReadWith(encoding.GetBytes("\r\n"), out value, encoding, returnEof);
        }
        public bool TryReadWith(byte[] eof, out CharBlock value, Encoding encoding, bool returnEof = false)
        {
            IndexOfResult result = IndexOf(eof);
            int length = result.Length;
            if (result.End != null)
            {
                if (returnEof)
                {
                    value = ReadChars(length, encoding);
                }
                else
                {
                    value = ReadChars(length - eof.Length, encoding);
                    ReadFree(eof.Length);
                }
                return true;
            }
            value = default;
            return false;
        }
        public ByteBlock ReadBytes(int length)
        {
            ByteBlock result = ByteBlock.Create(length);
            var len = Read(result.Data, 0, length);
            result.Count = len;
            return result;
        }
        public CharBlock ReadChars(int length, Encoding encoding = null)
        {
            CharBlock block = CharBlock.Create(length);
            var len = ReadChars(block.Data, length, encoding);
            block.Count = len;
            return block;
        }
        public int ReadChars(char[] data, int length, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding;
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(length);
            try
            {
                Read(buffer, 0, length);
                var len = encoding.GetChars(buffer, 0, length, data, 0);
                return len;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        public void Write(ArraySegment<char> data, Encoding encoding = null)
        {
            Write(data.Array, data.Offset, data.Count, encoding);
        }
        public void Write(char[] data, int offset, int cout, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding;
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(cout * 6);
            try
            {
                var len = encoding.GetBytes(data, offset, cout, buffer, 0);
                Write(buffer, 0, len);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        public void Write(StringBuilder data, Encoding encoding = null)
        {
            var chars = System.Buffers.ArrayPool<char>.Shared.Rent(data.Length);
            try
            {

                data.CopyTo(0, chars, 0, data.Length);
                Write(chars, 0, data.Length, encoding);
            }
            finally
            {
                System.Buffers.ArrayPool<char>.Shared.Return(chars);
            }
        }

    }



}
namespace BeetleX
{
    public static partial class SpanCharExtensions
    {

        public static ReadOnlySpan<char> SubLeftWith(this ReadOnlySpan<char> span, char[] chars, out ReadOnlySpan<char> item)
        {
            item = default;
            int index = span.IndexOf(chars);
            if (index > 0)
            {
                item = span.Slice(0, index);
                return span.Slice(index + chars.Length);
            }
            return span;
        }

        public static ReadOnlySpan<char> SubRightWith(this ReadOnlySpan<char> span, char[] chars, out ReadOnlySpan<char> item)
        {
            item = default;
            int index = span.LastIndexOf(chars);
            if (index > 0)
            {
                item = span.Slice(index + chars.Length);
                return span.Slice(0, index);

            }
            return span;
        }

        public static ReadOnlySpan<char> SubLeftWith(this ReadOnlySpan<char> span, char spitChar, out ReadOnlySpan<char> item)
        {
            item = default;
            int index = span.IndexOf(spitChar);
            if (index > 0)
            {
                item = span.Slice(0, index);
                return span.Slice(index + 1);
            }
            return span;
        }

        public static ReadOnlySpan<char> SubRightWith(this ReadOnlySpan<char> span, char spitChar, out ReadOnlySpan<char> item)
        {
            item = default;
            int index = span.LastIndexOf(spitChar);
            if (index > 0)
            {
                item = span.Slice(index + 1);
                return span.Slice(0, index);
            }
            return span;
        }


        public static ReadOnlySpan<char> SubLeftWith(this Span<char> span, char[] chars, out ReadOnlySpan<char> item)
        {
            item = default;
            int index = span.IndexOf(chars);
            if (index > 0)
            {
                item = span.Slice(0, index);
                return span.Slice(index + chars.Length);
            }
            return span;
        }

        public static ReadOnlySpan<char> SubRightWith(this Span<char> span, char[] chars, out ReadOnlySpan<char> item)
        {
            item = default;
            int index = span.LastIndexOf(chars);
            if (index > 0)
            {
                item = span.Slice(index + chars.Length);
                return span.Slice(0, index);

            }
            return span;
        }

        public static ReadOnlySpan<char> SubLeftWith(this Span<char> span, char spitChar, out ReadOnlySpan<char> item)
        {
            item = default;
            int index = span.IndexOf(spitChar);
            if (index > 0)
            {
                item = span.Slice(0, index);
                return span.Slice(index + 1);
            }
            return span;
        }

        public static ReadOnlySpan<char> SubRightWith(this Span<char> span, char spitChar, out ReadOnlySpan<char> item)
        {
            item = default;
            int index = span.LastIndexOf(spitChar);
            if (index > 0)
            {
                item = span.Slice(index + 1);
                return span.Slice(0, index);
            }
            return span;
        }



        public static string SubLeftWith(this string span, char[] chars, out string item)
        {
            item = default;
            int index = span.IndexOfAny(chars);
            if (index > 0)
            {
                item = span.Substring(0, index);
                return span.Substring(index + chars.Length);
            }
            return span;
        }

        public static string SubRightWith(this string span, char[] chars, out string item)
        {
            item = default;
            int index = span.LastIndexOfAny(chars);
            if (index > 0)
            {
                item = span.Substring(index + chars.Length);
                return span.Substring(0, index);

            }
            return span;
        }

        public static string SubLeftWith(this string span, char spitChar, out string item)
        {
            item = default;
            int index = span.IndexOf(spitChar);
            if (index > 0)
            {
                item = span.Substring(0, index);
                return span.Substring(index + 1);
            }
            return span;
        }

        public static string SubRightWith(this string span, char spitChar, out string item)
        {
            item = default;
            int index = span.LastIndexOf(spitChar);
            if (index > 0)
            {
                item = span.Substring(index + 1);
                return span.Substring(0, index);
            }
            return span;
        }
    }
}
