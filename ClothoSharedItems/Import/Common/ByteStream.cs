using System;
using System.Text;

namespace ClothoSharedItems.Common
{
    unsafe public sealed partial class ByteStream
    {
        private int iread = 0;
        private int iwrite = 0;
        private byte[] dtBytes, dtBytesRev;

        public ByteStream(int length) : this(new byte[length])
        {
        }

        public ByteStream(byte[] buffer)
        {
            dtBytes = buffer; dtBytesRev = new byte[16];
        }

        public byte[] Root { get { return dtBytes; } }
        public byte[] Bytes { get { return dtBytes.SubArray(0, iwrite); } }
        public int Capacity { get { return dtBytes.Length; } }
        public int Length { get { return iwrite; } set { iwrite = value; } }
        public int Position { get { return iread; } set { iread = value; } }

        public void Reset(int count)
        {
            Array.Clear(dtBytes, 0, count); iread = 0; iwrite = 0;
        }

        public byte this[int index] { get { return dtBytes[index]; } set { dtBytes[index] = value; } }

        public ByteStream SetLength(int length)
        {
            iwrite = length; return this;
        }

        public ByteStream SetPosition(int position)
        {
            iread = position; return this;
        }

        public ByteStream WriteBytes(byte[] values)
        {
            Array.Copy(values, 0, dtBytes, iwrite, values.Length);
            iwrite += values.Length;
            return this;
        }

        public ByteStream WriteBytes(int pos, byte[] values)
        {
            iwrite = pos; return this.WriteBytes(values);
        }

        public ByteStream WriteBytes(byte[] values, int length)
        {
            Array.Copy(values, 0, dtBytes, iwrite, length);
            iwrite += length;
            return this;
        }

        public ByteStream WriteBytes(int pos, byte[] values, int length)
        {
            iwrite = pos; return this.WriteBytes(values, length);
        }

        public ByteStream WriteBytes(byte[] values, int offset, int length)
        {
            Array.Copy(values, offset, dtBytes, iwrite, length);
            iwrite += length;
            return this;
        }

        public ByteStream WriteBytes(int pos, byte[] values, int offset, int length)
        {
            iwrite = pos; return this.WriteBytes(values, offset, length);
        }

        public ByteStream WriteSBytes(SByte[] values)
        {
            fixed (byte* p = &dtBytes[iwrite])
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ((SByte*)p)[i] = values[i];
                }
                iwrite += values.Length * sizeof(SByte);
            }
            return this;
        }

        public ByteStream WriteInt16s(Int16[] values)
        {
            fixed (byte* p = &dtBytes[iwrite])
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ((Int16*)p)[i] = values[i];
                }
                iwrite += values.Length * sizeof(Int16);
            }
            return this;
        }

        public ByteStream WriteUInt16s(UInt16[] values)
        {
            fixed (byte* p = &dtBytes[iwrite])
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ((UInt16*)p)[i] = values[i];
                }
                iwrite += values.Length * sizeof(UInt16);
            }
            return this;
        }

        public ByteStream WriteInt32s(Int32[] values)
        {
            fixed (byte* p = &dtBytes[iwrite])
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ((Int32*)p)[i] = values[i];
                }
                iwrite += values.Length * sizeof(Int32);
            }
            return this;
        }

        public ByteStream WriteUInt32s(UInt32[] values)
        {
            fixed (byte* p = &dtBytes[iwrite])
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ((UInt32*)p)[i] = values[i];
                }
                iwrite += values.Length * sizeof(UInt32);
            }
            return this;
        }

        public ByteStream WriteInt64s(Int64[] values)
        {
            fixed (byte* p = &dtBytes[iwrite])
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ((Int64*)p)[i] = values[i];
                }
                iwrite += values.Length * sizeof(Int64);
            }
            return this;
        }

        public ByteStream WriteUInt64s(UInt64[] values)
        {
            fixed (byte* p = &dtBytes[iwrite])
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ((UInt64*)p)[i] = values[i];
                }
                iwrite += values.Length * sizeof(UInt64);
            }
            return this;
        }

        public ByteStream WriteByte(Byte value)
        {
            dtBytes[iwrite++] = value;
            return this;
        }

        public ByteStream WriteByte(int pos, Byte value)
        {
            iwrite = pos; return this.WriteByte(value);
        }

        public ByteStream WriteSByte(SByte value)
        {
            fixed (byte* p = &dtBytes[iwrite]) { *((SByte*)p) = value; }
            iwrite += sizeof(SByte);
            return this;
        }

        public ByteStream WriteSByte(int pos, SByte value)
        {
            iwrite = pos; return this.WriteSByte(value);
        }

        public ByteStream WriteInt16(Int16 value)
        {
            fixed (byte* p = &dtBytes[iwrite]) { *((Int16*)p) = value; }
            iwrite += sizeof(Int16);
            return this;
        }

        public ByteStream WriteInt16(int pos, Int16 value)
        {
            iwrite = pos; return this.WriteInt16(value);
        }

        public ByteStream WriteUInt16(UInt16 value)
        {
            fixed (byte* p = &dtBytes[iwrite]) { *((UInt16*)p) = value; }
            iwrite += sizeof(UInt16);
            return this;
        }

        public ByteStream WriteUInt16(int pos, UInt16 value)
        {
            iwrite = pos; return this.WriteUInt16(value);
        }

        public ByteStream WriteInt32(Int32 value)
        {
            fixed (byte* p = &dtBytes[iwrite]) { *((Int32*)p) = value; }
            iwrite += sizeof(Int32);
            return this;
        }

        public ByteStream WriteInt32(int pos, Int32 value)
        {
            iwrite = pos; return this.WriteInt32(value);
        }

        public ByteStream WriteUInt32(UInt32 value)
        {
            fixed (byte* p = &dtBytes[iwrite]) { *((UInt32*)p) = value; }
            iwrite += sizeof(UInt32);
            return this;
        }

        public ByteStream WriteUInt32(int pos, UInt32 value)
        {
            iwrite = pos; return this.WriteUInt32(value);
        }

        public ByteStream WriteInt64(Int64 value)
        {
            fixed (byte* p = &dtBytes[iwrite]) { *((Int64*)p) = value; }
            iwrite += sizeof(Int64);
            return this;
        }

        public ByteStream WriteInt64(int pos, Int64 value)
        {
            iwrite = pos; return this.WriteInt64(value);
        }

        public ByteStream WriteUInt64(UInt64 value)
        {
            fixed (byte* p = &dtBytes[iwrite]) { *((UInt64*)p) = value; }
            iwrite += sizeof(UInt64);
            return this;
        }

        public ByteStream WriteUInt64(int pos, UInt64 value)
        {
            iwrite = pos; return this.WriteUInt64(value);
        }

        public ByteStream WriteSingle(Single value)
        {
            fixed (byte* p = &dtBytes[iwrite]) { *((Single*)p) = value; }
            iwrite += sizeof(Single);
            return this;
        }

        public ByteStream WriteSingle(int pos, Single value)
        {
            iwrite = pos; return this.WriteSingle(value);
        }

        public ByteStream WriteDouble(Double value)
        {
            fixed (byte* p = &dtBytes[iwrite]) { *((Double*)p) = value; }
            iwrite += sizeof(Double);
            return this;
        }

        public ByteStream WriteDouble(int pos, Double value)
        {
            iwrite = pos; return this.WriteDouble(value);
        }

        public ByteStream WriteString(String value)
        {
            return WriteBytes(ASCIIEncoding.UTF8.GetBytes(value));
        }

        public ByteStream WriteString(int pos, String value)
        {
            iwrite = pos; return this.WriteString(value);
        }

        public byte[] ReadBytes(int length)
        {
            byte[] result = new byte[length];
            Array.Copy(dtBytes, iread, result, 0, length);
            iread += length;
            return result;
        }

        public byte[] ReadBytes(int length, Converter<byte, byte> converter)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = converter(dtBytes[iread + i]);
            }
            iread += length;
            return result;
        }

        public byte[] ReadBytes(int pos, int length)
        {
            iread = pos; return this.ReadBytes(length);
        }

        public byte[] ReadBytes(int pos, int length, Converter<byte, byte> converter)
        {
            iread = pos; return this.ReadBytes(length, converter);
        }

        public SByte[] ReadSBytes(int length)
        {
            SByte[] result = new SByte[length];
            fixed (byte* p = &dtBytes[iread])
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = ((SByte*)p)[i];
                }
                iread += length * sizeof(SByte);
            }
            return result;
        }

        public Int16[] ReadInt16s(int length)
        {
            Int16[] result = new Int16[length];
            fixed (byte* p = &dtBytes[iread])
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = ((Int16*)p)[i];
                }
                iread += length * sizeof(Int16);
            }
            return result;
        }

        public UInt16[] ReadUInt16s(int length)
        {
            UInt16[] result = new UInt16[length];
            fixed (byte* p = &dtBytes[iread])
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = ((UInt16*)p)[i];
                }
                iread += length * sizeof(UInt16);
            }
            return result;
        }

        public Int32[] ReadInt32s(int length)
        {
            Int32[] result = new Int32[length];
            fixed (byte* p = &dtBytes[iread])
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = ((Int32*)p)[i];
                }
                iread += length * sizeof(Int32);
            }
            return result;
        }

        public UInt32[] ReadUInt32s(int length)
        {
            UInt32[] result = new UInt32[length];
            fixed (byte* p = &dtBytes[iread])
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = ((UInt32*)p)[i];
                }
                iread += length * sizeof(UInt32);
            }
            return result;
        }

        public Int64[] ReadInt64s(int length)
        {
            Int64[] result = new Int64[length];
            fixed (byte* p = &dtBytes[iread])
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = ((Int64*)p)[i];
                }
                iread += length * sizeof(Int64);
            }
            return result;
        }

        public UInt64[] ReadUInt64s(int length)
        {
            UInt64[] result = new UInt64[length];
            fixed (byte* p = &dtBytes[iread])
            {
                for (int i = 0; i < length; i++)
                {
                    result[i] = ((UInt64*)p)[i];
                }
                iread += length * sizeof(UInt64);
            }
            return result;
        }

        public Byte ReadByte()
        {
            byte result = dtBytes[iread];
            iread += sizeof(Byte);
            return result;
        }

        public Byte ReadByte(int pos)
        {
            iread = pos; return ReadByte();
        }

        public SByte ReadSByte()
        {
            SByte result;
            fixed (byte* p = &dtBytes[iread]) { result = *((SByte*)p); }
            iread += sizeof(SByte);
            return result;
        }

        public SByte ReadSByte(int pos)
        {
            iread = pos; return this.ReadSByte();
        }

        public Int16 ReadInt16()
        {
            Int16 result;
            fixed (byte* p = &dtBytes[iread]) { result = *((Int16*)p); }
            iread += sizeof(Int16);
            return result;
        }

        public Int16 ReadInt16(int pos)
        {
            iread = pos; return this.ReadInt16();
        }

        public UInt16 ReadUInt16()
        {
            UInt16 result;
            fixed (byte* p = &dtBytes[iread]) { result = *((UInt16*)p); }
            iread += sizeof(UInt16);
            return result;
        }

        public UInt16 ReadUInt16(int pos)
        {
            iread = pos; return this.ReadUInt16();
        }

        public Int32 ReadInt32()
        {
            Int32 result;
            fixed (byte* p = &dtBytes[iread]) { result = *((Int32*)p); }
            iread += sizeof(Int32);
            return result;
        }

        public Int32 ReadInt32(int pos)
        {
            iread = pos; return this.ReadInt32();
        }

        public UInt32 ReadUInt32()
        {
            UInt32 result;
            fixed (byte* p = &dtBytes[iread]) { result = *((UInt32*)p); }
            iread += sizeof(UInt32);
            return result;
        }

        public UInt32 ReadUInt32(int pos)
        {
            iread = pos; return this.ReadUInt32();
        }

        public Int64 ReadInt64()
        {
            Int64 result;
            fixed (byte* p = &dtBytes[iread]) { result = *((Int64*)p); }
            iread += sizeof(Int64);
            return result;
        }

        public Int64 ReadInt64(int pos)
        {
            iread = pos; return this.ReadInt64();
        }

        public UInt64 ReadUInt64()
        {
            UInt64 result;
            fixed (byte* p = &dtBytes[iread]) { result = *((UInt64*)p); }
            iread += sizeof(UInt64);
            return result;
        }

        public UInt64 ReadUInt64(int pos)
        {
            iread = pos; return this.ReadUInt64();
        }

        public Single ReadSingle()
        {
            Single result;
            fixed (byte* p = &dtBytes[iread]) { result = *((Single*)p); }
            iread += sizeof(Single);
            return result;
        }

        public Single ReadSingle(int pos)
        {
            iread = pos; return this.ReadSingle();
        }

        public Single ReadSingleReverse()
        {
            readReverse(sizeof(Single));
            fixed (byte* p = dtBytesRev) { return *((Single*)p); }
        }

        public Single ReadSingleReverse(int pos)
        {
            iread = pos; return this.ReadSingleReverse();
        }

        public Double ReadDouble()
        {
            Double result;
            fixed (byte* p = &dtBytes[iread]) { result = *((Double*)p); }
            iread += sizeof(Double);
            return result;
        }

        public Double ReadDouble(int pos)
        {
            iread = pos; return this.ReadDouble();
        }

        public Double ReadDoubleReverse()
        {
            readReverse(sizeof(Double));
            fixed (byte* p = dtBytesRev) { return *((Double*)p); }
        }

        public Double ReadDoubleReverse(int pos)
        {
            iread = pos; return this.ReadDoubleReverse();
        }

        public String ReadString(int count)
        {
            string result = ASCIIEncoding.UTF8.GetString(dtBytes, iread, count);
            iread += count;
            return result;
        }

        public String ReadString(int pos, int count)
        {
            iread = pos; return this.ReadString(count);
        }

        public String ReadString(int count, bool untilEOF = false)
        {
            if (untilEOF)
            {
                int countUntilEOF = 0;
                do
                {
                    if (dtBytes[iread + countUntilEOF] == 0)
                        break;
                    countUntilEOF++;
                } while (countUntilEOF < count);

                count = countUntilEOF;
            }

            string result = ASCIIEncoding.UTF8.GetString(dtBytes, iread, count);
            iread += count;
            return result;
        }

        public String ReadString(int pos, int count, bool untilEOF = false)
        {
            iread = pos; return this.ReadString(count, untilEOF);
        }

        private void readReverse(int len)
        {
            iread += len;
            for (int i = 0; i < len; i++)
                dtBytesRev[i] = dtBytes[iread - i - 1];
        }

        public string ToString(int length = -1)
        {
            return length < 0 ? ByteStream.ToText(dtBytes, iwrite) : ByteStream.ToText(dtBytes, length);
        }
    }
}