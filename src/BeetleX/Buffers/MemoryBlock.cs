using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Buffers
{

    public struct MemoryBlockCollection
    {

        public MemoryBlockCollection(IList<Memory<byte>> blocks)
        {

            Blocks = blocks;
        }

        public IList<Memory<byte>> Blocks { get; set; }

        public void Full(byte[] value)
        {
            if (value.Length <= 8)
            {
                int index = 0;
                for (int i = 0; i < Blocks.Count; i++)
                {
                    Span<byte> span = Blocks[i].Span;
                    for (int j = 0; j < span.Length; j++)
                    {
                        span[j] = value[index];
                        index++;
                    }
                }
            }
            else
            {
                int count = value.Length;
                int offset = 0;
                Span<byte> data = new Span<byte>(value);
                for (int i = 0; i < Blocks.Count; i++)
                {
                    Span<byte> span = Blocks[i].Span;
                    data.Slice(offset, span.Length).CopyTo(span);
                    offset += span.Length;
                }
            }
        }

        public void Full(string value, Encoding encoding)
        {
            Full(encoding.GetBytes(value));
        }

        public void Full(long value)
        {

            Full(BitConverter.GetBytes(value));
        }

        public void Full(short value)
        {

            Full(BitConverter.GetBytes(value));
        }

        public void Full(int value)
        {

            Full(BitConverter.GetBytes(value));
        }

        public void Full(ulong value)
        {

            Full(BitConverter.GetBytes(value));
        }

        public void Full(ushort value)
        {

            Full(BitConverter.GetBytes(value));
        }

        public void Full(uint value)
        {

            Full(BitConverter.GetBytes(value));
        }
    }
}
