using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Buffers
{
    public class Int7bit
    {

        [ThreadStatic]
        private static Byte[] mInt7BitDataBuffer;

        public void Write(System.IO.Stream stream, int value)
        {

            if (mInt7BitDataBuffer == null)
                mInt7BitDataBuffer = new byte[32];
            var count = 0;
            var num = (UInt64)value;
            while (num >= 0x80)
            {
                mInt7BitDataBuffer[count++] = (Byte)(num | 0x80);
                num >>= 7;
            }
            mInt7BitDataBuffer[count++] = (Byte)num;
            stream.Write(mInt7BitDataBuffer, 0, count);
        }

        private uint mInt7BitResult = 0;

        private byte mInt7Bits = 0;

        public int? Read(System.IO.Stream stream)
        {

            Byte b;
            while (true)
            {
                if (stream.Length < 1)
                    return null;
                var bt = stream.ReadByte();
                if (bt < 0)
                {
                    mInt7Bits = 0;
                    mInt7BitResult = 0;
                    throw new BXException("Read 7bit int error:byte value cannot be less than zero!");
                }
                b = (Byte)bt;

                mInt7BitResult |= (UInt32)((b & 0x7f) << mInt7Bits);
                if ((b & 0x80) == 0) break;
                mInt7Bits += 7;
                if (mInt7Bits >= 32)
                {
                    mInt7Bits = 0;
                    mInt7BitResult = 0;
                    throw new BXException("Read 7bit int error:out of maximum value!");
                }
            }
            mInt7Bits = 0;
            var result = mInt7BitResult;
            mInt7BitResult = 0;
            return (Int32)result;
        }
    }
    public class BitHelper
    {


        public const int BIT_1 = 0b0000_0000_0000_0001;

        public const int BIT_2 = 0b0000_0000_0000_0010;

        public const int BIT_3 = 0b0000_0000_0000_0100;

        public const int BIT_4 = 0b0000_0000_0000_1000;

        public const int BIT_5 = 0b0000_0000_0001_0000;

        public const int BIT_6 = 0b0000_0000_0010_0000;

        public const int BIT_7 = 0b0000_0000_0100_0000;

        public const int BIT_8 = 0b0000_0000_1000_0000;

        public const int BIT_9 = 0b0000_0001_0000_0000;

        public const int BIT_10 = 0b0000_0010_0000_0000;

        public const int BIT_11 = 0b0000_0100_0000_0000;

        public const int BIT_12 = 0b0000_1000_0000_0000;

        public const int BIT_13 = 0b0001_0000_0000_0000;

        public const int BIT_14 = 0b0010_0000_0000_0000;

        public const int BIT_15 = 0b0100_0000_0000_0000;

        public const int BIT_16 = 0b1000_0000_0000_0000;

        public static short SwapInt16(short v)
        {
            return (short)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
        }

        public static ushort SwapUInt16(ushort v)
        {
            return (ushort)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
        }

        public static int SwapInt32(int v)
        {
            return (int)(((SwapInt16((short)v) & 0xffff) << 0x10) |
                          (SwapInt16((short)(v >> 0x10)) & 0xffff));
        }

        public static uint SwapUInt32(uint v)
        {
            return (uint)(((SwapUInt16((ushort)v) & 0xffff) << 0x10) |
                           (SwapUInt16((ushort)(v >> 0x10)) & 0xffff));
        }

        public static long SwapInt64(long v)
        {
            return (long)(((SwapInt32((int)v) & 0xffffffffL) << 0x20) |
                           (SwapInt32((int)(v >> 0x20)) & 0xffffffffL));
        }

        public static ulong SwapUInt64(ulong v)
        {
            return (ulong)(((SwapUInt32((uint)v) & 0xffffffffL) << 0x20) |
                            (SwapUInt32((uint)(v >> 0x20)) & 0xffffffffL));
        }


        public static void Write(Span<byte> _buffer, short value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
        }

        public static void Write(Span<byte> _buffer, ushort value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);

        }


        public static void Write(Span<byte> _buffer, int value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);

        }

        public static void Write(Span<byte> _buffer, uint value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);

        }

        public static void Write(Span<byte> _buffer, long value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);

        }

        public static void Write(Span<byte> _buffer, ulong value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);

        }




        //------------------------------------------------------------
        public static void Write(byte[] _buffer, int postion, short value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
        }

        public static void Write(byte[] _buffer, int postion, ushort value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);

        }


        public static void Write(byte[] _buffer, int postion, int value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);

        }

        public static void Write(byte[] _buffer, int postion, uint value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);

        }

        public static void Write(byte[] _buffer, int postion, long value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);
            _buffer[postion + 4] = (byte)(value >> 32);
            _buffer[postion + 5] = (byte)(value >> 40);
            _buffer[postion + 6] = (byte)(value >> 48);
            _buffer[postion + 7] = (byte)(value >> 56);

        }

        public static void Write(byte[] _buffer, int postion, ulong value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);
            _buffer[postion + 4] = (byte)(value >> 32);
            _buffer[postion + 5] = (byte)(value >> 40);
            _buffer[postion + 6] = (byte)(value >> 48);
            _buffer[postion + 7] = (byte)(value >> 56);

        }



        public static short ReadInt16(byte[] m_buffer, int postion)
        {

            return (short)((int)m_buffer[postion + 0] | (int)m_buffer[postion + 1] << 8);
        }

        public static ushort ReadUInt16(byte[] m_buffer, int postion)
        {

            return (ushort)((int)m_buffer[postion + 0] | (int)m_buffer[postion + 1] << 8);
        }

        public static int ReadInt32(byte[] m_buffer, int postion)
        {

            return (int)m_buffer[postion + 0] | (int)m_buffer[postion + 1] << 8 | (int)m_buffer[postion + 2] << 16 | (int)m_buffer[postion + 3] << 24;
        }

        public static uint ReadUInt32(byte[] m_buffer, int postion)
        {

            return (uint)((int)m_buffer[postion + 0] | (int)m_buffer[postion + 1] << 8 | (int)m_buffer[postion + 2] << 16 | (int)m_buffer[postion + 3] << 24);
        }

        public static long ReadInt64(byte[] m_buffer, int postion)
        {

            uint num = (uint)((int)m_buffer[postion + 0] | (int)m_buffer[postion + 1] << 8 | (int)m_buffer[postion + 2] << 16 | (int)m_buffer[postion + 3] << 24);
            uint num2 = (uint)((int)m_buffer[postion + 4] | (int)m_buffer[postion + 5] << 8 | (int)m_buffer[postion + 6] << 16 | (int)m_buffer[postion + 7] << 24);
            return (long)((ulong)num2 << 32 | (ulong)num);
        }

        public static ulong ReadUInt64(byte[] m_buffer, int postion)
        {

            uint num = (uint)((int)m_buffer[postion + 0] | (int)m_buffer[postion + 1] << 8 | (int)m_buffer[postion + 2] << 16 | (int)m_buffer[postion + 3] << 24);
            uint num2 = (uint)((int)m_buffer[postion + 4] | (int)m_buffer[postion + 5] << 8 | (int)m_buffer[postion + 6] << 16 | (int)m_buffer[postion + 7] << 24);
            return (ulong)num2 << 32 | (ulong)num;
        }


        public static short ReadInt16(Span<byte> m_buffer)
        {

            return (short)((int)m_buffer[0] | (int)m_buffer[1] << 8);
        }

        public static ushort ReadUInt16(Span<byte> m_buffer)
        {

            return (ushort)((int)m_buffer[0] | (int)m_buffer[1] << 8);
        }

        public static int ReadInt32(Span<byte> m_buffer)
        {

            return (int)m_buffer[0] | (int)m_buffer[1] << 8 | (int)m_buffer[2] << 16 | (int)m_buffer[3] << 24;
        }

        public static uint ReadUInt32(Span<byte> m_buffer)
        {

            return (uint)((int)m_buffer[0] | (int)m_buffer[1] << 8 | (int)m_buffer[2] << 16 | (int)m_buffer[3] << 24);
        }

        public static long ReadInt64(Span<byte> m_buffer)
        {

            uint num = (uint)((int)m_buffer[0] | (int)m_buffer[1] << 8 | (int)m_buffer[2] << 16 | (int)m_buffer[3] << 24);
            uint num2 = (uint)((int)m_buffer[4] | (int)m_buffer[5] << 8 | (int)m_buffer[6] << 16 | (int)m_buffer[7] << 24);
            return (long)((ulong)num2 << 32 | (ulong)num);
        }

        public static ulong ReadUInt64(Span<byte> m_buffer)
        {

            uint num = (uint)((int)m_buffer[0] | (int)m_buffer[1] << 8 | (int)m_buffer[2] << 16 | (int)m_buffer[3] << 24);
            uint num2 = (uint)((int)m_buffer[4] | (int)m_buffer[5] << 8 | (int)m_buffer[6] << 16 | (int)m_buffer[7] << 24);
            return (ulong)num2 << 32 | (ulong)num;
        }

    }
}
