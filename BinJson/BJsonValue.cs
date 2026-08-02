#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Krampus.BinJson
{
    /// <summary>
    /// A discriminated union value representing any JSON-like type.
    /// This is a readonly struct with a compact memory layout optimized for value semantics.
    /// </summary>
    /// <remarks>
    /// <para><b>Memory Layout:</b></para>
    /// <para>Total size: 16 bytes on 64-bit platforms (8 bytes _data + 8 bytes _reference pointer).</para>
    /// <para>- Null, Integer, Float, Boolean: stored inline using _data field and special tag objects.</para>
    /// <para>- String, Array, Object, Binary: stored as managed references in _reference field.</para>
    /// <para></para>
    /// <para><b>Performance Considerations:</b></para>
    /// <para>- Value types (null/bool/int/float) have zero GC pressure (inline storage).</para>
    /// <para>- Reference types (string/array/object/binary) allocate on heap as expected.</para>
    /// <para>- Large DOM structures can benefit from object pooling if GC pressure becomes an issue.</para>
    /// <para>- Structural equality for arrays/objects walks entire nested structure; cache results when needed.</para>
    /// </remarks>
    public readonly struct BJsonValue : IEquatable<BJsonValue>, IComparable<BJsonValue>
    {
        // Markers used to distinguish special non-reference states and booleans/doubles
        // _reference == null will now represent Null
        // SpecialIntTag marks integer values stored in _data
        // SpecialBoolTag marks boolean values; _data==0 => false, _data!=0 => true
        private static readonly object SpecialIntTag = new();
        private static readonly object SpecialBoolTag = new();
        private static readonly object SpecialDoubleTag = new();

        // Tags for reference types stored in _data when _reference holds the real object
        private const ulong TagString = 1;
        private const ulong TagArray = 2;
        private const ulong TagObject = 3;
        private const ulong TagBinary = 4;

        // Compact storage: 8 bytes of payload + 8 bytes for a reference = 16 bytes total
        private readonly ulong _data;
        private readonly object? _reference;

        public static readonly BJsonValue Null = new(0, null);
        public static readonly BJsonValue True = new(1, SpecialBoolTag);
        public static readonly BJsonValue False = new(0, SpecialBoolTag);

        private BJsonValue(ulong data, object? reference)
        {
            _data = data;
            _reference = reference;
        }

        public BJsonValueType Type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var r = _reference;
                if (r is null) return BJsonValueType.Null;
                if (ReferenceEquals(r, SpecialIntTag)) return BJsonValueType.Integer;
                if (ReferenceEquals(r, SpecialDoubleTag)) return BJsonValueType.Float;
                if (ReferenceEquals(r, SpecialBoolTag)) return BJsonValueType.Boolean;

                return _data switch
                {
                    TagString => BJsonValueType.String,
                    TagArray => BJsonValueType.Array,
                    TagObject => BJsonValueType.Object,
                    TagBinary => BJsonValueType.Binary,
                    _ => throw new InvalidOperationException("Invalid BJsonValue state.")
                };
            }
        }

        public bool IsNull => _reference is null;
        public bool IsInteger => ReferenceEquals(_reference, SpecialIntTag);
        public bool IsFloat => ReferenceEquals(_reference, SpecialDoubleTag);
        public bool IsBoolean => ReferenceEquals(_reference, SpecialBoolTag);

        public bool IsString => _reference is not null
            && !ReferenceEquals(_reference, SpecialIntTag)
            && !ReferenceEquals(_reference, SpecialBoolTag)
            && !ReferenceEquals(_reference, SpecialDoubleTag)
            && _data == TagString;

        public bool IsArray => _reference is not null
            && !ReferenceEquals(_reference, SpecialIntTag)
            && !ReferenceEquals(_reference, SpecialBoolTag)
            && !ReferenceEquals(_reference, SpecialDoubleTag)
            && _data == TagArray;

        public bool IsObject => _reference is not null
            && !ReferenceEquals(_reference, SpecialIntTag)
            && !ReferenceEquals(_reference, SpecialBoolTag)
            && !ReferenceEquals(_reference, SpecialDoubleTag)
            && _data == TagObject;

        public bool IsBinary => _reference is not null
            && !ReferenceEquals(_reference, SpecialIntTag)
            && !ReferenceEquals(_reference, SpecialBoolTag)
            && !ReferenceEquals(_reference, SpecialDoubleTag)
            && _data == TagBinary;

        public bool IsNumber => IsInteger || IsFloat;

        public sbyte SByteValue => IsInteger ? (sbyte)_data : throw new InvalidOperationException("Value is not an integer.");
        public short ShortValue => IsInteger ? (short)_data : throw new InvalidOperationException("Value is not an integer.");
        public int IntValue => IsInteger ? (int)_data : throw new InvalidOperationException("Value is not an integer.");
        public long LongValue => IsInteger ? (long)_data : throw new InvalidOperationException("Value is not an integer.");
        public byte ByteValue => IsInteger ? (byte)_data : throw new InvalidOperationException("Value is not an integer.");
        public ushort UShortValue => IsInteger ? (ushort)_data : throw new InvalidOperationException("Value is not an integer.");
        public uint UIntValue => IsInteger ? (uint)_data : throw new InvalidOperationException("Value is not an integer.");
        public ulong ULongValue => IsInteger ? _data : throw new InvalidOperationException("Value is not an integer.");
        public float FloatValue => IsFloat ? (float)BitConverter.Int64BitsToDouble((long)_data) : throw new InvalidOperationException("Value is not a float.");
        public double DoubleValue => IsFloat ? BitConverter.Int64BitsToDouble((long)_data) : throw new InvalidOperationException("Value is not a float.");
        public bool BoolValue => IsBoolean ? _data != 0 : throw new InvalidOperationException("Value is not a boolean.");
        public string StringValue => IsString ? (string)_reference! : throw new InvalidOperationException("Value is not a string.");
        public BJsonArray ArrayValue => IsArray ? (BJsonArray)_reference! : throw new InvalidOperationException("Value is not an array.");
        public BJsonObject ObjectValue => IsObject ? (BJsonObject)_reference! : throw new InvalidOperationException("Value is not an object.");
        public BJsonBinary BinaryValue => IsBinary ? (BJsonBinary)_reference! : throw new InvalidOperationException("Value is not binary.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSByte(out sbyte value)
        {
            if (IsInteger)
            {
                long longValue = (long)_data;
                if (longValue >= sbyte.MinValue && longValue <= sbyte.MaxValue)
                {
                    value = (sbyte)longValue;
                    return true;
                }
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetShort(out short value)
        {
            if (IsInteger)
            {
                long longValue = (long)_data;
                if (longValue >= short.MinValue && longValue <= short.MaxValue)
                {
                    value = (short)longValue;
                    return true;
                }
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetInt(out int value)
        {
            if (IsInteger)
            {
                long longValue = (long)_data;
                if (longValue >= int.MinValue && longValue <= int.MaxValue)
                {
                    value = (int)longValue;
                    return true;
                }
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetLong(out long value)
        {
            if (IsInteger)
            {
                value = (long)_data;
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetByte(out byte value)
        {
            if (IsInteger)
            {
                if (_data <= byte.MaxValue)
                {
                    value = (byte)_data;
                    return true;
                }
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetUShort(out ushort value)
        {
            if (IsInteger)
            {
                if (_data <= ushort.MaxValue)
                {
                    value = (ushort)_data;
                    return true;
                }
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetUInt(out uint value)
        {
            if (IsInteger)
            {
                if (_data <= uint.MaxValue)
                {
                    value = (uint)_data;
                    return true;
                }
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetULong(out ulong value)
        {
            if (IsInteger)
            {
                value = _data;
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetFloat(out float value)
        {
            if (IsFloat)
            {
                value = (float)BitConverter.Int64BitsToDouble((long)_data);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetDouble(out double value)
        {
            if (IsFloat)
            {
                value = BitConverter.Int64BitsToDouble((long)_data);
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBool(out bool value)
        {
            if (IsBoolean)
            {
                value = _data != 0;
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetString(out string value)
        {
            if (IsString)
            {
                value = (string)_reference!;
                return true;
            }
            value = default!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetArray(out BJsonArray value)
        {
            if (IsArray)
            {
                value = (BJsonArray)_reference!;
                return true;
            }
            value = default!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetObject(out BJsonObject value)
        {
            if (IsObject)
            {
                value = (BJsonObject)_reference!;
                return true;
            }
            value = default!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBinary(out BJsonBinary value)
        {
            if (IsBinary)
            {
                value = (BJsonBinary)_reference!;
                return true;
            }
            value = default!;
            return false;
        }

        // Numeric helpers that accept either integer or float representations.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNumberAsDouble(out double value)
        {
            if (IsFloat)
            {
                value = BitConverter.Int64BitsToDouble((long)_data);
                return true;
            }
            if (IsInteger)
            {
                value = (double)(long)_data;
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNumberAsFloat(out float value)
        {
            if (IsFloat)
            {
                value = (float)BitConverter.Int64BitsToDouble((long)_data);
                return true;
            }
            if (IsInteger)
            {
                value = (float)(long)_data;
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNumberAsInt(out int value)
        {
            if (IsInteger)
            {
                value = (int)_data;
                return true;
            }
            if (IsFloat)
            {
                var v = BitConverter.Int64BitsToDouble((long)_data);
                if (v >= int.MinValue && v <= int.MaxValue && !double.IsNaN(v) && !double.IsInfinity(v))
                {
                    value = (int)v;
                    return true;
                }
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNumberAsLong(out long value)
        {
            if (IsInteger)
            {
                value = (long)_data;
                return true;
            }
            if (IsFloat)
            {
                var v = BitConverter.Int64BitsToDouble((long)_data);
                if (v >= long.MinValue && v <= long.MaxValue && !double.IsNaN(v) && !double.IsInfinity(v))
                {
                    value = (long)v;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public override bool Equals(object? obj) => obj is BJsonValue other && Equals(other);

        public bool Equals(BJsonValue other)
        {
            if (ReferenceEquals(_reference, other._reference) && _data == other._data)
                return true;

            var type1 = Type;
            var type2 = other.Type;
            if (type1 == type2)
            {
                return type1 switch
                {
                    BJsonValueType.Null => true,
                    BJsonValueType.Integer => _data == other._data,
                    BJsonValueType.Float => _data == other._data, // bitwise compare
                    BJsonValueType.Boolean => _data == other._data,
                    BJsonValueType.String => ReferenceEquals(_reference, other._reference) || string.Equals((string)_reference!, (string)other._reference!, StringComparison.Ordinal),
                    BJsonValueType.Array or BJsonValueType.Object or BJsonValueType.Binary => Equals(_reference, other._reference),
                    _ => false,
                };
            }

            if (IsNumber && other.IsNumber)
            {
                double a = IsFloat ? BitConverter.Int64BitsToDouble((long)_data) : (double)(long)_data;
                double b = other.IsFloat ? BitConverter.Int64BitsToDouble((long)other._data) : (double)(long)other._data;
                return a.Equals(b);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var type = Type;

                // Normalize numeric hash: all numbers hash as double for consistency with Equals()
                if (IsNumber)
                {
                    double value = IsFloat ? BitConverter.Int64BitsToDouble((long)_data) : (double)(long)_data;
                    return value.GetHashCode();
                }

                return type switch
                {
                    BJsonValueType.Null => 0,
                    BJsonValueType.Boolean => (_data != 0 ? 1 : 0),
                    BJsonValueType.String => ((string)_reference!).GetHashCode(),
                    BJsonValueType.Array or BJsonValueType.Object or BJsonValueType.Binary => _reference?.GetHashCode() ?? 0,
                    _ => ((int)type).GetHashCode(),
                };
            }
        }

        public int CompareTo(BJsonValue other)
        {
            if (Equals(other))
                return 0;

            if (IsNumber && other.IsNumber)
            {
                double a = IsFloat ? BitConverter.Int64BitsToDouble((long)_data) : (double)(long)_data;
                double b = other.IsFloat ? BitConverter.Int64BitsToDouble((long)other._data) : (double)(long)other._data;
                return a.CompareTo(b);
            }

            var type1 = Type;
            var type2 = other.Type;

            if (type1 != type2)
                return ((int)type1).CompareTo((int)type2);

            return type1 switch
            {
                BJsonValueType.String => string.CompareOrdinal((string)_reference!, (string)other._reference!),
                BJsonValueType.Boolean => ((_data != 0) ? 1 : 0).CompareTo((other._data != 0) ? 1 : 0),
                BJsonValueType.Array => CompareArrays((BJsonArray)_reference!, (BJsonArray)other._reference!),
                BJsonValueType.Object or BJsonValueType.Binary => CompareByReference(_reference, other._reference),
                _ => 0,
            };
        }

        private static int CompareArrays(BJsonArray a, BJsonArray b)
        {
            int minCount = System.Math.Min(a.Count, b.Count);
            for (int i = 0; i < minCount; i++)
            {
                int cmp = a[i].CompareTo(b[i]);
                if (cmp != 0)
                    return cmp;
            }
            return a.Count.CompareTo(b.Count);
        }

        private static int CompareByReference(object? a, object? b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a is null) return -1;
            if (b is null) return 1;
            return a.GetHashCode().CompareTo(b.GetHashCode());
        }


        public static BJsonValue CreateNull() => Null;
        public static BJsonValue Create() => Null;
        public static BJsonValue Create(sbyte value) => new((ulong)value, SpecialIntTag);
        public static BJsonValue Create(short value) => new((ulong)value, SpecialIntTag);
        public static BJsonValue Create(int value) => new((ulong)value, SpecialIntTag);
        public static BJsonValue Create(long value) => new((ulong)value, SpecialIntTag);
        public static BJsonValue Create(byte value) => new((ulong)value, SpecialIntTag);
        public static BJsonValue Create(ushort value) => new((ulong)value, SpecialIntTag);
        public static BJsonValue Create(uint value) => new((ulong)value, SpecialIntTag);
        public static BJsonValue Create(ulong value) => new(value, SpecialIntTag);
        public static BJsonValue Create(float value) => Create((double)value);
        public static BJsonValue Create(double value) => new(unchecked((ulong)BitConverter.DoubleToInt64Bits(value)), SpecialDoubleTag);
        public static BJsonValue Create(bool value) => value ? True : False;
        public static BJsonValue Create(string? value) => value is null ? Null : new(TagString, value);
        public static BJsonValue Create(BJsonArray? value) => value is null ? Null : new(TagArray, value);
        public static BJsonValue Create(BJsonArray? value, bool asCopy) => value is null ? Null : new(TagArray, asCopy ? new BJsonArray(value) : value);
        public static BJsonValue Create(BJsonObject? value) => value is null ? Null : new(TagObject, value);
        public static BJsonValue Create(BJsonObject? value, bool asCopy) => value is null ? Null : new(TagObject, asCopy ? new BJsonObject(value) : value);
        public static BJsonValue Create(BJsonBinary? value) => value is null ? Null : new(TagBinary, value);


        public static bool operator ==(BJsonValue left, BJsonValue right) => left.Equals(right);
        public static bool operator !=(BJsonValue left, BJsonValue right) => !left.Equals(right);
    }
}
