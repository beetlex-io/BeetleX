using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Buffers
{
    public interface IBinaryReader
    {
        bool LittleEndian
        { get; set; }

        int Read(byte[] buffer, int offset, int count);

        System.IO.Stream Stream { get; }

        Encoding Encoding { get; set; }

        int ReadByte();

        bool TryRead(int count);

        bool ReadBool();

        short ReadInt16();

        int ReadInt32();

        long ReadInt64();

        ushort ReadUInt16();

        uint ReadUInt32();

        ulong ReadUInt64();

        char ReadChar();

        DateTime ReadDateTime();

        float ReadFloat();

        double ReadDouble();

        string ReadString(int length);

        string ReadUTF();

        string ReadShortUTF();

        string ReadLine();

        bool TryReadLine(out string value, bool returnEof);

        bool TryReadWith(string eof, out string value, bool returnEof);

        bool TryReadWith(byte[] eof, out string value, bool returnEof);

        string ReadToEnd();

        long Length { get; }

    }
}
