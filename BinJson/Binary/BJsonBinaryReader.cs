#nullable enable

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Krampus.BinJson.Error;

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
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");
            if (!stream.CanRead)
                throw new BJsonValidationException("Stream must be readable.");

            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        public BJsonValue Read()
        {
            try
            {
                return ReadValue();
            }
            catch (Exception ex) when (!(ex is BJsonException))
            {
                throw new BJsonBinaryFormatException("Failed to deserialize binary BinJson payload.", ex);
            }
        }

        public async Task<BJsonValue> ReadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await ReadValueAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is BJsonException))
            {
                throw new BJsonBinaryFormatException("Failed to deserialize binary BinJson payload.", ex);
            }
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

        public static async Task<BJsonValue> DeserializeAsync(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            using var reader = new BJsonBinaryReader(stream, leaveOpen);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        public static BJsonValue Deserialize(ReadOnlySpan<byte> data)
        {
            using var stream = new MemoryStream(data.ToArray(), writable: false);
            using var reader = new BJsonBinaryReader(stream, leaveOpen: true);
            return reader.Read();
        }

        public static async Task<BJsonValue> DeserializeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream(data.ToArray(), writable: false);
            using var reader = new BJsonBinaryReader(stream, leaveOpen: true);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
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
                    throw new BJsonBinaryFormatException($"Invalid BJson type code: 0x{(byte)typeCode:X2}");
            }
        }

        private async Task<BJsonValue> ReadValueAsync(CancellationToken cancellationToken)
        {
            var typeCode = (BJsonValueTypeCode)await ReadByteAsync(cancellationToken).ConfigureAwait(false);

            switch (typeCode)
            {
                case BJsonValueTypeCode.Null:
                    return BJsonValue.Null;
                case BJsonValueTypeCode.Int8:
                    return BJsonValue.Create(unchecked((sbyte)await ReadByteAsync(cancellationToken).ConfigureAwait(false)));
                case BJsonValueTypeCode.Int16:
                    return BJsonValue.Create(await ReadInt16Async(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.Int32:
                    return BJsonValue.Create(await ReadInt32Async(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.Int64:
                    return BJsonValue.Create(await ReadInt64Async(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.UInt8:
                    return BJsonValue.Create(await ReadByteAsync(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.UInt16:
                    return BJsonValue.Create(await ReadUInt16Async(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.UInt32:
                    return BJsonValue.Create(await ReadUInt32Async(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.UInt64:
                    return BJsonValue.Create(await ReadUInt64Async(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.Float32:
                    return BJsonValue.Create(await ReadSingleAsync(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.Float64:
                    return BJsonValue.Create(await ReadDoubleAsync(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.BoolTrue:
                    return BJsonValue.True;
                case BJsonValueTypeCode.BoolFalse:
                    return BJsonValue.False;
                case BJsonValueTypeCode.String:
                    return BJsonValue.Create(await ReadStringDataAsync(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.Array:
                    return BJsonValue.Create(await ReadArrayAsync(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.Object:
                    return BJsonValue.Create(await ReadObjectAsync(cancellationToken).ConfigureAwait(false));
                case BJsonValueTypeCode.Binary:
                    return BJsonValue.Create(await ReadBinaryAsync(cancellationToken).ConfigureAwait(false));
                default:
                    throw new BJsonBinaryFormatException($"Invalid BJson type code: 0x{(byte)typeCode:X2}");
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

        private async Task<BJsonArray> ReadArrayAsync(CancellationToken cancellationToken)
        {
            int count = await ReadNonNegativeLengthAsync("Array element count", cancellationToken).ConfigureAwait(false);
            var array = new BJsonArray(count);

            for (int i = 0; i < count; i++)
            {
                array.Add(await ReadValueAsync(cancellationToken).ConfigureAwait(false));
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
                    throw new BJsonBinaryFormatException($"Duplicate object key '{key}' is not allowed.");

                obj.Add(key, ReadValue());
            }

            return obj;
        }

        private async Task<BJsonObject> ReadObjectAsync(CancellationToken cancellationToken)
        {
            int count = await ReadNonNegativeLengthAsync("Object pair count", cancellationToken).ConfigureAwait(false);
            var obj = new BJsonObject(count);

            for (int i = 0; i < count; i++)
            {
                string key = await ReadStringDataAsync(cancellationToken).ConfigureAwait(false);
                if (obj.ContainsKey(key))
                    throw new BJsonBinaryFormatException($"Duplicate object key '{key}' is not allowed.");

                obj.Add(key, await ReadValueAsync(cancellationToken).ConfigureAwait(false));
            }

            return obj;
        }

        private BJsonBinary ReadBinary()
        {
            int length = ReadNonNegativeLength("Binary length");
            byte[] data = ReadBytesExact(length);
            return BJsonBinary.CreateUnsafe(data);
        }

        private async Task<BJsonBinary> ReadBinaryAsync(CancellationToken cancellationToken)
        {
            int length = await ReadNonNegativeLengthAsync("Binary length", cancellationToken).ConfigureAwait(false);
            byte[] data = await ReadBytesExactAsync(length, cancellationToken).ConfigureAwait(false);
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

        private async Task<string> ReadStringDataAsync(CancellationToken cancellationToken)
        {
            int length = await ReadNonNegativeLengthAsync("String length", cancellationToken).ConfigureAwait(false);
            if (length == 0)
                return string.Empty;

            byte[] data = await ReadBytesExactAsync(length, cancellationToken).ConfigureAwait(false);
            return Utf8.GetString(data);
        }

        private byte ReadByte()
        {
            int value = _stream.ReadByte();
            if (value < 0)
                throw new BJsonBinaryFormatException("Unexpected end of stream while reading BinJson data.");
            return (byte)value;
        }

        private async Task<byte> ReadByteAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1];
            int read = await _stream.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);
            if (read <= 0)
                throw new BJsonBinaryFormatException("Unexpected end of stream while reading BinJson data.");
            return buffer[0];
        }

        private short ReadInt16()
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        private async Task<short> ReadInt16Async(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[sizeof(short)];
            await ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        private ushort ReadUInt16()
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        private async Task<ushort> ReadUInt16Async(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[sizeof(ushort)];
            await ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        private int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        private async Task<int> ReadInt32Async(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[sizeof(int)];
            await ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        private uint ReadUInt32()
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        private async Task<uint> ReadUInt32Async(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[sizeof(uint)];
            await ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        private long ReadInt64()
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }

        private async Task<long> ReadInt64Async(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[sizeof(long)];
            await ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }

        private ulong ReadUInt64()
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        private async Task<ulong> ReadUInt64Async(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[sizeof(ulong)];
            await ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        private float ReadSingle()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            ReadExactly(buffer);
            return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(buffer));
        }

        private async Task<float> ReadSingleAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[sizeof(int)];
            await ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(buffer));
        }

        private double ReadDouble()
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            ReadExactly(buffer);
            return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(buffer));
        }

        private async Task<double> ReadDoubleAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[sizeof(long)];
            await ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(buffer));
        }

        private int ReadNonNegativeLength(string name)
        {
            int value = ReadInt32();
            if (value < 0)
                throw new BJsonBinaryFormatException($"{name} cannot be negative.");
            return value;
        }

        private async Task<int> ReadNonNegativeLengthAsync(string name, CancellationToken cancellationToken)
        {
            int value = await ReadInt32Async(cancellationToken).ConfigureAwait(false);
            if (value < 0)
                throw new BJsonBinaryFormatException($"{name} cannot be negative.");
            return value;
        }

        private byte[] ReadBytesExact(int length)
        {
            var buffer = new byte[length];
            ReadExactly(buffer);
            return buffer;
        }

        private async Task<byte[]> ReadBytesExactAsync(int length, CancellationToken cancellationToken)
        {
            var buffer = new byte[length];
            await ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return buffer;
        }

        private void ReadExactly(Span<byte> buffer)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int bytesRead = _stream.Read(buffer.Slice(totalRead));
                if (bytesRead <= 0)
                    throw new BJsonBinaryFormatException("Unexpected end of stream while reading BinJson data.");
                totalRead += bytesRead;
            }
        }

        private async Task ReadExactlyAsync(byte[] buffer, CancellationToken cancellationToken)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int bytesRead = await _stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, cancellationToken).ConfigureAwait(false);
                if (bytesRead <= 0)
                    throw new BJsonBinaryFormatException("Unexpected end of stream while reading BinJson data.");
                totalRead += bytesRead;
            }
        }
    }
}
