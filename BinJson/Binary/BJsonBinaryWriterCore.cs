#nullable enable

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Krampus.BinJson.Error;
using Krampus.BinJson.Utilities;

namespace Krampus.BinJson.Binary
{
    internal sealed class BJsonBinaryWriterCore : IDisposable
    {
        private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly BufferWriterStream _writer;
        private readonly BJsonBinaryWriterOptions _options;

        private readonly Dictionary<string, int> _stringTable;
        private readonly Dictionary<string, int> _stringFrequencies;
        private bool _stringTablePrepared;

        private PathSegment[] _pathSegments;
        private int _pathDepth;

        public BJsonBinaryWriterCore(Stream stream, bool leaveOpen = false, BJsonBinaryWriterOptions? options = null, int bufferSize = 8192)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");
            if (!stream.CanWrite)
                throw new BJsonValidationException("Stream must be writable.");

            _writer = new BufferWriterStream(stream, leaveOpen);
            _options = options ?? BJsonBinaryWriterOptions.Default;

            _stringTable = new Dictionary<string, int>(StringComparer.Ordinal);
            _stringFrequencies = new Dictionary<string, int>(StringComparer.Ordinal);
            _stringTablePrepared = false;
            _pathSegments = Array.Empty<PathSegment>();
            _pathDepth = 0;
        }

        public void Write(BJsonValue value)
        {
            try
            {
                PrepareStringTable(value);
                WriteValue(value);
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw CreateSerializationException("Failed to serialize BinJson value to binary format.", "WriteValue", ex);
            }
        }

        public void Flush()
        {
            try
            {
                _writer.Flush();
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw CreateSerializationException("Failed to flush binary BinJson writer.", "Flush", ex);
            }
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        private void PrepareStringTable(BJsonValue root)
        {
            if (_stringTablePrepared)
                return;

            _stringTablePrepared = true;
            _stringTable.Clear();
            _stringFrequencies.Clear();

            if (!_options.EnableStringTable)
                return;

            CollectStringFrequencies(root);
            foreach (var pair in _stringFrequencies)
            {
                int utf8Length = Utf8.GetByteCount(pair.Key);
                int frequency = pair.Value;

                if (frequency * (1 + utf8Length) > (1 + utf8Length) + (frequency * 2))
                {
                    _stringTable[pair.Key] = _stringTable.Count;
                }
            }

            if (_stringTable.Count > 0)
            {
                WriteHeader(hasStringTable: true, hasExtContainer: false);
                WriteStringTable();
            }
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

        private void WriteInteger(BJsonValue value)
        {
            ulong rawValue = value.ULongValue;
            long signedValue = unchecked((long)rawValue);

            if (rawValue <= BJsonBinaryTypeRanges.PositiveFixIntMax)
            {
                _writer.WriteByte((byte)rawValue);
                return;
            }

            if (signedValue < 0)
            {
                if (signedValue >= sbyte.MinValue && signedValue <= sbyte.MaxValue)
                {
                    WriteTypeCode(BJsonValueTypeCode.Int8);
                    _writer.WriteByte(unchecked((byte)(sbyte)signedValue));
                    return;
                }
                if (signedValue >= short.MinValue && signedValue <= short.MaxValue)
                {
                    WriteTypeCode(BJsonValueTypeCode.Int16);
                    _writer.WriteInt16LE((short)signedValue);
                    return;
                }
                if (signedValue >= int.MinValue && signedValue <= int.MaxValue)
                {
                    WriteTypeCode(BJsonValueTypeCode.Int32);
                    _writer.WriteInt32LE((int)signedValue);
                    return;
                }

                WriteTypeCode(BJsonValueTypeCode.Int64);
                _writer.WriteInt64LE(signedValue);
                return;
            }

            if (rawValue <= byte.MaxValue)
            {
                WriteTypeCode(BJsonValueTypeCode.UInt8);
                _writer.WriteByte((byte)rawValue);
                return;
            }
            if (rawValue <= ushort.MaxValue)
            {
                WriteTypeCode(BJsonValueTypeCode.UInt16);
                _writer.WriteUInt16LE((ushort)rawValue);
                return;
            }
            if (rawValue <= uint.MaxValue)
            {
                WriteTypeCode(BJsonValueTypeCode.UInt32);
                _writer.WriteUInt32LE((uint)rawValue);
                return;
            }

            WriteTypeCode(BJsonValueTypeCode.UInt64);
            _writer.WriteUInt64LE(rawValue);
        }

        private void WriteFloat(BJsonValue value)
        {
            double d = value.DoubleValue;
            float f = (float)d;

            if (BitConverter.DoubleToInt64Bits(d) == BitConverter.DoubleToInt64Bits((double)f))
            {
                WriteTypeCode(BJsonValueTypeCode.Float32);
                _writer.WriteSingleLE(f);
            }
            else
            {
                WriteTypeCode(BJsonValueTypeCode.Float64);
                _writer.WriteDoubleLE(d);
            }
        }

        private void WriteArray(BJsonArray array)
        {
            if (TryWritePackedArray(array))
                return;

            if (array.Count <= 15)
            {
                _writer.WriteByte((byte)(BJsonBinaryTypeRanges.FixArrayMin + array.Count));
            }
            else
            {
                WriteTypeCode(BJsonValueTypeCode.ArrayVar);
                _writer.WriteVarUInt((ulong)array.Count);
            }

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

        private void WriteObject(BJsonObject obj)
        {
            if (obj.Count <= 15)
            {
                _writer.WriteByte((byte)(BJsonBinaryTypeRanges.FixObjectMin + obj.Count));
            }
            else
            {
                WriteTypeCode(BJsonValueTypeCode.ObjectVar);
                _writer.WriteVarUInt((ulong)obj.Count);
            }

            foreach (var pair in obj)
            {
                WriteString(pair.Key);
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

        private bool TryWritePackedArray(BJsonArray array)
        {
            if (!_options.EnablePackedArrays || array.Count < 3)
                return false;

            var plan = BuildPackedPlan(array);
            if (!plan.CanPack)
                return false;

            WriteTypeCode(BJsonValueTypeCode.PackedArray);
            _writer.WriteByte((byte)plan.ElementTypeCode);
            _writer.WriteVarUInt((ulong)array.Count);
            WritePackedPayload(array, plan);
            return true;
        }

        private PackedPlan BuildPackedPlan(BJsonArray array)
        {
            if (array.Count == 0)
                return PackedPlan.None;

            BJsonValueType firstType = array[0].Type;
            for (int i = 1; i < array.Count; i++)
            {
                if (array[i].Type != firstType)
                    return PackedPlan.None;
            }

            switch (firstType)
            {
                case BJsonValueType.Null:
                    return PackedPlan.For(BJsonValueTypeCode.Null);
                case BJsonValueType.Boolean:
                    return PackedPlan.For(BJsonValueTypeCode.BoolTrue);
                case BJsonValueType.Integer:
                    return BuildIntegerPackedPlan(array);
                case BJsonValueType.Float:
                    return PackedPlan.For(BJsonValueTypeCode.Float64);
                default:
                    return PackedPlan.None;
            }
        }

        private PackedPlan BuildIntegerPackedPlan(BJsonArray array)
        {
            long min = long.MaxValue;
            long max = long.MinValue;
            ulong maxUnsigned = 0;

            for (int i = 0; i < array.Count; i++)
            {
                ulong raw = array[i].ULongValue;
                long signed = unchecked((long)raw);
                if (signed < 0)
                {
                    max = Math.Max(max, signed);
                    min = Math.Min(min, signed);
                }
                else
                {
                    maxUnsigned = Math.Max(maxUnsigned, raw);
                    min = Math.Min(min, signed);
                    max = Math.Max(max, signed);
                }
            }

            if (min < 0)
            {
                if (min >= sbyte.MinValue && max <= sbyte.MaxValue) return PackedPlan.For(BJsonValueTypeCode.Int8);
                if (min >= short.MinValue && max <= short.MaxValue) return PackedPlan.For(BJsonValueTypeCode.Int16);
                if (min >= int.MinValue && max <= int.MaxValue) return PackedPlan.For(BJsonValueTypeCode.Int32);
                return PackedPlan.For(BJsonValueTypeCode.Int64);
            }

            if (maxUnsigned <= byte.MaxValue) return PackedPlan.For(BJsonValueTypeCode.UInt8);
            if (maxUnsigned <= ushort.MaxValue) return PackedPlan.For(BJsonValueTypeCode.UInt16);
            if (maxUnsigned <= uint.MaxValue) return PackedPlan.For(BJsonValueTypeCode.UInt32);
            return PackedPlan.For(BJsonValueTypeCode.UInt64);
        }

        private void WritePackedPayload(BJsonArray array, PackedPlan plan)
        {
            switch (plan.ElementTypeCode)
            {
                case BJsonValueTypeCode.Null:
                    return;
                case BJsonValueTypeCode.BoolTrue:
                    WritePackedBools(array);
                    return;
                case BJsonValueTypeCode.Int8:
                    for (int i = 0; i < array.Count; i++) _writer.WriteByte(unchecked((byte)(sbyte)unchecked((long)array[i].ULongValue)));
                    return;
                case BJsonValueTypeCode.Int16:
                    for (int i = 0; i < array.Count; i++) _writer.WriteInt16LE((short)unchecked((long)array[i].ULongValue));
                    return;
                case BJsonValueTypeCode.Int32:
                    for (int i = 0; i < array.Count; i++) _writer.WriteInt32LE((int)unchecked((long)array[i].ULongValue));
                    return;
                case BJsonValueTypeCode.Int64:
                    for (int i = 0; i < array.Count; i++) _writer.WriteInt64LE(unchecked((long)array[i].ULongValue));
                    return;
                case BJsonValueTypeCode.UInt8:
                    for (int i = 0; i < array.Count; i++) _writer.WriteByte((byte)array[i].ULongValue);
                    return;
                case BJsonValueTypeCode.UInt16:
                    for (int i = 0; i < array.Count; i++) _writer.WriteUInt16LE((ushort)array[i].ULongValue);
                    return;
                case BJsonValueTypeCode.UInt32:
                    for (int i = 0; i < array.Count; i++) _writer.WriteUInt32LE((uint)array[i].ULongValue);
                    return;
                case BJsonValueTypeCode.UInt64:
                    for (int i = 0; i < array.Count; i++) _writer.WriteUInt64LE(array[i].ULongValue);
                    return;
                case BJsonValueTypeCode.Float32:
                    for (int i = 0; i < array.Count; i++) _writer.WriteSingleLE((float)array[i].DoubleValue);
                    return;
                case BJsonValueTypeCode.Float64:
                    for (int i = 0; i < array.Count; i++) _writer.WriteDoubleLE(array[i].DoubleValue);
                    return;
                default:
                    throw CreateSerializationException("Unsupported packed element type.", "WritePackedPayload");
            }
        }

        private void WritePackedBools(BJsonArray array)
        {
            int byteCount = (array.Count + 7) / 8;
            byte[] data = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                Array.Clear(data, 0, byteCount);
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i].BoolValue)
                    {
                        int byteIndex = i / 8;
                        int bitIndex = i % 8;
                        data[byteIndex] |= (byte)(1 << bitIndex);
                    }
                }
                _writer.Write(data.AsSpan(0, byteCount));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(data);
            }
        }

        private void WriteString(string value)
        {
            if (_stringTable.TryGetValue(value, out int index))
            {
                WriteTypeCode(BJsonValueTypeCode.StringRef);
                _writer.WriteVarUInt((ulong)index);
                return;
            }

            WriteStringData(value);
        }

        private void WriteBinary(BJsonBinary value)
        {
            WriteTypeCode(BJsonValueTypeCode.Binary);
            _writer.WriteVarUInt((ulong)value.Count);
            _writer.Write(value.AsSpan());
        }

        private void CollectStringFrequencies(BJsonValue value)
        {
            switch (value.Type)
            {
                case BJsonValueType.String:
                    CountString(value.StringValue);
                    break;
                case BJsonValueType.Array:
                    for (int i = 0; i < value.ArrayValue.Count; i++)
                        CollectStringFrequencies(value.ArrayValue[i]);
                    break;
                case BJsonValueType.Object:
                    foreach (var pair in value.ObjectValue)
                    {
                        CountString(pair.Key);
                        CollectStringFrequencies(pair.Value);
                    }
                    break;
            }
        }

        private void CountString(string value)
        {
            if (_stringFrequencies.TryGetValue(value, out int freq))
                _stringFrequencies[value] = freq + 1;
            else
                _stringFrequencies[value] = 1;
        }

        private void WriteStringData(string value)
        {
            int maxByteCount = Utf8.GetMaxByteCount(value.Length);

            if (_writer.BufferSize >= maxByteCount + 5)
            {
                int actualUtf8Bytes = Utf8.GetBytes(value.AsSpan(), _writer.GetSpan(maxByteCount, 5));
                int headerSize = CalculateStringHeaderSize(actualUtf8Bytes);

                if (headerSize < 5)
                    _writer.MoveTo(5, headerSize, actualUtf8Bytes);

                WriteStringHeaderToSpan(_writer.GetSpan(headerSize), actualUtf8Bytes);
                _writer.Advance(headerSize + actualUtf8Bytes);
            }
            else
            {
                byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
                try
                {
                    int actualUtf8Bytes = Utf8.GetBytes(value.AsSpan(), tempBuffer);

                    if (actualUtf8Bytes <= 31)
                    {
                        _writer.WriteByte((byte)(BJsonBinaryTypeRanges.FixStrMin + actualUtf8Bytes));
                    }
                    else if (actualUtf8Bytes <= byte.MaxValue)
                    {
                        WriteTypeCode(BJsonValueTypeCode.String8);
                        _writer.WriteByte((byte)actualUtf8Bytes);
                    }
                    else if (actualUtf8Bytes <= ushort.MaxValue)
                    {
                        WriteTypeCode(BJsonValueTypeCode.String16);
                        _writer.WriteUInt16LE((ushort)actualUtf8Bytes);
                    }
                    else
                    {
                        WriteTypeCode(BJsonValueTypeCode.String32);
                        _writer.WriteUInt32LE((uint)actualUtf8Bytes);
                    }

                    _writer.Write(tempBuffer.AsSpan(0, actualUtf8Bytes));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(tempBuffer);
                }
            }
        }

        private static int CalculateStringHeaderSize(int byteLength)
        {
            if (byteLength <= 31) return 1;
            if (byteLength <= byte.MaxValue) return 2;
            if (byteLength <= ushort.MaxValue) return 3;
            return 5;
        }

        private static void WriteStringHeaderToSpan(Span<byte> destination, int byteLength)
        {
            if (byteLength <= 31)
            {
                destination[0] = (byte)(BJsonBinaryTypeRanges.FixStrMin + byteLength);
            }
            else if (byteLength <= byte.MaxValue)
            {
                destination[0] = (byte)BJsonValueTypeCode.String8;
                destination[1] = (byte)byteLength;
            }
            else if (byteLength <= ushort.MaxValue)
            {
                destination[0] = (byte)BJsonValueTypeCode.String16;
                BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], (ushort)byteLength);
            }
            else
            {
                destination[0] = (byte)BJsonValueTypeCode.String32;
                BinaryPrimitives.WriteUInt32LittleEndian(destination[1..], (uint)byteLength);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteTypeCode(BJsonValueTypeCode code)
        {
            _writer.WriteByte((byte)code);
        }

        private void WriteHeader(bool hasStringTable, bool hasExtContainer)
        {
            WriteTypeCode(BJsonValueTypeCode.HeaderMarker);
            _writer.WriteByte((byte)'B');
            _writer.WriteByte((byte)'J');
            _writer.WriteByte(0x01);

            byte flags = 0;
            if (hasStringTable)
                flags |= 0x01;
            if (hasExtContainer)
                flags |= 0x02;
            _writer.WriteByte(flags);
        }

        private void WriteStringTable()
        {
            WriteTypeCode(BJsonValueTypeCode.StringTable);
            _writer.WriteVarUInt((ulong)_stringTable.Count);

            var ordered = new string[_stringTable.Count];
            foreach (var pair in _stringTable)
                ordered[pair.Value] = pair.Key;

            for (int i = 0; i < ordered.Length; i++)
            {
                WriteStringData(ordered[i]);
            }
        }

        private void PushIndexPathSegment(int index)
        {
            EnsurePathCapacity(_pathDepth + 1);
            _pathSegments[_pathDepth++] = new PathSegment(index);
        }

        private void PushPropertyPathSegment(string propertyName)
        {
            EnsurePathCapacity(_pathDepth + 1);
            _pathSegments[_pathDepth++] = new PathSegment(propertyName);
        }

        private void PopPathSegment()
        {
            if (_pathDepth > 0)
                _pathDepth--;
        }

        private string BuildCurrentPath()
        {
            if (_pathDepth == 0)
                return "$";

            var builder = new StringBuilder("$");
            for (int i = 0; i < _pathDepth; i++)
            {
                PathSegment segment = _pathSegments[i];
                if (segment.Kind == PathSegmentKind.Index)
                {
                    builder.Append('[');
                    builder.Append(segment.Index);
                    builder.Append(']');
                }
                else
                {
                    builder.Append('.');
                    builder.Append(segment.PropertyName);
                }
            }

            return builder.ToString();
        }

        private void EnsurePathCapacity(int required)
        {
            if (_pathSegments.Length >= required)
                return;

            int newSize = _pathSegments.Length == 0 ? 8 : _pathSegments.Length * 2;
            if (newSize < required)
                newSize = required;

            Array.Resize(ref _pathSegments, newSize);
        }

        private BJsonSerializationException CreateSerializationException(
            string message,
            string? operation,
            Exception? innerException = null,
            IDictionary<string, object?>? details = null)
        {
            var map = details is null
                ? new Dictionary<string, object?>(StringComparer.Ordinal)
                : new Dictionary<string, object?>(details, StringComparer.Ordinal);

            long totalWrittenBytes = _writer.BytesWritten + _writer.BufferPos;
            map["byteOffset"] = totalWrittenBytes;
            map["path"] = BuildCurrentPath();

            return new BJsonSerializationException(
                message,
                byteOffset: totalWrittenBytes,
                operation: operation,
                documentPath: BuildCurrentPath(),
                innerException: innerException,
                errorCode: BJsonErrorCode.BinarySerializationError,
                details: map);
        }

        private readonly struct PackedPlan
        {
            public static readonly PackedPlan None = new PackedPlan(false, BJsonValueTypeCode.Null);

            private PackedPlan(bool canPack, BJsonValueTypeCode elementTypeCode)
            {
                CanPack = canPack;
                ElementTypeCode = elementTypeCode;
            }

            public bool CanPack { get; }
            public BJsonValueTypeCode ElementTypeCode { get; }

            public static PackedPlan For(BJsonValueTypeCode code) => new PackedPlan(true, code);
        }

        private readonly struct PathSegment
        {
            public PathSegment(int index)
            {
                Kind = PathSegmentKind.Index;
                Index = index;
                PropertyName = string.Empty;
            }

            public PathSegment(string propertyName)
            {
                Kind = PathSegmentKind.Property;
                Index = -1;
                PropertyName = propertyName;
            }

            public PathSegmentKind Kind { get; }
            public int Index { get; }
            public string PropertyName { get; }
        }

        private enum PathSegmentKind
        {
            Index,
            Property,
        }
    }
}
