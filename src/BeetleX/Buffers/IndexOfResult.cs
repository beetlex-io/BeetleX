using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Buffers
{
    public struct IndexOfResult
    {

        public IMemoryBlock Start;

        public int StartPostion;

        public IMemoryBlock End;

        public int EndPostion;

        public int Length;

        public byte[] EofData;

        public int EofIndex;
    }
}
