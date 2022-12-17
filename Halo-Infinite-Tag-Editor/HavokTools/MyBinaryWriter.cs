using System;
using System.Collections.Generic;
using System.Buffers.Binary;

namespace HavokScriptToolsCommon
{
    class MyBinaryWriter
    {
        private readonly List<byte> data;
        private bool littleEndian;
        public MyBinaryWriter(bool littleEndian=true)
        {
            this.littleEndian = littleEndian;
            data = new List<byte>();
        }

        public void SetLittleEndian(bool isLittleEndian)
        {
            littleEndian = isLittleEndian;
        }

        public void WriteBool(bool value)
        {
            WriteUInt8((byte)(value ? 1 : 0));
        }

        public void WriteInt8(sbyte value)
        {
            WriteUInt8((byte)value);
        }

        public void WriteUInt8(byte value)
        {

            data.Add(value);
        }

        public void WriteInt16(short value)
        {
            byte[] bytes = new byte[sizeof(short)];
            if (littleEndian)
            {
                BinaryPrimitives.WriteInt16LittleEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteInt16BigEndian(bytes, value);
            }
            data.AddRange(bytes);
        }

        public void WriteUInt16(ushort value)
        {
            WriteInt16((short)value);
        }

        public void WriteInt32(int value)
        {
            byte[] bytes = new byte[sizeof(int)];
            if (littleEndian)
            {
                BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            }
            data.AddRange(bytes);
        }

        public void WriteUInt32(uint value)
        {
            WriteInt32((int)value);
        }

        public void WriteInt64(long value)
        {
            byte[] bytes = new byte[sizeof(long)];
            if (littleEndian)
            {
                BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteInt64BigEndian(bytes, value);
            }
            data.AddRange(bytes);
        }

        public void WriteUInt64(ulong value)
        {
            WriteInt64((long)value);
        }

        public void WriteFloat(float value)
        {
            byte[] bytes = new byte[sizeof(float)];
            if (littleEndian)
            {
                BinaryPrimitives.WriteSingleLittleEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteSingleBigEndian(bytes, value);
            }
            data.AddRange(bytes);
        }

        public void WriteDouble(double value)
        {
            byte[] bytes = new byte[sizeof(double)];
            if (littleEndian)
            {
                BinaryPrimitives.WriteDoubleLittleEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteDoubleBigEndian(bytes, value);
            }
            data.AddRange(bytes);
        }

        public void WriteString(string value)
        {
            data.AddRange(System.Text.Encoding.UTF8.GetBytes(value));
        }

        public void WriteBytes(byte[] value)
        {
            data.AddRange(value);
        }

        public void Pad(int padding, byte value=0)
        {
            if (data.Count % padding != 0)
            {
                int size = padding - (data.Count % padding);
                for (int i = 0; i < size; i++)
                {
                    data.Add(value);
                }
            }
        }

        public List<byte> GetData()
        {
            return data;
        }
    }
}
