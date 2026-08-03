#nullable enable

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Binary
{
    internal sealed class BJsonBinaryReaderCore : IDisposable
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly BJsonBinaryReaderOptions _options;
        private readonly byte[] _singleByteBuffer;
        private readonly byte[] _numericBuffer;
        private readonly System.Collections.Generic.List<string> _stringTable;
        private PathSegment[] _pathSegments;
        private int _pathDepth;
        private long _bytesRead;

        public BJsonBinaryReaderCore(Stream stream, bool leaveOpen = false, BJsonBinaryReaderOptions? options = null)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");
            if (!stream.CanRead)
                throw new BJsonValidationException("Stream must be readable.");

            _stream = stream;
            _leaveOpen = leaveOpen;
            _options = options ?? BJsonBinaryReaderOptions.Default;
            _singleByteBuffer = new byte[1];
            _numericBuffer = new byte[8];
            _stringTable = new System.Collections.Generic.List<string>();
            _pathSegments = Array.Empty<PathSegment>();
            _pathDepth = 0;
            _bytesRead = 0;
        }

        public BJsonValue Read()
        {
            try
            {
                return ReadRootValue();
            }
            catch (Exception ex) when (!(ex is BJsonException))
            {
                throw CreateFormatException("Failed to deserialize binary BinJson payload.", "Root", ex);
            }
        }

        public void Dispose()
        {
            if (!_leaveOpen)
                _stream.Dispose();
        }

        private BJsonValue ReadRootValue()
        {
            while (true)
            {
                byte typeCode = ReadByte();
                if (typeCode == (byte)BJsonValueTypeCode.HeaderMarker)
                {
                    ReadHeader();
                    continue;
                }
                if (typeCode == (byte)BJsonValueTypeCode.StringTable)
                {
                    ReadStringTable();
                    continue;
                }
                if (typeCode == (byte)BJsonValueTypeCode.ExtContainer)
                {
                    SkipExtContainer();
                    continue;
                }

                return ReadValueFromTypeCode(typeCode);
            }
        }

        private BJsonValue ReadValue()
        {
            return ReadValueFromTypeCode(ReadByte());
        }

        private BJsonValue ReadValueFromTypeCode(byte typeCode)
        {
            if (BJsonBinaryTypeRanges.IsPositiveFixInt(typeCode))
                return BJsonValue.Create((ulong)typeCode);

            if (BJsonBinaryTypeRanges.IsFixStr(typeCode))
                return BJsonValue.Create(ReadStringBytes(typeCode - BJsonBinaryTypeRanges.FixStrMin));

            if (BJsonBinaryTypeRanges.IsFixArray(typeCode))
                return BJsonValue.Create(ReadArray(typeCode & 0x0F));

            if (BJsonBinaryTypeRanges.IsFixObject(typeCode))
                return BJsonValue.Create(ReadObject(typeCode & 0x0F));

            switch ((BJsonValueTypeCode)typeCode)
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
                case BJsonValueTypeCode.String8:
                    return BJsonValue.Create(ReadStringData(ReadByte()));
                case BJsonValueTypeCode.String16:
                    return BJsonValue.Create(ReadStringData(ReadUInt16()));
                case BJsonValueTypeCode.String32:
                    return BJsonValue.Create(ReadStringData(ReadUInt32AsCount("String length")));
                case BJsonValueTypeCode.StringRef:
                    return ReadStringReference();
                case BJsonValueTypeCode.ArrayVar:
                    return BJsonValue.Create(ReadArray(ReadVarUIntAsCount("Array element count")));
                case BJsonValueTypeCode.ObjectVar:
                    return BJsonValue.Create(ReadObject(ReadVarUIntAsCount("Object pair count")));
                case BJsonValueTypeCode.PackedArray:
                    return BJsonValue.Create(ReadPackedArray());
                case BJsonValueTypeCode.Binary:
                    return BJsonValue.Create(ReadBinary());
                default:
                    throw CreateFormatException(
                        $"Invalid BJson type code: 0x{(byte)typeCode:X2}",
                        "TypeCode",
                        details: new System.Collections.Generic.Dictionary<string, object?>
                        {
                            ["typeCode"] = (byte)typeCode
                        });
            }
        }

        private BJsonArray ReadArray(int count)
        {
            var array = new BJsonArray(count);

            for (int i = 0; i < count; i++)
            {
                PushIndexPathSegment(i);
                try
                {
                    array.Add(ReadValue());
                }
                finally
                {
                    PopPathSegment();
                }
            }

            return array;
        }

        private BJsonObject ReadObject(int count)
        {
            var obj = new BJsonObject(count);

            for (int i = 0; i < count; i++)
            {
                string key = ReadObjectKey();
                if (obj.ContainsKey(key))
                    throw CreateFormatException(
                        $"Duplicate object key '{key}' is not allowed.",
                        "Object",
                        details: new System.Collections.Generic.Dictionary<string, object?>
                        {
                            ["key"] = key
                        });

                PushPropertyPathSegment(key);
                try
                {
                    obj.Add(key, ReadValue());
                }
                finally
                {
                    PopPathSegment();
                }
            }

            return obj;
        }

        private byte ReadByte()
        {
            int read = _stream.Read(_singleByteBuffer, 0, 1);
            if (read != 1)
                throw CreateFormatException("Unexpected end of stream while reading byte.", "ReadByte");
            _bytesRead += 1;
            return _singleByteBuffer[0];
        }

        private short ReadInt16()
        {
            FillBuffer(_numericBuffer, sizeof(short));
            return BinaryPrimitives.ReadInt16LittleEndian(_numericBuffer.AsSpan(0, sizeof(short)));
        }

        private ushort ReadUInt16()
        {
            FillBuffer(_numericBuffer, sizeof(ushort));
            return BinaryPrimitives.ReadUInt16LittleEndian(_numericBuffer.AsSpan(0, sizeof(ushort)));
        }

        private int ReadInt32()
        {
            FillBuffer(_numericBuffer, sizeof(int));
            return BinaryPrimitives.ReadInt32LittleEndian(_numericBuffer.AsSpan(0, sizeof(int)));
        }

        private uint ReadUInt32()
        {
            FillBuffer(_numericBuffer, sizeof(uint));
            return BinaryPrimitives.ReadUInt32LittleEndian(_numericBuffer.AsSpan(0, sizeof(uint)));
        }

        private long ReadInt64()
        {
            FillBuffer(_numericBuffer, sizeof(long));
            return BinaryPrimitives.ReadInt64LittleEndian(_numericBuffer.AsSpan(0, sizeof(long)));
        }

        private ulong ReadUInt64()
        {
            FillBuffer(_numericBuffer, sizeof(ulong));
            return BinaryPrimitives.ReadUInt64LittleEndian(_numericBuffer.AsSpan(0, sizeof(ulong)));
        }

        private float ReadSingle()
        {
            FillBuffer(_numericBuffer, sizeof(int));
            int bits = BinaryPrimitives.ReadInt32LittleEndian(_numericBuffer.AsSpan(0, sizeof(int)));
            return BitConverter.Int32BitsToSingle(bits);
        }

        private double ReadDouble()
        {
            FillBuffer(_numericBuffer, sizeof(long));
            long bits = BinaryPrimitives.ReadInt64LittleEndian(_numericBuffer.AsSpan(0, sizeof(long)));
            return BitConverter.Int64BitsToDouble(bits);
        }

        private ulong ReadVarUInt()
        {
            ulong value = 0;
            int shift = 0;

            for (int i = 0; i < 10; i++)
            {
                byte b = ReadByte();
                value |= (ulong)(b & 0x7F) << shift;

                if ((b & 0x80) == 0)
                    return value;

                shift += 7;
            }

            throw CreateFormatException("Invalid VarUInt encoding.", "VarUInt");
        }

        private int ReadVarUIntAsCount(string context)
        {
            ulong value = ReadVarUInt();
            if (value > int.MaxValue)
                throw CreateFormatException($"{context} exceeds Int32.MaxValue.", "VarUInt", details: new System.Collections.Generic.Dictionary<string, object?> { ["value"] = value });
            return (int)value;
        }

        private string ReadStringData(int len)
        {
            return ReadStringBytes(len);
        }

        private int ReadUInt32AsCount(string context)
        {
            uint value = ReadUInt32();
            if (value > int.MaxValue)
                throw CreateFormatException($"{context} exceeds Int32.MaxValue.", "UInt32", details: new System.Collections.Generic.Dictionary<string, object?> { ["value"] = value });
            return (int)value;
        }

        private string ReadStringBytes(int byteLength)
        {
            byte[] bytes = ReadBytes(byteLength);
            try
            {
                return Utf8.GetString(bytes);
            }
            catch (DecoderFallbackException ex)
            {
                throw CreateFormatException("Invalid UTF-8 string encoding.", "String", ex);
            }
        }

        private BJsonBinary ReadBinary()
        {
            int len = ReadVarUIntAsCount("Binary length");
            byte[] bytes = ReadBytes(len);
            return new BJsonBinary(bytes);
        }

        private string ReadObjectKey()
        {
            int len = ReadVarUIntAsCount("Object key length");
            if (len == 0)
                return string.Empty;

            string key = ReadStringBytes(len);
            return key;
        }

        private void ReadHeader()
        {
            byte magic0 = ReadByte();
            byte magic1 = ReadByte();
            byte version = ReadByte();
            byte flags = ReadByte();

            if (magic0 != (byte)'B' || magic1 != (byte)'J')
                throw CreateFormatException("Invalid binary header magic.", "Header");
            if (version != 0x01)
                throw CreateFormatException($"Unsupported binary version: {version}.", "Header", details: new System.Collections.Generic.Dictionary<string, object?> { ["version"] = version });
            if ((flags & 0xFC) != 0)
                throw CreateFormatException("Unsupported header flags.", "Header", details: new System.Collections.Generic.Dictionary<string, object?> { ["flags"] = flags });
        }

        private void ReadStringTable()
        {
            int count = ReadVarUIntAsCount("String table count");
            _stringTable.Clear();
            _stringTable.Capacity = Math.Max(_stringTable.Capacity, count);

            for (int i = 0; i < count; i++)
            {
                _stringTable.Add(ReadStringTableEntry());
            }
        }

        private string ReadStringTableEntry()
        {
            byte typeCode = ReadByte();
            if (BJsonBinaryTypeRanges.IsFixStr(typeCode))
                return ReadStringBytes(typeCode - BJsonBinaryTypeRanges.FixStrMin);

            switch ((BJsonValueTypeCode)typeCode)
            {
                case BJsonValueTypeCode.String8:
                    return ReadStringData(ReadByte());
                case BJsonValueTypeCode.String16:
                    return ReadStringData(ReadUInt16());
                case BJsonValueTypeCode.String32:
                    return ReadStringData(ReadUInt32AsCount("String table entry length"));
                default:
                    throw CreateFormatException(
                        "Invalid string table entry type code.",
                        "StringTable",
                        details: new System.Collections.Generic.Dictionary<string, object?>
                        {
                            ["typeCode"] = typeCode
                        });
            }
        }

        private BJsonValue ReadStringReference()
        {
            int index = ReadVarUIntAsCount("StringRef index");
            if ((uint)index < (uint)_stringTable.Count)
                return BJsonValue.Create(_stringTable[index]);

            if (_options.InvalidStringRefPolicy == BJsonInvalidStringRefPolicy.CoerceNull)
                return BJsonValue.Null;

            throw CreateFormatException(
                $"Invalid StringRef index {index}.",
                "StringRef",
                details: new System.Collections.Generic.Dictionary<string, object?>
                {
                    ["index"] = index,
                    ["stringTableCount"] = _stringTable.Count
                });
        }

        private void SkipExtContainer()
        {
            int len = ReadVarUIntAsCount("ExtContainer length");
            if (len == 0)
                return;

            byte[] skipped = ReadBytes(len);
            _ = skipped;
        }

        private BJsonArray ReadPackedArray()
        {
            byte elementType = ReadByte();
            int count = ReadVarUIntAsCount("Packed array count");
            var array = new BJsonArray(count);

            if (!IsSupportedPackedElementType(elementType))
                throw CreateFormatException(
                    $"Unsupported packed element type code: 0x{elementType:X2}.",
                    "PackedArray",
                    details: new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["elementType"] = elementType
                    });

            switch ((BJsonValueTypeCode)elementType)
            {
                case BJsonValueTypeCode.Null:
                    for (int i = 0; i < count; i++) array.Add(BJsonValue.Null);
                    break;
                case BJsonValueTypeCode.BoolFalse:
                case BJsonValueTypeCode.BoolTrue:
                    ReadPackedBools(array, count);
                    break;
                default:
                    for (int i = 0; i < count; i++)
                    {
                        PushIndexPathSegment(i);
                        try
                        {
                            array.Add(ReadValueFromTypeCode(elementType));
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    break;
            }

            return array;
        }

        private static bool IsSupportedPackedElementType(byte typeCode)
        {
            switch ((BJsonValueTypeCode)typeCode)
            {
                case BJsonValueTypeCode.Null:
                case BJsonValueTypeCode.BoolFalse:
                case BJsonValueTypeCode.BoolTrue:
                case BJsonValueTypeCode.Int8:
                case BJsonValueTypeCode.Int16:
                case BJsonValueTypeCode.Int32:
                case BJsonValueTypeCode.Int64:
                case BJsonValueTypeCode.UInt8:
                case BJsonValueTypeCode.UInt16:
                case BJsonValueTypeCode.UInt32:
                case BJsonValueTypeCode.UInt64:
                case BJsonValueTypeCode.Float32:
                case BJsonValueTypeCode.Float64:
                    return true;
                default:
                    return false;
            }
        }

        private void ReadPackedBools(BJsonArray array, int count)
        {
            int byteCount = (count + 7) / 8;
            byte[] packed = ReadBytes(byteCount);

            for (int i = 0; i < count; i++)
            {
                int byteIndex = i / 8;
                int bitIndex = i % 8;
                bool bit = (packed[byteIndex] & (1 << bitIndex)) != 0;
                array.Add(bit ? BJsonValue.True : BJsonValue.False);
            }
        }

        private byte[] ReadBytes(int length)
        {
            if (length < 0)
                throw CreateFormatException("Negative length is not allowed.", "ReadBytes");
            if (length == 0)
                return Array.Empty<byte>();

            byte[] buffer = new byte[length];
            FillBuffer(buffer, length);
            return buffer;
        }

        private void FillBuffer(byte[] buffer, int length)
        {
            int totalRead = 0;
            while (totalRead < length)
            {
                int bytesRead = _stream.Read(buffer, totalRead, length - totalRead);
                if (bytesRead == 0)
                    throw CreateFormatException(
                        "Unexpected end of stream.",
                        "ReadExactly",
                        details: new System.Collections.Generic.Dictionary<string, object?>
                        {
                            ["expectedBytes"] = length,
                            ["actualBytes"] = totalRead
                        });

                totalRead += bytesRead;
                _bytesRead += bytesRead;
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

        private BJsonBinaryFormatException CreateFormatException(
            string message,
            string? operation,
            Exception? innerException = null,
            System.Collections.Generic.IDictionary<string, object?>? details = null)
        {
            var map = details is null
                ? new System.Collections.Generic.Dictionary<string, object?>(StringComparer.Ordinal)
                : new System.Collections.Generic.Dictionary<string, object?>(details, StringComparer.Ordinal);

            map["byteOffset"] = _bytesRead;
            map["path"] = BuildCurrentPath();

            return new BJsonBinaryFormatException(
                message,
                byteOffset: _bytesRead,
                section: operation,
                documentPath: BuildCurrentPath(),
                errorCode: BJsonErrorCode.BinaryFormatError,
                innerException: innerException,
                details: map);
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
