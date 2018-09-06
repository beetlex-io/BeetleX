using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Buffers
{
    public interface IBinaryWriter
    {

        void Write(byte[] buffer, int offset, int count);

        Encoding Encoding { get; set; }

        bool LittleEndian { get; set; }

        System.IO.Stream Stream { get; }

        void Write(bool value);

        void Write(short value);

        void Write(int value);

        void Write(long value);

        void Write(ushort value);

        void Write(uint value);

        void Write(ulong value);

        void Write(DateTime value);

        void Write(char value);

        void Write(float value);

        void Write(double value);

        int Write(string value);

        int WriteLine(string value);

        int WriteLine(string value, params object[] parameters);

        int Write(string value, params object[] parameters);

        void WriteUTF(string value);

        void WriteShortUTF(string value);

        void Flush();

        int CacheLength { get; }



        MemoryBlockCollection Allocate(int size);

        long Length
        {
            get;
        }
    }
}
