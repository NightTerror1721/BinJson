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
    public sealed class BJsonBinaryWriter : IDisposable
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly byte[] _singleByteBuffer;
        private readonly byte[] _numericBuffer;
        private PathSegment[] _pathSegments;
        private int _pathDepth;
        private long _bytesWritten;

        public BJsonBinaryWriter(Stream stream, bool leaveOpen = false)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");
            if (!stream.CanWrite)
                throw new BJsonValidationException("Stream must be writable.");

            _stream = stream;
            _leaveOpen = leaveOpen;
            _singleByteBuffer = new byte[1];
            _numericBuffer = new byte[8];
            _pathSegments = Array.Empty<PathSegment>();
            _pathDepth = 0;
            _bytesWritten = 0;
        }

        public void Write(BJsonValue value)
        {
            try
            {
                WriteValue(value);
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw CreateSerializationException("Failed to serialize BinJson value to binary format.", "WriteValue", ex);
            }
        }

        public async Task WriteAsync(BJsonValue value, CancellationToken cancellationToken = default)
        {
            try
            {
                await WriteValueAsync(value, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw CreateSerializationException("Failed to serialize BinJson value to binary format.", "WriteValueAsync", ex);
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
                throw CreateSerializationException("Failed to flush binary BinJson writer.", "Flush", ex);
            }
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw CreateSerializationException("Failed to flush binary BinJson writer.", "FlushAsync", ex);
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

        public static async Task SerializeAsync(Stream stream, BJsonValue value, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            using var writer = new BJsonBinaryWriter(stream, leaveOpen);
            await writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public static byte[] Serialize(BJsonValue value)
        {
            using var stream = new MemoryStream();
            using var writer = new BJsonBinaryWriter(stream, leaveOpen: true);
            writer.Write(value);
            writer.Flush();
            return stream.ToArray();
        }

        public static async Task<byte[]> SerializeAsync(BJsonValue value, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            using var writer = new BJsonBinaryWriter(stream, leaveOpen: true);
            await writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
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
                    throw CreateSerializationException($"Unsupported BJsonValueType: {value.Type}", "WriteValue");
            }
        }

        private async Task WriteValueAsync(BJsonValue value, CancellationToken cancellationToken)
        {
            switch (value.Type)
            {
                case BJsonValueType.Null:
                    await WriteTypeCodeAsync(BJsonValueTypeCode.Null, cancellationToken).ConfigureAwait(false);
                    return;
                case BJsonValueType.Integer:
                    await WriteIntegerAsync(value, cancellationToken).ConfigureAwait(false);
                    return;
                case BJsonValueType.Float:
                    await WriteFloatAsync(value, cancellationToken).ConfigureAwait(false);
                    return;
                case BJsonValueType.Boolean:
                    await WriteTypeCodeAsync(value.BoolValue ? BJsonValueTypeCode.BoolTrue : BJsonValueTypeCode.BoolFalse, cancellationToken).ConfigureAwait(false);
                    return;
                case BJsonValueType.String:
                    await WriteStringAsync(value.StringValue, cancellationToken).ConfigureAwait(false);
                    return;
                case BJsonValueType.Array:
                    await WriteArrayAsync(value.ArrayValue, cancellationToken).ConfigureAwait(false);
                    return;
                case BJsonValueType.Object:
                    await WriteObjectAsync(value.ObjectValue, cancellationToken).ConfigureAwait(false);
                    return;
                case BJsonValueType.Binary:
                    await WriteBinaryAsync(value.BinaryValue, cancellationToken).ConfigureAwait(false);
                    return;
                default:
                    throw CreateSerializationException($"Unsupported BJsonValueType: {value.Type}", "WriteValueAsync");
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

        private async Task WriteIntegerAsync(BJsonValue value, CancellationToken cancellationToken)
        {
            ulong rawValue = value.ULongValue;
            long signedValue = unchecked((long)rawValue);

            if (signedValue < 0)
            {
                if (signedValue >= sbyte.MinValue && signedValue <= sbyte.MaxValue)
                {
                    await WriteTypeCodeAsync(BJsonValueTypeCode.Int8, cancellationToken).ConfigureAwait(false);
                    await WriteByteAsync(unchecked((byte)(sbyte)signedValue), cancellationToken).ConfigureAwait(false);
                    return;
                }
                if (signedValue >= short.MinValue && signedValue <= short.MaxValue)
                {
                    await WriteTypeCodeAsync(BJsonValueTypeCode.Int16, cancellationToken).ConfigureAwait(false);
                    await WriteInt16Async((short)signedValue, cancellationToken).ConfigureAwait(false);
                    return;
                }
                if (signedValue >= int.MinValue && signedValue <= int.MaxValue)
                {
                    await WriteTypeCodeAsync(BJsonValueTypeCode.Int32, cancellationToken).ConfigureAwait(false);
                    await WriteInt32Async((int)signedValue, cancellationToken).ConfigureAwait(false);
                    return;
                }

                await WriteTypeCodeAsync(BJsonValueTypeCode.Int64, cancellationToken).ConfigureAwait(false);
                await WriteInt64Async(signedValue, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (rawValue <= byte.MaxValue)
            {
                await WriteTypeCodeAsync(BJsonValueTypeCode.UInt8, cancellationToken).ConfigureAwait(false);
                await WriteByteAsync((byte)rawValue, cancellationToken).ConfigureAwait(false);
                return;
            }
            if (rawValue <= ushort.MaxValue)
            {
                await WriteTypeCodeAsync(BJsonValueTypeCode.UInt16, cancellationToken).ConfigureAwait(false);
                await WriteUInt16Async((ushort)rawValue, cancellationToken).ConfigureAwait(false);
                return;
            }
            if (rawValue <= uint.MaxValue)
            {
                await WriteTypeCodeAsync(BJsonValueTypeCode.UInt32, cancellationToken).ConfigureAwait(false);
                await WriteUInt32Async((uint)rawValue, cancellationToken).ConfigureAwait(false);
                return;
            }

            await WriteTypeCodeAsync(BJsonValueTypeCode.UInt64, cancellationToken).ConfigureAwait(false);
            await WriteUInt64Async(rawValue, cancellationToken).ConfigureAwait(false);
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

        private async Task WriteFloatAsync(BJsonValue value, CancellationToken cancellationToken)
        {
            double doubleValue = value.DoubleValue;
            float singleValue = (float)doubleValue;

            if (CanRoundTripAsSingle(doubleValue, singleValue))
            {
                await WriteTypeCodeAsync(BJsonValueTypeCode.Float32, cancellationToken).ConfigureAwait(false);
                await WriteSingleAsync(singleValue, cancellationToken).ConfigureAwait(false);
                return;
            }

            await WriteTypeCodeAsync(BJsonValueTypeCode.Float64, cancellationToken).ConfigureAwait(false);
            await WriteDoubleAsync(doubleValue, cancellationToken).ConfigureAwait(false);
        }

        private void WriteArray(BJsonArray array)
        {
            WriteTypeCode(BJsonValueTypeCode.Array);
            WriteInt32(array.Count);

            for (int i = 0; i < array.Count; i++)
            {
                PushIndexPathSegment(i);
                try
                {
                    WriteValue(array[i]);
                }
                finally
                {
                    PopPathSegment();
                }
            }
        }

        private async Task WriteArrayAsync(BJsonArray array, CancellationToken cancellationToken)
        {
            await WriteTypeCodeAsync(BJsonValueTypeCode.Array, cancellationToken).ConfigureAwait(false);
            await WriteInt32Async(array.Count, cancellationToken).ConfigureAwait(false);

            for (int i = 0; i < array.Count; i++)
            {
                PushIndexPathSegment(i);
                try
                {
                    await WriteValueAsync(array[i], cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    PopPathSegment();
                }
            }
        }

        private void WriteObject(BJsonObject obj)
        {
            WriteTypeCode(BJsonValueTypeCode.Object);
            WriteInt32(obj.Count);

            foreach (var pair in obj)
            {
                WriteStringData(pair.Key);
                PushPropertyPathSegment(pair.Key);
                try
                {
                    WriteValue(pair.Value);
                }
                finally
                {
                    PopPathSegment();
                }
            }
        }

        private async Task WriteObjectAsync(BJsonObject obj, CancellationToken cancellationToken)
        {
            await WriteTypeCodeAsync(BJsonValueTypeCode.Object, cancellationToken).ConfigureAwait(false);
            await WriteInt32Async(obj.Count, cancellationToken).ConfigureAwait(false);

            foreach (var pair in obj)
            {
                await WriteStringDataAsync(pair.Key, cancellationToken).ConfigureAwait(false);
                PushPropertyPathSegment(pair.Key);
                try
                {
                    await WriteValueAsync(pair.Value, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    PopPathSegment();
                }
            }
        }

        private void WriteString(string value)
        {
            WriteTypeCode(BJsonValueTypeCode.String);
            WriteStringData(value);
        }

        private async Task WriteStringAsync(string value, CancellationToken cancellationToken)
        {
            await WriteTypeCodeAsync(BJsonValueTypeCode.String, cancellationToken).ConfigureAwait(false);
            await WriteStringDataAsync(value, cancellationToken).ConfigureAwait(false);
        }

        private void WriteBinary(BJsonBinary value)
        {
            WriteTypeCode(BJsonValueTypeCode.Binary);
            WriteInt32(value.Count);
            _stream.Write(value.AsSpan());
            _bytesWritten += value.Count;
        }

        private async Task WriteBinaryAsync(BJsonBinary value, CancellationToken cancellationToken)
        {
            await WriteTypeCodeAsync(BJsonValueTypeCode.Binary, cancellationToken).ConfigureAwait(false);
            await WriteInt32Async(value.Count, cancellationToken).ConfigureAwait(false);
            await _stream.WriteAsync(value.AsMemory(), cancellationToken).ConfigureAwait(false);
            _bytesWritten += value.Count;
        }

        private void WriteStringData(string value)
        {
            byte[] bytes = Utf8.GetBytes(value);
            WriteInt32(bytes.Length);
            _stream.Write(bytes, 0, bytes.Length);
            _bytesWritten += bytes.Length;
        }

        private async Task WriteStringDataAsync(string value, CancellationToken cancellationToken)
        {
            byte[] bytes = Utf8.GetBytes(value);
            await WriteInt32Async(bytes.Length, cancellationToken).ConfigureAwait(false);
            await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
            _bytesWritten += bytes.Length;
        }

        private void WriteTypeCode(BJsonValueTypeCode code)
        {
            _stream.WriteByte((byte)code);
            _bytesWritten += 1;
        }

        private Task WriteTypeCodeAsync(BJsonValueTypeCode code, CancellationToken cancellationToken)
            => WriteByteAsync((byte)code, cancellationToken);

        private void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        private async Task WriteByteAsync(byte value, CancellationToken cancellationToken)
        {
            _singleByteBuffer[0] = value;
            await _stream.WriteAsync(_singleByteBuffer, 0, 1, cancellationToken).ConfigureAwait(false);
            _bytesWritten += 1;
        }

        private void WriteInt16(short value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
            _stream.Write(buffer);
            _bytesWritten += buffer.Length;
        }

        private async Task WriteInt16Async(short value, CancellationToken cancellationToken)
        {
            BinaryPrimitives.WriteInt16LittleEndian(_numericBuffer.AsSpan(0, sizeof(short)), value);
            await _stream.WriteAsync(_numericBuffer, 0, sizeof(short), cancellationToken).ConfigureAwait(false);
            _bytesWritten += sizeof(short);
        }

        private void WriteUInt16(ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            _stream.Write(buffer);
            _bytesWritten += buffer.Length;
        }

        private async Task WriteUInt16Async(ushort value, CancellationToken cancellationToken)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_numericBuffer.AsSpan(0, sizeof(ushort)), value);
            await _stream.WriteAsync(_numericBuffer, 0, sizeof(ushort), cancellationToken).ConfigureAwait(false);
            _bytesWritten += sizeof(ushort);
        }

        private void WriteInt32(int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            _stream.Write(buffer);
            _bytesWritten += buffer.Length;
        }

        private async Task WriteInt32Async(int value, CancellationToken cancellationToken)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_numericBuffer.AsSpan(0, sizeof(int)), value);
            await _stream.WriteAsync(_numericBuffer, 0, sizeof(int), cancellationToken).ConfigureAwait(false);
            _bytesWritten += sizeof(int);
        }

        private void WriteUInt32(uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            _stream.Write(buffer);
            _bytesWritten += buffer.Length;
        }

        private async Task WriteUInt32Async(uint value, CancellationToken cancellationToken)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_numericBuffer.AsSpan(0, sizeof(uint)), value);
            await _stream.WriteAsync(_numericBuffer, 0, sizeof(uint), cancellationToken).ConfigureAwait(false);
            _bytesWritten += sizeof(uint);
        }

        private void WriteInt64(long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            _stream.Write(buffer);
            _bytesWritten += buffer.Length;
        }

        private async Task WriteInt64Async(long value, CancellationToken cancellationToken)
        {
            BinaryPrimitives.WriteInt64LittleEndian(_numericBuffer.AsSpan(0, sizeof(long)), value);
            await _stream.WriteAsync(_numericBuffer, 0, sizeof(long), cancellationToken).ConfigureAwait(false);
            _bytesWritten += sizeof(long);
        }

        private void WriteUInt64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            _stream.Write(buffer);
            _bytesWritten += buffer.Length;
        }

        private async Task WriteUInt64Async(ulong value, CancellationToken cancellationToken)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_numericBuffer.AsSpan(0, sizeof(ulong)), value);
            await _stream.WriteAsync(_numericBuffer, 0, sizeof(ulong), cancellationToken).ConfigureAwait(false);
            _bytesWritten += sizeof(ulong);
        }

        private void WriteSingle(float value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, BitConverter.SingleToInt32Bits(value));
            _stream.Write(buffer);
            _bytesWritten += buffer.Length;
        }

        private async Task WriteSingleAsync(float value, CancellationToken cancellationToken)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_numericBuffer.AsSpan(0, sizeof(int)), BitConverter.SingleToInt32Bits(value));
            await _stream.WriteAsync(_numericBuffer, 0, sizeof(int), cancellationToken).ConfigureAwait(false);
            _bytesWritten += sizeof(int);
        }

        private void WriteDouble(double value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, BitConverter.DoubleToInt64Bits(value));
            _stream.Write(buffer);
            _bytesWritten += buffer.Length;
        }

        private async Task WriteDoubleAsync(double value, CancellationToken cancellationToken)
        {
            BinaryPrimitives.WriteInt64LittleEndian(_numericBuffer.AsSpan(0, sizeof(long)), BitConverter.DoubleToInt64Bits(value));
            await _stream.WriteAsync(_numericBuffer, 0, sizeof(long), cancellationToken).ConfigureAwait(false);
            _bytesWritten += sizeof(long);
        }

        private BJsonSerializationException CreateSerializationException(string message, string operation, Exception? innerException = null)
        {
            return new BJsonSerializationException(
                message,
                byteOffset: _bytesWritten,
                operation: operation,
                documentPath: CurrentPath,
                errorCode: BJsonErrorCode.BinarySerializationError,
                innerException: innerException);
        }

        private string CurrentPath
        {
            get
            {
                if (_pathDepth == 0)
                    return "$";

                var builder = new StringBuilder("$");
                for (int i = 0; i < _pathDepth; i++)
                {
                    var segment = _pathSegments[i];
                    if (segment.IsIndex)
                    {
                        builder.Append('[');
                        builder.Append(segment.Index);
                        builder.Append(']');
                    }
                    else
                    {
                        AppendPropertySegment(builder, segment.PropertyName!);
                    }
                }

                return builder.ToString();
            }
        }

        private void PushIndexPathSegment(int index)
        {
            EnsurePathCapacity(_pathDepth + 1);
            _pathSegments[_pathDepth++] = PathSegment.ForIndex(index);
        }

        private void PushPropertyPathSegment(string key)
        {
            EnsurePathCapacity(_pathDepth + 1);
            _pathSegments[_pathDepth++] = PathSegment.ForProperty(key);
        }

        private void PopPathSegment()
        {
            if (_pathDepth <= 0)
                return;

            _pathDepth--;
            _pathSegments[_pathDepth] = default;
        }

        private void EnsurePathCapacity(int requiredCapacity)
        {
            if (_pathSegments.Length >= requiredCapacity)
                return;

            int nextSize = _pathSegments.Length == 0 ? 8 : _pathSegments.Length * 2;
            while (nextSize < requiredCapacity)
                nextSize *= 2;

            Array.Resize(ref _pathSegments, nextSize);
        }

        private static void AppendPropertySegment(StringBuilder builder, string key)
        {
            if (IsSimpleIdentifier(key))
            {
                builder.Append('.');
                builder.Append(key);
                return;
            }

            builder.Append("['");
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (c == '\\' || c == '\'')
                    builder.Append('\\');
                builder.Append(c);
            }
            builder.Append("']");
        }

        private static bool IsSimpleIdentifier(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (!(char.IsLetter(key[0]) || key[0] == '_'))
                return false;

            for (int i = 1; i < key.Length; i++)
            {
                char c = key[i];
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                    return false;
            }

            return true;
        }

        private readonly struct PathSegment
        {
            private PathSegment(bool isIndex, int index, string? propertyName)
            {
                IsIndex = isIndex;
                Index = index;
                PropertyName = propertyName;
            }

            public bool IsIndex { get; }

            public int Index { get; }

            public string? PropertyName { get; }

            public static PathSegment ForIndex(int index) => new PathSegment(true, index, null);

            public static PathSegment ForProperty(string propertyName) => new PathSegment(false, 0, propertyName);
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
