#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Binary
{
    internal sealed class BJsonBinaryWriterCore : IDisposable
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly BJsonBinaryWriterOptions _options;
        private readonly byte[] _singleByteBuffer;
        private readonly byte[] _numericBuffer;
        private readonly Dictionary<string, int> _stringTable;
        private readonly Dictionary<string, int> _stringFrequencies;
        private bool _stringTablePrepared;
        private PathSegment[] _pathSegments;
        private int _pathDepth;
        private long _bytesWritten;

        public BJsonBinaryWriterCore(Stream stream, bool leaveOpen = false, BJsonBinaryWriterOptions? options = null)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");
            if (!stream.CanWrite)
                throw new BJsonValidationException("Stream must be writable.");

            _stream = stream;
            _leaveOpen = leaveOpen;
            _options = options ?? BJsonBinaryWriterOptions.Default;
            _singleByteBuffer = new byte[1];
            _numericBuffer = new byte[8];
            _stringTable = new Dictionary<string, int>(StringComparer.Ordinal);
            _stringFrequencies = new Dictionary<string, int>(StringComparer.Ordinal);
            _stringTablePrepared = false;
            _pathSegments = Array.Empty<PathSegment>();
            _pathDepth = 0;
            _bytesWritten = 0;
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
                _stream.Flush();
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw CreateSerializationException("Failed to flush binary BinJson writer.", "Flush", ex);
            }
        }

        public void Dispose()
        {
            if (!_leaveOpen)
                _stream.Dispose();
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
                WriteByte((byte)rawValue);
                return;
            }

            if (signedValue < 0)
            {
                if (signedValue >= sbyte.MinValue && signedValue <= sbyte.MaxValue)
                {
                    WriteTypeCode(BJsonValueTypeCode.Int8);
                    WriteByte(unchecked((byte)(sbyte)signedValue));
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
            double d = value.DoubleValue;
            float f = (float)d;

            if (BitConverter.DoubleToInt64Bits(d) == BitConverter.DoubleToInt64Bits((double)f))
            {
                WriteTypeCode(BJsonValueTypeCode.Float32);
                WriteSingle(f);
            }
            else
            {
                WriteTypeCode(BJsonValueTypeCode.Float64);
                WriteDouble(d);
            }
        }

        private void WriteArray(BJsonArray array)
        {
            if (TryWritePackedArray(array))
                return;

            if (array.Count <= 15)
            {
                WriteByte((byte)(BJsonBinaryTypeRanges.FixArrayMin + array.Count));
            }
            else
            {
                WriteTypeCode(BJsonValueTypeCode.ArrayVar);
                WriteVarUInt((ulong)array.Count);
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
                WriteByte((byte)(BJsonBinaryTypeRanges.FixObjectMin + obj.Count));
            }
            else
            {
                WriteTypeCode(BJsonValueTypeCode.ObjectVar);
                WriteVarUInt((ulong)obj.Count);
            }

            foreach (var pair in obj)
            {
                WriteObjectKey(pair.Key);
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
            WriteByte((byte)plan.ElementTypeCode);
            WriteVarUInt((ulong)array.Count);
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
                case BJsonValueType.String:
                    if (_stringTable.Count > 0)
                    {
                        for (int i = 0; i < array.Count; i++)
                        {
                            if (!_stringTable.ContainsKey(array[i].StringValue))
                                return PackedPlan.For(BJsonValueTypeCode.String32);
                        }
                        return PackedPlan.For(BJsonValueTypeCode.StringRef);
                    }
                    return PackedPlan.For(BJsonValueTypeCode.String32);
                case BJsonValueType.Binary:
                    return PackedPlan.For(BJsonValueTypeCode.Binary);
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
                    for (int i = 0; i < array.Count; i++) WriteByte(unchecked((byte)(sbyte)unchecked((long)array[i].ULongValue)));
                    return;
                case BJsonValueTypeCode.Int16:
                    for (int i = 0; i < array.Count; i++) WriteInt16((short)unchecked((long)array[i].ULongValue));
                    return;
                case BJsonValueTypeCode.Int32:
                    for (int i = 0; i < array.Count; i++) WriteInt32((int)unchecked((long)array[i].ULongValue));
                    return;
                case BJsonValueTypeCode.Int64:
                    for (int i = 0; i < array.Count; i++) WriteInt64(unchecked((long)array[i].ULongValue));
                    return;
                case BJsonValueTypeCode.UInt8:
                    for (int i = 0; i < array.Count; i++) WriteByte((byte)array[i].ULongValue);
                    return;
                case BJsonValueTypeCode.UInt16:
                    for (int i = 0; i < array.Count; i++) WriteUInt16((ushort)array[i].ULongValue);
                    return;
                case BJsonValueTypeCode.UInt32:
                    for (int i = 0; i < array.Count; i++) WriteUInt32((uint)array[i].ULongValue);
                    return;
                case BJsonValueTypeCode.UInt64:
                    for (int i = 0; i < array.Count; i++) WriteUInt64(array[i].ULongValue);
                    return;
                case BJsonValueTypeCode.Float64:
                    for (int i = 0; i < array.Count; i++) WriteDouble(array[i].DoubleValue);
                    return;
                case BJsonValueTypeCode.StringRef:
                    for (int i = 0; i < array.Count; i++) WriteVarUInt((ulong)_stringTable[array[i].StringValue]);
                    return;
                case BJsonValueTypeCode.String32:
                    for (int i = 0; i < array.Count; i++) WriteStringRaw(array[i].StringValue);
                    return;
                case BJsonValueTypeCode.Binary:
                    for (int i = 0; i < array.Count; i++) WriteBinaryRaw(array[i].BinaryValue);
                    return;
                default:
                    throw CreateSerializationException("Unsupported packed element type.", "WritePackedPayload");
            }
        }

        private void WritePackedBools(BJsonArray array)
        {
            int byteCount = (array.Count + 7) / 8;
            byte[] data = new byte[byteCount];

            for (int i = 0; i < array.Count; i++)
            {
                if (array[i].BoolValue)
                {
                    int byteIndex = i / 8;
                    int bitIndex = i % 8;
                    data[byteIndex] |= (byte)(1 << bitIndex);
                }
            }

            _stream.Write(data, 0, data.Length);
            _bytesWritten += data.Length;
        }

        private void WriteStringRaw(string value)
        {
            byte[] bytes = Utf8.GetBytes(value);
            WriteVarUInt((ulong)bytes.Length);
            _stream.Write(bytes, 0, bytes.Length);
            _bytesWritten += bytes.Length;
        }

        private void WriteBinaryRaw(BJsonBinary value)
        {
            WriteVarUInt((ulong)value.Count);
            _stream.Write(value.AsSpan());
            _bytesWritten += value.Count;
        }

        private void WriteString(string value)
        {
            if (_stringTable.TryGetValue(value, out int index))
            {
                WriteTypeCode(BJsonValueTypeCode.StringRef);
                WriteVarUInt((ulong)index);
                return;
            }

            WriteStringData(value);
        }

        private void WriteBinary(BJsonBinary value)
        {
            WriteTypeCode(BJsonValueTypeCode.Binary);
            WriteVarUInt((ulong)value.Count);
            _stream.Write(value.AsSpan());
            _bytesWritten += value.Count;
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
            byte[] bytes = Utf8.GetBytes(value);
            if (bytes.Length <= 31)
            {
                WriteByte((byte)(BJsonBinaryTypeRanges.FixStrMin + bytes.Length));
            }
            else if (bytes.Length <= byte.MaxValue)
            {
                WriteTypeCode(BJsonValueTypeCode.String8);
                WriteVarUInt((ulong)bytes.Length);
            }
            else if (bytes.Length <= ushort.MaxValue)
            {
                WriteTypeCode(BJsonValueTypeCode.String16);
                WriteVarUInt((ulong)bytes.Length);
            }
            else
            {
                WriteTypeCode(BJsonValueTypeCode.String32);
                WriteVarUInt((ulong)bytes.Length);
            }
            _stream.Write(bytes, 0, bytes.Length);
            _bytesWritten += bytes.Length;
        }

        private void WriteObjectKey(string key)
        {
            byte[] bytes = Utf8.GetBytes(key);
            WriteVarUInt((ulong)bytes.Length);
            _stream.Write(bytes, 0, bytes.Length);
            _bytesWritten += bytes.Length;
        }

        private void WriteTypeCode(BJsonValueTypeCode code)
        {
            _stream.WriteByte((byte)code);
            _bytesWritten += 1;
        }

        private void WriteByte(byte value)
        {
            _singleByteBuffer[0] = value;
            _stream.Write(_singleByteBuffer, 0, 1);
            _bytesWritten += 1;
        }

        private void WriteVarUInt(ulong value)
        {
            while (value >= 0x80)
            {
                WriteByte((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }

            WriteByte((byte)value);
        }

        private void WriteHeader(bool hasStringTable, bool hasExtContainer)
        {
            WriteTypeCode(BJsonValueTypeCode.HeaderMarker);
            WriteByte((byte)'B');
            WriteByte((byte)'J');
            WriteByte(0x01);

            byte flags = 0;
            if (hasStringTable)
                flags |= 0x01;
            if (hasExtContainer)
                flags |= 0x02;
            WriteByte(flags);
        }

        private void WriteStringTable()
        {
            WriteTypeCode(BJsonValueTypeCode.StringTable);
            WriteVarUInt((ulong)_stringTable.Count);

            var ordered = new string[_stringTable.Count];
            foreach (var pair in _stringTable)
                ordered[pair.Value] = pair.Key;

            for (int i = 0; i < ordered.Length; i++)
            {
                WriteStringRaw(ordered[i]);
            }
        }

        private void WriteInt16(short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(_numericBuffer.AsSpan(0, sizeof(short)), value);
            _stream.Write(_numericBuffer, 0, sizeof(short));
            _bytesWritten += sizeof(short);
        }

        private void WriteUInt16(ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_numericBuffer.AsSpan(0, sizeof(ushort)), value);
            _stream.Write(_numericBuffer, 0, sizeof(ushort));
            _bytesWritten += sizeof(ushort);
        }

        private void WriteInt32(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_numericBuffer.AsSpan(0, sizeof(int)), value);
            _stream.Write(_numericBuffer, 0, sizeof(int));
            _bytesWritten += sizeof(int);
        }

        private void WriteUInt32(uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_numericBuffer.AsSpan(0, sizeof(uint)), value);
            _stream.Write(_numericBuffer, 0, sizeof(uint));
            _bytesWritten += sizeof(uint);
        }

        private void WriteInt64(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(_numericBuffer.AsSpan(0, sizeof(long)), value);
            _stream.Write(_numericBuffer, 0, sizeof(long));
            _bytesWritten += sizeof(long);
        }

        private void WriteUInt64(ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_numericBuffer.AsSpan(0, sizeof(ulong)), value);
            _stream.Write(_numericBuffer, 0, sizeof(ulong));
            _bytesWritten += sizeof(ulong);
        }

        private void WriteSingle(float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value);
            BinaryPrimitives.WriteInt32LittleEndian(_numericBuffer.AsSpan(0, sizeof(int)), bits);
            _stream.Write(_numericBuffer, 0, sizeof(int));
            _bytesWritten += sizeof(int);
        }

        private void WriteDouble(double value)
        {
            long bits = BitConverter.DoubleToInt64Bits(value);
            BinaryPrimitives.WriteInt64LittleEndian(_numericBuffer.AsSpan(0, sizeof(long)), bits);
            _stream.Write(_numericBuffer, 0, sizeof(long));
            _bytesWritten += sizeof(long);
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

            map["byteOffset"] = _bytesWritten;
            map["path"] = BuildCurrentPath();

            return new BJsonSerializationException(
                message,
                byteOffset: _bytesWritten,
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
