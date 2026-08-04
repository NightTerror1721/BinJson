#nullable enable

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Krampus.BinJson.Error;
using Krampus.BinJson.Utilities;

namespace Krampus.BinJson.Binary
{
    internal sealed class BJsonBinaryReaderCore : IDisposable
    {
        private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly BufferReaderStream _reader;
        private readonly BJsonBinaryReaderOptions _options;
        private List<string>? _stringTable;
        private PathSegment[] _pathSegments;
        private int _pathDepth;

        public BJsonBinaryReaderCore(Stream stream, bool leaveOpen = false, BJsonBinaryReaderOptions? options = null)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");
            if (!stream.CanRead)
                throw new BJsonValidationException("Stream must be readable.");

            _reader = new BufferReaderStream(stream, leaveOpen);
            _options = options ?? BJsonBinaryReaderOptions.Default;
            _stringTable = null;
            _pathSegments = Array.Empty<PathSegment>();
            _pathDepth = 0;
        }

        public BJsonValue Read()
        {
            try
            {
                return ReadRootValue();
            }
            catch (EndOfStreamException ex)
            {
                var details = new Dictionary<string, object?>(StringComparer.Ordinal);
                if (ex.Data.Contains("expectedBytes"))
                    details["expectedBytes"] = ex.Data["expectedBytes"];
                if (ex.Data.Contains("actualBytes"))
                    details["actualBytes"] = ex.Data["actualBytes"];

                throw CreateFormatException("Unexpected end of stream.", "ReadExactly", ex, details.Count > 0 ? details : null);
            }
            catch (Exception ex) when (ex is not BJsonException)
            {
                throw CreateFormatException("Failed to deserialize binary BinJson payload.", "Root", ex);
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        private BJsonValue ReadRootValue()
        {
            while (true)
            {
                byte typeCode = _reader.ReadByte();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BJsonValue ReadValue()
        {
            return ReadValueFromTypeCode(_reader.ReadByte());
        }

        private BJsonValue ReadValueFromTypeCode(byte typeCode)
        {
            if (BJsonBinaryTypeRanges.IsPositiveFixInt(typeCode))
                return BJsonValue.Create((ulong)typeCode);

            if (BJsonBinaryTypeRanges.IsFixStr(typeCode))
                return BJsonValue.Create(ReadStringData(typeCode - BJsonBinaryTypeRanges.FixStrMin));

            if (BJsonBinaryTypeRanges.IsFixArray(typeCode))
                return BJsonValue.Create(ReadArray(typeCode & 0x0F));

            if (BJsonBinaryTypeRanges.IsFixObject(typeCode))
                return BJsonValue.Create(ReadObject(typeCode & 0x0F));

            return (BJsonValueTypeCode)typeCode switch
            {
                BJsonValueTypeCode.Null => BJsonValue.Null,
                BJsonValueTypeCode.Int8 => BJsonValue.Create(unchecked((sbyte)_reader.ReadByte())),
                BJsonValueTypeCode.Int16 => BJsonValue.Create(_reader.ReadInt16LE()),
                BJsonValueTypeCode.Int32 => BJsonValue.Create(_reader.ReadInt32LE()),
                BJsonValueTypeCode.Int64 => BJsonValue.Create(_reader.ReadInt64LE()),
                BJsonValueTypeCode.UInt8 => BJsonValue.Create(_reader.ReadByte()),
                BJsonValueTypeCode.UInt16 => BJsonValue.Create(_reader.ReadUInt16LE()),
                BJsonValueTypeCode.UInt32 => BJsonValue.Create(_reader.ReadUInt32LE()),
                BJsonValueTypeCode.UInt64 => BJsonValue.Create(_reader.ReadUInt64LE()),
                BJsonValueTypeCode.Float32 => BJsonValue.Create(_reader.ReadSingleLE()),
                BJsonValueTypeCode.Float64 => BJsonValue.Create(_reader.ReadDoubleLE()),
                BJsonValueTypeCode.BoolTrue => BJsonValue.True,
                BJsonValueTypeCode.BoolFalse => BJsonValue.False,
                BJsonValueTypeCode.String8 => BJsonValue.Create(ReadStringData(_reader.ReadByte())),
                BJsonValueTypeCode.String16 => BJsonValue.Create(ReadStringData(_reader.ReadUInt16LE())),
                BJsonValueTypeCode.String32 => BJsonValue.Create(ReadStringData(ReadUInt32AsCount("String length"))),
                BJsonValueTypeCode.StringRef => ReadStringReference(),
                BJsonValueTypeCode.ArrayVar => BJsonValue.Create(ReadArray(ReadVarUIntAsCount("Array element count"))),
                BJsonValueTypeCode.ObjectVar => BJsonValue.Create(ReadObject(ReadVarUIntAsCount("Object pair count"))),
                BJsonValueTypeCode.PackedArray => BJsonValue.Create(ReadPackedArray()),
                BJsonValueTypeCode.Binary => BJsonValue.Create(ReadBinary()),
                _ => throw CreateFormatException(
                                        $"Invalid BJson type code: 0x{typeCode:X2}",
                                        "TypeCode",
                                        details: new Dictionary<string, object?> { ["typeCode"] = typeCode }),
            };
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
                        details: new Dictionary<string, object?> { ["key"] = key });

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

        private int ReadVarUIntAsCount(string context)
        {
            if (!_reader.ReadVarUInt(out var value))
                throw CreateFormatException("Invalid VarUInt encoding.", "VarUInt", details: new Dictionary<string, object?> { ["context"] = context });
            if (value > int.MaxValue)
                throw CreateFormatException($"{context} exceeds Int32.MaxValue.", "VarUInt", details: new Dictionary<string, object?> { ["value"] = value });
            return (int)value;
        }

        private int ReadUInt32AsCount(string context)
        {
            uint value = _reader.ReadUInt32LE();
            if (value > int.MaxValue)
                throw CreateFormatException($"{context} exceeds Int32.MaxValue.", "UInt32", details: new Dictionary<string, object?> { ["value"] = value });
            return (int)value;
        }

        private string ReadStringData(int byteLength)
        {
            if (byteLength == 0)
                return string.Empty;

            try
            {
                using var rented = _reader.ReadRentedBuffer(byteLength);
                return Utf8.GetString(rented);
            }
            catch (DecoderFallbackException ex)
            {
                throw CreateFormatException("Invalid UTF-8 string encoding.", "String", ex);
            }
        }

        private BJsonBinary ReadBinary()
        {
            int len = ReadVarUIntAsCount("Binary length");
            if (len == 0)
                return new BJsonBinary(Array.Empty<byte>());

            using var rented = _reader.ReadRentedBuffer(len);
            return new BJsonBinary(rented.ToArray());
        }

        private string ReadObjectKey()
        {
            BJsonValue keyValue = ReadValue();
            if (keyValue.Type != BJsonValueType.String)
                throw CreateFormatException(
                    "Invalid object key encoding. Expected string value.",
                    "ObjectKey",
                    details: new Dictionary<string, object?> { ["actualType"] = keyValue.Type.ToString() });

            return keyValue.StringValue;
        }

        private void ReadHeader()
        {
            byte magic0 = _reader.ReadByte();
            byte magic1 = _reader.ReadByte();
            byte version = _reader.ReadByte();
            byte flags = _reader.ReadByte();

            if (magic0 != (byte)'B' || magic1 != (byte)'J')
                throw CreateFormatException("Invalid binary header magic.", "Header");
            if (version != 0x01)
                throw CreateFormatException($"Unsupported binary version: {version}.", "Header", details: new Dictionary<string, object?> { ["version"] = version });
            if ((flags & 0xFC) != 0)
                throw CreateFormatException("Unsupported header flags.", "Header", details: new Dictionary<string, object?> { ["flags"] = flags });
        }

        private void ReadStringTable()
        {
            int count = ReadVarUIntAsCount("String table count");
            if (count <= 0)
            {
                _stringTable?.Clear();
                _stringTable = null;
                return;
            }

            if (_stringTable is null)
                _stringTable = new List<string>(count);
            else
            {
                _stringTable.Clear();
                _stringTable.Capacity = Math.Max(_stringTable.Capacity, count);
            }

            for (int i = 0; i < count; i++)
                _stringTable.Add(ReadStringTableEntry());
        }

        private string ReadStringTableEntry()
        {
            byte typeCode = _reader.ReadByte();
            if (BJsonBinaryTypeRanges.IsFixStr(typeCode))
                return ReadStringData(typeCode - BJsonBinaryTypeRanges.FixStrMin);

            return (BJsonValueTypeCode)typeCode switch
            {
                BJsonValueTypeCode.String8 => ReadStringData(_reader.ReadByte()),
                BJsonValueTypeCode.String16 => ReadStringData(_reader.ReadUInt16LE()),
                BJsonValueTypeCode.String32 => ReadStringData(ReadUInt32AsCount("String table entry length")),
                _ => throw CreateFormatException(
                                        "Invalid string table entry type code.",
                                        "StringTable",
                                        details: new Dictionary<string, object?> { ["typeCode"] = typeCode }),
            };
        }

        private BJsonValue ReadStringReference()
        {
            int index = ReadVarUIntAsCount("StringRef index");
            if (_stringTable is not null && (uint)index < (uint)_stringTable.Count)
                return BJsonValue.Create(_stringTable[index]);

            if (_options.InvalidStringRefPolicy == BJsonInvalidStringRefPolicy.CoerceNull)
                return BJsonValue.Null;

            throw CreateFormatException(
                $"Invalid StringRef index {index}.",
                "StringRef",
                details: new Dictionary<string, object?>
                {
                    ["index"] = index,
                    ["stringTableCount"] = _stringTable?.Count ?? 0
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipExtContainer()
        {
            int len = ReadVarUIntAsCount("ExtContainer length");
            if (len > 0)
                _reader.Skip(len);
        }

        private BJsonArray ReadPackedArray()
        {
            byte elementType = _reader.ReadByte();
            int count = ReadVarUIntAsCount("Packed array count");
            var array = new BJsonArray(count);

            if (!IsSupportedPackedElementType(elementType))
                throw CreateFormatException(
                    $"Unsupported packed element type code: 0x{elementType:X2}.",
                    "PackedArray",
                    details: new Dictionary<string, object?> { ["elementType"] = elementType });

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
                {
                    if (!BJsonBinaryTypeSizes.IsFixedSize(elementType))
                    {
                        throw CreateFormatException(
                            $"Packed element type code 0x{elementType:X2} is not fixed-size.",
                            "PackedArray",
                            details: new Dictionary<string, object?> {
                                ["elementType"] = elementType
                            }
                        );
                    }

                    int elementSize = BJsonBinaryTypeSizes.GetSize(elementType);
                    using var rented = _reader.ReadRentedBuffer(count * elementSize);
                    var span = rented.Span;

                    switch ((BJsonValueTypeCode)elementType)
                    {
                        case BJsonValueTypeCode.Int8:
                            for (int i = 0; i < count; i++)
                                array.Add(BJsonValue.Create((sbyte)span[i]));
                            break;

                        case BJsonValueTypeCode.UInt8:
                            for (int i = 0; i < count; i++)
                                array.Add(BJsonValue.Create(span[i]));
                            break;

                        case BJsonValueTypeCode.Int16:
                        {
                            var typedSpan = MemoryMarshal.Cast<byte, short>(span);
                            if (BitConverter.IsLittleEndian)
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(typedSpan[i]));
                            }
                            else
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(BinaryPrimitives.ReverseEndianness(typedSpan[i])));
                            }
                            break;
                        }

                        case BJsonValueTypeCode.UInt16:
                        {
                            var typedSpan = MemoryMarshal.Cast<byte, ushort>(span);
                            if (BitConverter.IsLittleEndian)
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(typedSpan[i]));
                            }
                            else
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(BinaryPrimitives.ReverseEndianness(typedSpan[i])));
                            }
                            break;
                        }

                        case BJsonValueTypeCode.Int32:
                        {
                            var typedSpan = MemoryMarshal.Cast<byte, int>(span);
                            if (BitConverter.IsLittleEndian)
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(typedSpan[i]));
                            }
                            else
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(BinaryPrimitives.ReverseEndianness(typedSpan[i])));
                            }
                            break;
                        }

                        case BJsonValueTypeCode.UInt32:
                        {
                            var typedSpan = MemoryMarshal.Cast<byte, uint>(span);
                            if (BitConverter.IsLittleEndian)
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(typedSpan[i]));
                            }
                            else
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(BinaryPrimitives.ReverseEndianness(typedSpan[i])));
                            }
                            break;
                        }

                        case BJsonValueTypeCode.Int64:
                        {
                            var typedSpan = MemoryMarshal.Cast<byte, long>(span);
                            if (BitConverter.IsLittleEndian)
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(typedSpan[i]));
                            }
                            else
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(BinaryPrimitives.ReverseEndianness(typedSpan[i])));
                            }
                            break;
                        }

                        case BJsonValueTypeCode.UInt64:
                        {
                            var typedSpan = MemoryMarshal.Cast<byte, ulong>(span);
                            if (BitConverter.IsLittleEndian)
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(typedSpan[i]));
                            }
                            else
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(BinaryPrimitives.ReverseEndianness(typedSpan[i])));
                            }
                            break;
                        }

                        case BJsonValueTypeCode.Float32:
                        {
                            var typedSpan = MemoryMarshal.Cast<byte, uint>(span);
                            if (BitConverter.IsLittleEndian)
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(BitConverter.Int32BitsToSingle((int)typedSpan[i])));
                            }
                            else
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(BitConverter.Int32BitsToSingle((int)BinaryPrimitives.ReverseEndianness(typedSpan[i]))));
                            }
                            break;
                        }

                        case BJsonValueTypeCode.Float64:
                        {
                            var doubleSpan = MemoryMarshal.Cast<byte, ulong>(span);
                            if (BitConverter.IsLittleEndian)
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(BitConverter.Int64BitsToDouble((long)doubleSpan[i])));
                            }
                            else
                            {
                                for (int i = 0; i < count; i++)
                                    array.Add(BJsonValue.Create(BitConverter.Int64BitsToDouble((long)BinaryPrimitives.ReverseEndianness(doubleSpan[i]))));
                            }
                            break;
                        }
                    }

                    break;
                }
            }

            return array;
        }

        private static bool IsSupportedPackedElementType(byte typeCode)
        {
            return (BJsonValueTypeCode)typeCode switch
            {
                BJsonValueTypeCode.Null or
                BJsonValueTypeCode.BoolFalse or
                BJsonValueTypeCode.BoolTrue or
                BJsonValueTypeCode.Int8 or
                BJsonValueTypeCode.Int16 or
                BJsonValueTypeCode.Int32 or
                BJsonValueTypeCode.Int64 or
                BJsonValueTypeCode.UInt8 or
                BJsonValueTypeCode.UInt16 or
                BJsonValueTypeCode.UInt32 or
                BJsonValueTypeCode.UInt64 or
                BJsonValueTypeCode.Float32 or
                BJsonValueTypeCode.Float64 => true,
                _ => false,
            };
        }

        private void ReadPackedBools(BJsonArray array, int count)
        {
            int byteCount = (count + 7) / 8;
            using var packedRented = _reader.ReadRentedBuffer(byteCount);

            for (int i = 0; i < count; i++)
            {
                int byteIndex = i / 8;
                int bitIndex = i % 8;
                bool bit = (packedRented[byteIndex] & (1 << bitIndex)) != 0;
                array.Add(bit ? BJsonValue.True : BJsonValue.False);
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
            IDictionary<string, object?>? details = null)
        {
            var map = details is null
                ? new Dictionary<string, object?>(StringComparer.Ordinal)
                : new Dictionary<string, object?>(details, StringComparer.Ordinal);

            long currentOffset = _reader.BytesRead;
            map["byteOffset"] = currentOffset;
            map["path"] = BuildCurrentPath();

            return new BJsonBinaryFormatException(
                message,
                byteOffset: currentOffset,
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
