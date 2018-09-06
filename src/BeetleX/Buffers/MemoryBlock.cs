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
