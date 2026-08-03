#nullable enable

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Binary
{
    public sealed class BJsonBinaryWriter : IDisposable
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly Stream _stream;
        private readonly bool _leaveOpen;

        public BJsonBinaryWriter(Stream stream, bool leaveOpen = false)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");
            if (!stream.CanWrite)
                throw new BJsonValidationException("Stream must be writable.");

            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        public void Write(BJsonValue value)
        {
            try
            {
                WriteValue(value);
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw new BJsonSerializationException("Failed to serialize BinJson value to binary format.", ex);
            }
        }

        public void Flush()
        {
            try
            {
                _stream.Flush();
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw new BJsonSerializationException("Failed to flush binary BinJson writer.", ex);
            }
        }

        public void Dispose()
        {
            if (!_leaveOpen)
                _stream.Dispose();
        }

        public static void Serialize(Stream stream, BJsonValue value, bool leaveOpen = false)
        {
            using var writer = new BJsonBinaryWriter(stream, leaveOpen);
            writer.Write(value);
            writer.Flush();
        }

        public static byte[] Serialize(BJsonValue value)
        {
            using var stream = new MemoryStream();
            using var writer = new BJsonBinaryWriter(stream, leaveOpen: true);
            writer.Write(value);
            writer.Flush();
            return stream.ToArray();
        }

        private void WriteValue(BJsonValue value)
        {
            switch (value.Type)
            {
                case BJsonValueType.Null:
                    WriteTypeCode(BJsonValueTypeCode.Null);
                    return;
                case BJsonValueType.Integer:
                    WriteInteger(value);
                    return;
                case BJsonValueType.Float:
                    WriteFloat(value);
                    return;
                case BJsonValueType.Boolean:
                    WriteTypeCode(value.BoolValue ? BJsonValueTypeCode.BoolTrue : BJsonValueTypeCode.BoolFalse);
                    return;
                case BJsonValueType.String:
                    WriteString(value.StringValue);
                    return;
                case BJsonValueType.Array:
                    WriteArray(value.ArrayValue);
                    return;
                case BJsonValueType.Object:
                    WriteObject(value.ObjectValue);
                    return;
                case BJsonValueType.Binary:
                    WriteBinary(value.BinaryValue);
                    return;
                default:
                    throw new BJsonSerializationException($"Unsupported BJsonValueType: {value.Type}");
            }
        }

        private void WriteInteger(BJsonValue value)
        {
            ulong rawValue = value.ULongValue;
            long signedValue = unchecked((long)rawValue);

            if (signedValue < 0)
            {
                if (signedValue >= sbyte.MinValue && signedValue <= sbyte.MaxValue)
                {
                    WriteTypeCode(BJsonValueTypeCode.Int8);
                    _stream.WriteByte(unchecked((byte)(sbyte)signedValue));
                    return;
                }
                if (signedValue >= short.MinValue && signedValue <= short.MaxValue)
                {
                    WriteTypeCode(BJsonValueTypeCode.Int16);
                    WriteInt16((short)signedValue);
                    return;
                }
                if (signedValue >= int.MinValue && signedValue <= int.MaxValue)
                {
                    WriteTypeCode(BJsonValueTypeCode.Int32);
                    WriteInt32((int)signedValue);
                    return;
                }

                WriteTypeCode(BJsonValueTypeCode.Int64);
                WriteInt64(signedValue);
                return;
            }

            if (rawValue <= byte.MaxValue)
            {
                WriteTypeCode(BJsonValueTypeCode.UInt8);
                WriteByte((byte)rawValue);
                return;
            }
            if (rawValue <= ushort.MaxValue)
            {
                WriteTypeCode(BJsonValueTypeCode.UInt16);
                WriteUInt16((ushort)rawValue);
                return;
            }
            if (rawValue <= uint.MaxValue)
            {
                WriteTypeCode(BJsonValueTypeCode.UInt32);
                WriteUInt32((uint)rawValue);
                return;
            }

            WriteTypeCode(BJsonValueTypeCode.UInt64);
            WriteUInt64(rawValue);
        }

        private void WriteFloat(BJsonValue value)
        {
            double doubleValue = value.DoubleValue;
            float singleValue = (float)doubleValue;

            if (CanRoundTripAsSingle(doubleValue, singleValue))
            {
                WriteTypeCode(BJsonValueTypeCode.Float32);
                WriteSingle(singleValue);
                return;
            }

            WriteTypeCode(BJsonValueTypeCode.Float64);
            WriteDouble(doubleValue);
        }

        private void WriteArray(BJsonArray array)
        {
            WriteTypeCode(BJsonValueTypeCode.Array);
            WriteInt32(array.Count);

            for (int i = 0; i < array.Count; i++)
            {
                WriteValue(array[i]);
            }
        }

        private void WriteObject(BJsonObject obj)
        {
            WriteTypeCode(BJsonValueTypeCode.Object);
            WriteInt32(obj.Count);

            foreach (var pair in obj)
            {
                WriteStringData(pair.Key);
                WriteValue(pair.Value);
            }
        }

        private void WriteString(string value)
        {
            WriteTypeCode(BJsonValueTypeCode.String);
            WriteStringData(value);
        }

        private void WriteBinary(BJsonBinary value)
        {
            WriteTypeCode(BJsonValueTypeCode.Binary);
            WriteInt32(value.Count);
            _stream.Write(value.AsSpan());
        }

        private void WriteStringData(string value)
        {
            byte[] bytes = Utf8.GetBytes(value);
            WriteInt32(bytes.Length);
            _stream.Write(bytes, 0, bytes.Length);
        }

        private void WriteTypeCode(BJsonValueTypeCode code)
        {
            _stream.WriteByte((byte)code);
        }

        private void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        private void WriteInt16(short value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        private void WriteUInt16(ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        private void WriteInt32(int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        private void WriteUInt32(uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        private void WriteInt64(long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        private void WriteUInt64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            _stream.Write(buffer);
        }

        private void WriteSingle(float value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, BitConverter.SingleToInt32Bits(value));
            _stream.Write(buffer);
        }

        private void WriteDouble(double value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, BitConverter.DoubleToInt64Bits(value));
            _stream.Write(buffer);
        }

        private static bool CanRoundTripAsSingle(double doubleValue, float singleValue)
        {
            if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                return false;

            double roundTripped = singleValue;
            return BitConverter.DoubleToInt64Bits(doubleValue) == BitConverter.DoubleToInt64Bits(roundTripped);
        }
    }
}
