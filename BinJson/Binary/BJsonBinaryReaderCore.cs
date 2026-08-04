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

        public BJsonBinaryReaderCore(ReadOnlyMemory<byte> data, BJsonBinaryReaderOptions? options = null)
        {
            _reader = new BufferReaderStream(data);
            _options = options ?? BJsonBinaryReaderOptions.Default;
            _stringTable = null;
            _pathSegments = Array.Empty<PathSegment>();
            _pathDepth = 0;
        }

        internal long BytesRead => _reader.BytesRead;

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

        public void Visit(BJsonBinaryVisitor visitor)
        {
            if (visitor is null)
                throw new BJsonValidationException("Parameter 'visitor' cannot be null.");

            try
            {
                visitor.OnDocumentStart();
                VisitRootValue(visitor);
                visitor.OnDocumentEnd();
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
                throw CreateFormatException("Failed to visit binary BinJson payload.", "Root", ex);
            }
        }

        public bool TryReadRootObjectProperty(string propertyName, out BJsonValue value)
        {
            if (propertyName is null)
                throw new BJsonValidationException("Parameter 'propertyName' cannot be null.");

            try
            {
                return TryReadRootObjectPropertyCore(propertyName, out value);
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
                throw CreateFormatException("Failed to read binary BinJson root object property.", "RootObjectProperty", ex);
            }
        }

        public BJsonObject ReadRootObjectProperties(IReadOnlyList<string> propertyNames)
        {
            if (propertyNames is null)
                throw new BJsonValidationException("Parameter 'propertyNames' cannot be null.");

            try
            {
                return ReadRootObjectPropertiesCore(propertyNames);
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
                throw CreateFormatException("Failed to read selected binary BinJson root object properties.", "RootObjectProperties", ex);
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

        private void VisitRootValue(BJsonBinaryVisitor visitor)
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

                VisitValueFromTypeCode(visitor, typeCode);
                return;
            }
        }

        private bool TryReadRootObjectPropertyCore(string propertyName, out BJsonValue value)
        {
            int count = ReadRootObjectCountOrThrow();
            return TryReadObjectProperty(count, propertyName, out value);
        }

        private BJsonObject ReadRootObjectPropertiesCore(IReadOnlyList<string> propertyNames)
        {
            int count = ReadRootObjectCountOrThrow();

            if (propertyNames.Count == 0)
            {
                SkipObject(count);
                return new BJsonObject();
            }

            using var matcher = PooledPropertyMatcher.Create(propertyNames, Utf8);
            return ReadSelectedObjectProperties(count, matcher);
        }

        private int ReadRootObjectCountOrThrow()
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

                if (BJsonBinaryTypeRanges.IsFixObject(typeCode))
                    return typeCode & 0x0F;

                if ((BJsonValueTypeCode)typeCode == BJsonValueTypeCode.ObjectVar)
                    return ReadVarUIntAsCount("Object pair count");

                throw CreateFormatException(
                    "Root value is not an object.",
                    "RootObjectProperty",
                    details: new Dictionary<string, object?> { ["actualType"] = GetValueTypeName(typeCode) });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BJsonValue ReadValue()
        {
            return ReadValueFromTypeCode(_reader.ReadByte());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void VisitValue(BJsonBinaryVisitor visitor)
        {
            VisitValueFromTypeCode(visitor, _reader.ReadByte());
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

        private void VisitValueFromTypeCode(BJsonBinaryVisitor visitor, byte typeCode)
        {
            if (BJsonBinaryTypeRanges.IsPositiveFixInt(typeCode))
            {
                visitor.OnUnsignedInteger(typeCode);
                return;
            }

            if (BJsonBinaryTypeRanges.IsFixStr(typeCode))
            {
                visitor.OnString(ReadStringData(typeCode - BJsonBinaryTypeRanges.FixStrMin));
                return;
            }

            if (BJsonBinaryTypeRanges.IsFixArray(typeCode))
            {
                VisitArray(visitor, typeCode & 0x0F, isPacked: false);
                return;
            }

            if (BJsonBinaryTypeRanges.IsFixObject(typeCode))
            {
                VisitObject(visitor, typeCode & 0x0F);
                return;
            }

            switch ((BJsonValueTypeCode)typeCode)
            {
                case BJsonValueTypeCode.Null:
                    visitor.OnNull();
                    return;
                case BJsonValueTypeCode.Int8:
                    visitor.OnSignedInteger(unchecked((sbyte)_reader.ReadByte()));
                    return;
                case BJsonValueTypeCode.Int16:
                    visitor.OnSignedInteger(_reader.ReadInt16LE());
                    return;
                case BJsonValueTypeCode.Int32:
                    visitor.OnSignedInteger(_reader.ReadInt32LE());
                    return;
                case BJsonValueTypeCode.Int64:
                    visitor.OnSignedInteger(_reader.ReadInt64LE());
                    return;
                case BJsonValueTypeCode.UInt8:
                    visitor.OnUnsignedInteger(_reader.ReadByte());
                    return;
                case BJsonValueTypeCode.UInt16:
                    visitor.OnUnsignedInteger(_reader.ReadUInt16LE());
                    return;
                case BJsonValueTypeCode.UInt32:
                    visitor.OnUnsignedInteger(_reader.ReadUInt32LE());
                    return;
                case BJsonValueTypeCode.UInt64:
                    visitor.OnUnsignedInteger(_reader.ReadUInt64LE());
                    return;
                case BJsonValueTypeCode.Float32:
                    visitor.OnFloat(_reader.ReadSingleLE());
                    return;
                case BJsonValueTypeCode.Float64:
                    visitor.OnFloat(_reader.ReadDoubleLE());
                    return;
                case BJsonValueTypeCode.BoolTrue:
                    visitor.OnBoolean(true);
                    return;
                case BJsonValueTypeCode.BoolFalse:
                    visitor.OnBoolean(false);
                    return;
                case BJsonValueTypeCode.String8:
                    visitor.OnString(ReadStringData(_reader.ReadByte()));
                    return;
                case BJsonValueTypeCode.String16:
                    visitor.OnString(ReadStringData(_reader.ReadUInt16LE()));
                    return;
                case BJsonValueTypeCode.String32:
                    visitor.OnString(ReadStringData(ReadUInt32AsCount("String length")));
                    return;
                case BJsonValueTypeCode.StringRef:
                {
                    string? stringRef = ReadStringReferenceValue();
                    if (stringRef is null)
                        visitor.OnNull();
                    else
                        visitor.OnString(stringRef);
                    return;
                }
                case BJsonValueTypeCode.ArrayVar:
                    VisitArray(visitor, ReadVarUIntAsCount("Array element count"), isPacked: false);
                    return;
                case BJsonValueTypeCode.ObjectVar:
                    VisitObject(visitor, ReadVarUIntAsCount("Object pair count"));
                    return;
                case BJsonValueTypeCode.PackedArray:
                    VisitPackedArray(visitor);
                    return;
                case BJsonValueTypeCode.Binary:
                    VisitBinary(visitor);
                    return;
                default:
                    throw CreateFormatException(
                        $"Invalid BJson type code: 0x{typeCode:X2}",
                        "TypeCode",
                        details: new Dictionary<string, object?> { ["typeCode"] = typeCode });
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

        private void SkipArray(int count)
        {
            for (int i = 0; i < count; i++)
            {
                PushIndexPathSegment(i);
                try
                {
                    SkipValue();
                }
                finally
                {
                    PopPathSegment();
                }
            }
        }

        private void VisitArray(BJsonBinaryVisitor visitor, int count, bool isPacked)
        {
            visitor.OnArrayStart(count, isPacked);

            for (int i = 0; i < count; i++)
            {
                PushIndexPathSegment(i);
                try
                {
                    VisitValue(visitor);
                }
                finally
                {
                    PopPathSegment();
                }
            }

            visitor.OnArrayEnd(count, isPacked);
        }

        private BJsonObject ReadObject(int count)
        {
            var obj = new BJsonObject(count);

            for (int i = 0; i < count; i++)
            {
                string key = ReadObjectKeyString();
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

        private bool TryReadObjectProperty(int count, string propertyName, out BJsonValue value)
        {
            using var matcher = PooledPropertyMatcher.Create(propertyName, Utf8);

            value = BJsonValue.Null;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                int matchIndex = ReadObjectKeyMatchIndex(matcher);
                if (!found && matchIndex == 0)
                {
                    value = ReadValue();
                    found = true;
                }
                else
                {
                    SkipValue();
                }
            }

            return found;
        }

        private BJsonObject ReadSelectedObjectProperties(int count, PooledPropertyMatcher matcher)
        {
            var result = new BJsonObject(Math.Min(count, matcher.Count));
            if (matcher.Count == 0)
            {
                SkipObject(count);
                return result;
            }

            int matchesFound = 0;
            bool[] foundFlags = ArrayPool<bool>.Shared.Rent(matcher.Count);
            Array.Clear(foundFlags, 0, matcher.Count);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    int matchIndex = ReadObjectKeyMatchIndex(matcher);
                    if (matchIndex >= 0 && !foundFlags[matchIndex])
                    {
                        result.Add(matcher.GetName(matchIndex), ReadValue());
                        foundFlags[matchIndex] = true;
                        matchesFound++;
                        if (matchesFound == matcher.Count)
                        {
                            // All requested keys were found. We still need to consume the remaining payload.
                            for (int j = i + 1; j < count; j++)
                            {
                                _ = ReadObjectKeyMatchIndex(matcher);
                                SkipValue();
                            }

                            break;
                        }
                    }
                    else
                    {
                        SkipValue();
                    }
                }
            }
            finally
            {
                ArrayPool<bool>.Shared.Return(foundFlags, clearArray: true);
            }

            return result;
        }

        private void SkipObject(int count)
        {
            HashSet<string>? seenKeys = count > 1 ? new HashSet<string>(StringComparer.Ordinal) : null;
            for (int i = 0; i < count; i++)
            {
                string key = ReadObjectKeyString();
                if (seenKeys is not null && !seenKeys.Add(key))
                {
                    throw CreateFormatException(
                        $"Duplicate object key '{key}' is not allowed.",
                        "Object",
                        details: new Dictionary<string, object?> { ["key"] = key });
                }

                PushPropertyPathSegment(key);
                try
                {
                    SkipValue();
                }
                finally
                {
                    PopPathSegment();
                }
            }
        }

        private void VisitObject(BJsonBinaryVisitor visitor, int count)
        {
            visitor.OnObjectStart(count);

            HashSet<string>? seenKeys = count > 1 ? new HashSet<string>(StringComparer.Ordinal) : null;
            for (int i = 0; i < count; i++)
            {
                string key = ReadObjectKeyString();
                if (seenKeys is not null && !seenKeys.Add(key))
                {
                    throw CreateFormatException(
                        $"Duplicate object key '{key}' is not allowed.",
                        "Object",
                        details: new Dictionary<string, object?> { ["key"] = key });
                }

                visitor.OnObjectProperty(key, i);
                PushPropertyPathSegment(key);
                try
                {
                    VisitValue(visitor);
                }
                finally
                {
                    PopPathSegment();
                }
            }

            visitor.OnObjectEnd(count);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipValue()
        {
            SkipValueFromTypeCode(_reader.ReadByte());
        }

        private void SkipValueFromTypeCode(byte typeCode)
        {
            if (BJsonBinaryTypeRanges.IsPositiveFixInt(typeCode))
                return;

            if (BJsonBinaryTypeRanges.IsFixStr(typeCode))
            {
                _reader.Skip(typeCode - BJsonBinaryTypeRanges.FixStrMin);
                return;
            }

            if (BJsonBinaryTypeRanges.IsFixArray(typeCode))
            {
                SkipArray(typeCode & 0x0F);
                return;
            }

            if (BJsonBinaryTypeRanges.IsFixObject(typeCode))
            {
                SkipObject(typeCode & 0x0F);
                return;
            }

            switch ((BJsonValueTypeCode)typeCode)
            {
                case BJsonValueTypeCode.Null:
                case BJsonValueTypeCode.BoolFalse:
                case BJsonValueTypeCode.BoolTrue:
                    return;
                case BJsonValueTypeCode.Int8:
                case BJsonValueTypeCode.UInt8:
                    _reader.Skip(1);
                    return;
                case BJsonValueTypeCode.Int16:
                case BJsonValueTypeCode.UInt16:
                    _reader.Skip(2);
                    return;
                case BJsonValueTypeCode.Int32:
                case BJsonValueTypeCode.UInt32:
                case BJsonValueTypeCode.Float32:
                    _reader.Skip(4);
                    return;
                case BJsonValueTypeCode.Int64:
                case BJsonValueTypeCode.UInt64:
                case BJsonValueTypeCode.Float64:
                    _reader.Skip(8);
                    return;
                case BJsonValueTypeCode.String8:
                    _reader.Skip(_reader.ReadByte());
                    return;
                case BJsonValueTypeCode.String16:
                    _reader.Skip(_reader.ReadUInt16LE());
                    return;
                case BJsonValueTypeCode.String32:
                    _reader.Skip(ReadUInt32AsCount("String length"));
                    return;
                case BJsonValueTypeCode.StringRef:
                    _ = ReadStringReferenceValue();
                    return;
                case BJsonValueTypeCode.ArrayVar:
                    SkipArray(ReadVarUIntAsCount("Array element count"));
                    return;
                case BJsonValueTypeCode.ObjectVar:
                    SkipObject(ReadVarUIntAsCount("Object pair count"));
                    return;
                case BJsonValueTypeCode.PackedArray:
                    SkipPackedArray();
                    return;
                case BJsonValueTypeCode.Binary:
                    _reader.Skip(ReadVarUIntAsCount("Binary length"));
                    return;
                default:
                    throw CreateFormatException(
                        $"Invalid BJson type code: 0x{typeCode:X2}",
                        "TypeCode",
                        details: new Dictionary<string, object?> { ["typeCode"] = typeCode });
            }
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

        private void VisitBinary(BJsonBinaryVisitor visitor)
        {
            int len = ReadVarUIntAsCount("Binary length");
            if (len == 0)
            {
                visitor.OnBinary(ReadOnlySpan<byte>.Empty);
                return;
            }

            using var rented = _reader.ReadRentedBuffer(len);
            visitor.OnBinary(rented.Span);
        }

        private void SkipPackedArray()
        {
            byte elementType = _reader.ReadByte();
            int count = ReadVarUIntAsCount("Packed array count");

            if (!IsSupportedPackedElementType(elementType))
            {
                throw CreateFormatException(
                    $"Unsupported packed element type code: 0x{elementType:X2}.",
                    "PackedArray",
                    details: new Dictionary<string, object?> { ["elementType"] = elementType });
            }

            switch ((BJsonValueTypeCode)elementType)
            {
                case BJsonValueTypeCode.Null:
                    return;
                case BJsonValueTypeCode.BoolFalse:
                case BJsonValueTypeCode.BoolTrue:
                    _reader.Skip((count + 7) / 8);
                    return;
                default:
                    if (!BJsonBinaryTypeSizes.IsFixedSize(elementType))
                    {
                        throw CreateFormatException(
                            $"Packed element type code 0x{elementType:X2} is not fixed-size.",
                            "PackedArray",
                            details: new Dictionary<string, object?> { ["elementType"] = elementType });
                    }

                    _reader.Skip(count * BJsonBinaryTypeSizes.GetSize(elementType));
                    return;
            }
        }

        private BJsonBinary ReadBinary()
        {
            int len = ReadVarUIntAsCount("Binary length");
            if (len == 0)
                return BJsonBinary.CreateUnsafe(Array.Empty<byte>());

            using var rented = _reader.ReadRentedBuffer(len);
            byte[] data = new byte[len];
            rented.CopyTo(data);
            return BJsonBinary.CreateUnsafe(data);
        }

        private string ReadObjectKeyString()
        {
            byte typeCode = _reader.ReadByte();
            string? key = ReadStringValueFromTypeCode(typeCode);
            if (key is not null)
                return key;

            throw CreateFormatException(
                "Invalid object key encoding. Expected string value.",
                "ObjectKey",
                details: new Dictionary<string, object?> { ["actualType"] = GetValueTypeName(typeCode) });
        }

        private int ReadObjectKeyMatchIndex(PooledPropertyMatcher matcher)
        {
            byte typeCode = _reader.ReadByte();

            if (BJsonBinaryTypeRanges.IsFixStr(typeCode))
                return ReadUtf8KeyMatchIndex(typeCode - BJsonBinaryTypeRanges.FixStrMin, matcher);

            return (BJsonValueTypeCode)typeCode switch
            {
                BJsonValueTypeCode.String8 => ReadUtf8KeyMatchIndex(_reader.ReadByte(), matcher),
                BJsonValueTypeCode.String16 => ReadUtf8KeyMatchIndex(_reader.ReadUInt16LE(), matcher),
                BJsonValueTypeCode.String32 => ReadUtf8KeyMatchIndex(ReadUInt32AsCount("String length"), matcher),
                BJsonValueTypeCode.StringRef => ReadStringRefKeyMatchIndex(matcher),
                _ => throw CreateFormatException(
                    "Invalid object key encoding. Expected string value.",
                    "ObjectKey",
                    details: new Dictionary<string, object?> { ["actualType"] = GetValueTypeName(typeCode) }),
            };
        }

        private int ReadUtf8KeyMatchIndex(int byteLength, PooledPropertyMatcher matcher)
        {
            if (byteLength == 0)
                return matcher.TryMatchUtf8(ReadOnlySpan<byte>.Empty, out int emptyIndex) ? emptyIndex : -1;

            using var rented = _reader.ReadRentedBuffer(byteLength);
            return matcher.TryMatchUtf8(rented.Span, out int index) ? index : -1;
        }

        private int ReadStringRefKeyMatchIndex(PooledPropertyMatcher matcher)
        {
            string? value = ReadStringReferenceValue();
            if (value is null)
            {
                throw CreateFormatException(
                    "Invalid object key encoding. Expected string value.",
                    "ObjectKey",
                    details: new Dictionary<string, object?> { ["actualType"] = BJsonValueType.Null.ToString() });
            }

            return matcher.TryMatchString(value, out int index) ? index : -1;
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
            string? stringValue = ReadStringReferenceValue();
            if (stringValue is null)
                return BJsonValue.Null;

            return BJsonValue.Create(stringValue);
        }

        private string? ReadStringReferenceValue()
        {
            int index = ReadVarUIntAsCount("StringRef index");
            if (_stringTable is not null && (uint)index < (uint)_stringTable.Count)
                return _stringTable[index];

            if (_options.InvalidStringRefPolicy == BJsonInvalidStringRefPolicy.CoerceNull)
                return null;

            throw CreateFormatException(
                $"Invalid StringRef index {index}.",
                "StringRef",
                details: new Dictionary<string, object?>
                {
                    ["index"] = index,
                    ["stringTableCount"] = _stringTable?.Count ?? 0
                });
        }

        private string? ReadStringValueFromTypeCode(byte typeCode)
        {
            if (BJsonBinaryTypeRanges.IsFixStr(typeCode))
                return ReadStringData(typeCode - BJsonBinaryTypeRanges.FixStrMin);

            return (BJsonValueTypeCode)typeCode switch
            {
                BJsonValueTypeCode.String8 => ReadStringData(_reader.ReadByte()),
                BJsonValueTypeCode.String16 => ReadStringData(_reader.ReadUInt16LE()),
                BJsonValueTypeCode.String32 => ReadStringData(ReadUInt32AsCount("String length")),
                BJsonValueTypeCode.StringRef => ReadStringReferenceValue(),
                _ => null,
            };
        }

        private sealed class PooledPropertyMatcher : IDisposable
        {
            private readonly string[] _names;
            private readonly byte[][] _utf8Buffers;
            private readonly int[] _utf8Lengths;
            private readonly Dictionary<string, int> _nameToIndex;

            private PooledPropertyMatcher(string[] names, byte[][] utf8Buffers, int[] utf8Lengths, Dictionary<string, int> nameToIndex)
            {
                _names = names;
                _utf8Buffers = utf8Buffers;
                _utf8Lengths = utf8Lengths;
                _nameToIndex = nameToIndex;
            }

            public int Count => _names.Length;

            public string GetName(int index) => _names[index];

            public bool TryMatchUtf8(ReadOnlySpan<byte> keyUtf8, out int index)
            {
                for (int i = 0; i < _utf8Buffers.Length; i++)
                {
                    if (_utf8Lengths[i] != keyUtf8.Length)
                        continue;

                    if (keyUtf8.SequenceEqual(_utf8Buffers[i].AsSpan(0, _utf8Lengths[i])))
                    {
                        index = i;
                        return true;
                    }
                }

                index = -1;
                return false;
            }

            public bool TryMatchString(string key, out int index)
            {
                return _nameToIndex.TryGetValue(key, out index);
            }

            public void Dispose()
            {
                for (int i = 0; i < _utf8Buffers.Length; i++)
                {
                    byte[]? buffer = _utf8Buffers[i];
                    if (buffer is not null)
                        ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            public static PooledPropertyMatcher Create(string propertyName, UTF8Encoding utf8)
            {
                if (propertyName is null)
                    throw new BJsonValidationException("Property name cannot be null.");

                int maxByteCount = utf8.GetMaxByteCount(propertyName.Length);
                byte[] rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
                int length = utf8.GetBytes(propertyName.AsSpan(), rented);

                var map = new Dictionary<string, int>(1, StringComparer.Ordinal)
                {
                    [propertyName] = 0
                };

                return new PooledPropertyMatcher(
                    names: new[] { propertyName },
                    utf8Buffers: new[] { rented },
                    utf8Lengths: new[] { length },
                    nameToIndex: map);
            }

            public static PooledPropertyMatcher Create(IReadOnlyList<string> propertyNames, UTF8Encoding utf8)
            {
                if (propertyNames is null)
                    throw new BJsonValidationException("Property names cannot be null.");

                var names = new List<string>(propertyNames.Count);
                var map = new Dictionary<string, int>(propertyNames.Count, StringComparer.Ordinal);

                for (int i = 0; i < propertyNames.Count; i++)
                {
                    string? name = propertyNames[i];
                    if (name is null)
                        throw new BJsonValidationException("Property names cannot contain null values.");

                    if (map.ContainsKey(name))
                        continue;

                    int index = names.Count;
                    map[name] = index;
                    names.Add(name);
                }

                string[] nameArray = names.ToArray();
                byte[][] buffers = new byte[nameArray.Length][];
                int[] lengths = new int[nameArray.Length];

                for (int i = 0; i < nameArray.Length; i++)
                {
                    int maxByteCount = utf8.GetMaxByteCount(nameArray[i].Length);
                    byte[] rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
                    int len = utf8.GetBytes(nameArray[i].AsSpan(), rented);
                    buffers[i] = rented;
                    lengths[i] = len;
                }

                return new PooledPropertyMatcher(nameArray, buffers, lengths, map);
            }
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

        private void VisitPackedArray(BJsonBinaryVisitor visitor)
        {
            byte elementType = _reader.ReadByte();
            int count = ReadVarUIntAsCount("Packed array count");

            if (!IsSupportedPackedElementType(elementType))
            {
                throw CreateFormatException(
                    $"Unsupported packed element type code: 0x{elementType:X2}.",
                    "PackedArray",
                    details: new Dictionary<string, object?> { ["elementType"] = elementType });
            }

            visitor.OnArrayStart(count, isPacked: true);

            try
            {
                switch ((BJsonValueTypeCode)elementType)
                {
                    case BJsonValueTypeCode.Null:
                        for (int i = 0; i < count; i++)
                        {
                            PushIndexPathSegment(i);
                            try
                            {
                                visitor.OnNull();
                            }
                            finally
                            {
                                PopPathSegment();
                            }
                        }
                        return;

                    case BJsonValueTypeCode.BoolFalse:
                    case BJsonValueTypeCode.BoolTrue:
                        VisitPackedBools(visitor, count);
                        return;

                    default:
                    {
                        if (!BJsonBinaryTypeSizes.IsFixedSize(elementType))
                        {
                            throw CreateFormatException(
                                $"Packed element type code 0x{elementType:X2} is not fixed-size.",
                                "PackedArray",
                                details: new Dictionary<string, object?> { ["elementType"] = elementType });
                        }

                        int elementSize = BJsonBinaryTypeSizes.GetSize(elementType);
                        using var rented = _reader.ReadRentedBuffer(count * elementSize);
                        VisitPackedScalarElements(visitor, elementType, count, rented.Span);
                        return;
                    }
                }
            }
            finally
            {
                visitor.OnArrayEnd(count, isPacked: true);
            }
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

        private void VisitPackedBools(BJsonBinaryVisitor visitor, int count)
        {
            int byteCount = (count + 7) / 8;
            using var packedRented = _reader.ReadRentedBuffer(byteCount);

            for (int i = 0; i < count; i++)
            {
                PushIndexPathSegment(i);
                try
                {
                    int byteIndex = i / 8;
                    int bitIndex = i % 8;
                    bool bit = (packedRented[byteIndex] & (1 << bitIndex)) != 0;
                    visitor.OnBoolean(bit);
                }
                finally
                {
                    PopPathSegment();
                }
            }
        }

        private void VisitPackedScalarElements(BJsonBinaryVisitor visitor, byte elementType, int count, Span<byte> span)
        {
            switch ((BJsonValueTypeCode)elementType)
            {
                case BJsonValueTypeCode.Int8:
                    for (int i = 0; i < count; i++)
                    {
                        PushIndexPathSegment(i);
                        try
                        {
                            visitor.OnSignedInteger((sbyte)span[i]);
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    return;

                case BJsonValueTypeCode.UInt8:
                    for (int i = 0; i < count; i++)
                    {
                        PushIndexPathSegment(i);
                        try
                        {
                            visitor.OnUnsignedInteger(span[i]);
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    return;

                case BJsonValueTypeCode.Int16:
                {
                    var typedSpan = MemoryMarshal.Cast<byte, short>(span);
                    for (int i = 0; i < count; i++)
                    {
                        short value = BitConverter.IsLittleEndian ? typedSpan[i] : BinaryPrimitives.ReverseEndianness(typedSpan[i]);
                        PushIndexPathSegment(i);
                        try
                        {
                            visitor.OnSignedInteger(value);
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    return;
                }

                case BJsonValueTypeCode.UInt16:
                {
                    var typedSpan = MemoryMarshal.Cast<byte, ushort>(span);
                    for (int i = 0; i < count; i++)
                    {
                        ushort value = BitConverter.IsLittleEndian ? typedSpan[i] : BinaryPrimitives.ReverseEndianness(typedSpan[i]);
                        PushIndexPathSegment(i);
                        try
                        {
                            visitor.OnUnsignedInteger(value);
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    return;
                }

                case BJsonValueTypeCode.Int32:
                {
                    var typedSpan = MemoryMarshal.Cast<byte, int>(span);
                    for (int i = 0; i < count; i++)
                    {
                        int value = BitConverter.IsLittleEndian ? typedSpan[i] : BinaryPrimitives.ReverseEndianness(typedSpan[i]);
                        PushIndexPathSegment(i);
                        try
                        {
                            visitor.OnSignedInteger(value);
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    return;
                }

                case BJsonValueTypeCode.UInt32:
                {
                    var typedSpan = MemoryMarshal.Cast<byte, uint>(span);
                    for (int i = 0; i < count; i++)
                    {
                        uint value = BitConverter.IsLittleEndian ? typedSpan[i] : BinaryPrimitives.ReverseEndianness(typedSpan[i]);
                        PushIndexPathSegment(i);
                        try
                        {
                            visitor.OnUnsignedInteger(value);
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    return;
                }

                case BJsonValueTypeCode.Int64:
                {
                    var typedSpan = MemoryMarshal.Cast<byte, long>(span);
                    for (int i = 0; i < count; i++)
                    {
                        long value = BitConverter.IsLittleEndian ? typedSpan[i] : BinaryPrimitives.ReverseEndianness(typedSpan[i]);
                        PushIndexPathSegment(i);
                        try
                        {
                            visitor.OnSignedInteger(value);
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    return;
                }

                case BJsonValueTypeCode.UInt64:
                {
                    var typedSpan = MemoryMarshal.Cast<byte, ulong>(span);
                    for (int i = 0; i < count; i++)
                    {
                        ulong value = BitConverter.IsLittleEndian ? typedSpan[i] : BinaryPrimitives.ReverseEndianness(typedSpan[i]);
                        PushIndexPathSegment(i);
                        try
                        {
                            visitor.OnUnsignedInteger(value);
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    return;
                }

                case BJsonValueTypeCode.Float32:
                {
                    var typedSpan = MemoryMarshal.Cast<byte, uint>(span);
                    for (int i = 0; i < count; i++)
                    {
                        uint value = BitConverter.IsLittleEndian ? typedSpan[i] : BinaryPrimitives.ReverseEndianness(typedSpan[i]);
                        PushIndexPathSegment(i);
                        try
                        {
                            visitor.OnFloat(BitConverter.Int32BitsToSingle((int)value));
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    return;
                }

                case BJsonValueTypeCode.Float64:
                {
                    var typedSpan = MemoryMarshal.Cast<byte, ulong>(span);
                    for (int i = 0; i < count; i++)
                    {
                        ulong value = BitConverter.IsLittleEndian ? typedSpan[i] : BinaryPrimitives.ReverseEndianness(typedSpan[i]);
                        PushIndexPathSegment(i);
                        try
                        {
                            visitor.OnFloat(BitConverter.Int64BitsToDouble((long)value));
                        }
                        finally
                        {
                            PopPathSegment();
                        }
                    }
                    return;
                }

                default:
                    throw CreateFormatException(
                        $"Unsupported packed element type code: 0x{elementType:X2}.",
                        "PackedArray",
                        details: new Dictionary<string, object?> { ["elementType"] = elementType });
            }
        }

        private static string GetValueTypeName(byte typeCode)
        {
            if (BJsonBinaryTypeRanges.IsPositiveFixInt(typeCode))
                return BJsonValueType.Integer.ToString();
            if (BJsonBinaryTypeRanges.IsFixStr(typeCode))
                return BJsonValueType.String.ToString();
            if (BJsonBinaryTypeRanges.IsFixArray(typeCode))
                return BJsonValueType.Array.ToString();
            if (BJsonBinaryTypeRanges.IsFixObject(typeCode))
                return BJsonValueType.Object.ToString();

            return (BJsonValueTypeCode)typeCode switch
            {
                BJsonValueTypeCode.Null => BJsonValueType.Null.ToString(),
                BJsonValueTypeCode.Int8 or BJsonValueTypeCode.Int16 or BJsonValueTypeCode.Int32 or BJsonValueTypeCode.Int64 or
                BJsonValueTypeCode.UInt8 or BJsonValueTypeCode.UInt16 or BJsonValueTypeCode.UInt32 or BJsonValueTypeCode.UInt64 => BJsonValueType.Integer.ToString(),
                BJsonValueTypeCode.Float32 or BJsonValueTypeCode.Float64 => BJsonValueType.Float.ToString(),
                BJsonValueTypeCode.BoolFalse or BJsonValueTypeCode.BoolTrue => BJsonValueType.Boolean.ToString(),
                BJsonValueTypeCode.String8 or BJsonValueTypeCode.String16 or BJsonValueTypeCode.String32 or BJsonValueTypeCode.StringRef => BJsonValueType.String.ToString(),
                BJsonValueTypeCode.ArrayVar or BJsonValueTypeCode.PackedArray => BJsonValueType.Array.ToString(),
                BJsonValueTypeCode.ObjectVar => BJsonValueType.Object.ToString(),
                BJsonValueTypeCode.Binary => BJsonValueType.Binary.ToString(),
                _ => "Unknown"
            };
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
