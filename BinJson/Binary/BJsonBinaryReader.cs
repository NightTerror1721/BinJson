#nullable enable

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Krampus.BinJson.Binary
{
    public sealed class BJsonBinaryReader : IDisposable
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly Stream _stream;
        private readonly bool _leaveOpen;

        public BJsonBinaryReader(Stream stream, bool leaveOpen = false)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(stream));

            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        public BJsonValue Read()
        {
            return ReadValue();
        }

        public void Dispose()
        {
            if (!_leaveOpen)
                _stream.Dispose();
        }

        public static BJsonValue Deserialize(Stream stream, bool leaveOpen = false)
        {
            using var reader = new BJsonBinaryReader(stream, leaveOpen);
            return reader.Read();
        }

        public static BJsonValue Deserialize(ReadOnlySpan<byte> data)
        {
            using var stream = new MemoryStream(data.ToArray(), writable: false);
            using var reader = new BJsonBinaryReader(stream, leaveOpen: true);
            return reader.Read();
        }

        private BJsonValue ReadValue()
        {
            var typeCode = (BJsonValueTypeCode)ReadByte();

            switch (typeCode)
            {
                case BJsonValueTypeCode.Null:
                    return BJsonValue.Null;
                case BJsonValueTypeCode.Int8:
                    return BJsonValue.Create(unchecked((sbyte)ReadByte()));
                case BJsonValueTypeCode.Int16:
                    return BJsonValue.Create(ReadInt16());
                case BJsonValueTypeCode.Int32:
                    return BJsonValue.Create(ReadInt32());
                case BJsonValueTypeCode.Int64:
                    return BJsonValue.Create(ReadInt64());
                case BJsonValueTypeCode.UInt8:
                    return BJsonValue.Create(ReadByte());
                case BJsonValueTypeCode.UInt16:
                    return BJsonValue.Create(ReadUInt16());
                case BJsonValueTypeCode.UInt32:
                    return BJsonValue.Create(ReadUInt32());
                case BJsonValueTypeCode.UInt64:
                    return BJsonValue.Create(ReadUInt64());
                case BJsonValueTypeCode.Float32:
                    return BJsonValue.Create(ReadSingle());
                case BJsonValueTypeCode.Float64:
                    return BJsonValue.Create(ReadDouble());
                case BJsonValueTypeCode.BoolTrue:
                    return BJsonValue.True;
                case BJsonValueTypeCode.BoolFalse:
                    return BJsonValue.False;
                case BJsonValueTypeCode.String:
                    return BJsonValue.Create(ReadStringData());
                case BJsonValueTypeCode.Array:
                    return BJsonValue.Create(ReadArray());
                case BJsonValueTypeCode.Object:
                    return BJsonValue.Create(ReadObject());
                case BJsonValueTypeCode.Binary:
                    return BJsonValue.Create(ReadBinary());
                default:
                    throw new InvalidDataException($"Invalid BJson type code: 0x{(byte)typeCode:X2}");
            }
        }

        private BJsonArray ReadArray()
        {
            int count = ReadNonNegativeLength("Array element count");
            var array = new BJsonArray(count);

            for (int i = 0; i < count; i++)
            {
                array.Add(ReadValue());
            }

            return array;
        }

        private BJsonObject ReadObject()
        {
            int count = ReadNonNegativeLength("Object pair count");
            var obj = new BJsonObject(count);

            for (int i = 0; i < count; i++)
            {
                string key = ReadStringData();
                if (obj.ContainsKey(key))
                    throw new InvalidDataException($"Duplicate object key '{key}' is not allowed.");

                obj.Add(key, ReadValue());
            }

            return obj;
        }

        private BJsonBinary ReadBinary()
        {
            int length = ReadNonNegativeLength("Binary length");
            byte[] data = ReadBytesExact(length);
            return BJsonBinary.CreateUnsafe(data);
        }

        private string ReadStringData()
        {
            int length = ReadNonNegativeLength("String length");
            if (length == 0)
                return string.Empty;

            byte[] data = ReadBytesExact(length);
            return Utf8.GetString(data);
        }

        private byte ReadByte()
        {
            int value = _stream.ReadByte();
            if (value < 0)
                throw new EndOfStreamException("Unexpected end of stream while reading BinJson data.");
            return (byte)value;
        }

        private short ReadInt16()
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        private ushort ReadUInt16()
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        private int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        private uint ReadUInt32()
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        private long ReadInt64()
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }

        private ulong ReadUInt64()
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        private float ReadSingle()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            ReadExactly(buffer);
            return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(buffer));
        }

        private double ReadDouble()
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            ReadExactly(buffer);
            return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(buffer));
        }

        private int ReadNonNegativeLength(string name)
        {
            int value = ReadInt32();
            if (value < 0)
                throw new InvalidDataException($"{name} cannot be negative.");
            return value;
        }

        private byte[] ReadBytesExact(int length)
        {
            var buffer = new byte[length];
            ReadExactly(buffer);
            return buffer;
        }

        private void ReadExactly(Span<byte> buffer)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int bytesRead = _stream.Read(buffer.Slice(totalRead));
                if (bytesRead <= 0)
                    throw new EndOfStreamException("Unexpected end of stream while reading BinJson data.");
                totalRead += bytesRead;
            }
        }
    }
}
