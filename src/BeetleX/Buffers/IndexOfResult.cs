using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Buffers
{
    public struct IndexOfResult
    {

        public IReadMemory Start;

        public int StartPostion;

        public IReadMemory End;

        public int EndPostion;

        public int Length;
    }
}
