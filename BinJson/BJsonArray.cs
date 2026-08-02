#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace Krampus.BinJson
{
    /// <summary>
    /// A JSON array container backed by <see cref="List{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Memory:</b> Uses List&lt;BJsonValue&gt; for storage. Each BJsonValue is 16 bytes.</para>
    /// <para>Small arrays (1-10 elements) allocate ~160-320 bytes plus List overhead.</para>
    /// <para>Consider pre-sizing with capacity constructor if final count is known to reduce reallocations.</para>
    /// </remarks>
    public sealed class BJsonArray : IList<BJsonValue>, ICollection<BJsonValue>, IEnumerable<BJsonValue>
    {
        private readonly List<BJsonValue> _values;

        public BJsonArray()
        {
            _values = new List<BJsonValue>();
        }
        public BJsonArray(int capacity)
        {
            _values = new List<BJsonValue>(capacity);
        }
        public BJsonArray(IEnumerable<BJsonValue> values)
        {
            _values = new List<BJsonValue>(values);
        }

        public BJsonValue this[int index]
        {
            get => _values[index];
            set => _values[index] = value;
        }

        public int Count => _values.Count;

        public bool IsReadOnly => false;

        public void Add(BJsonValue item) => _values.Add(item);

        public void Clear() => _values.Clear();

        public bool Contains(BJsonValue item) => _values.Contains(item);

        public void CopyTo(BJsonValue[] array, int arrayIndex) => _values.CopyTo(array, arrayIndex);

        public IEnumerator<BJsonValue> GetEnumerator() => _values.GetEnumerator();

        public int IndexOf(BJsonValue item) => _values.IndexOf(item);

        public void Insert(int index, BJsonValue item) => _values.Insert(index, item);

        public bool Remove(BJsonValue item) => _values.Remove(item);

        public void RemoveAt(int index) => _values.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        #region QoL Methods

        public void AddNull() => _values.Add(BJsonValue.Null);
        public void Add() => _values.Add(BJsonValue.Null);
        public void Add(sbyte value) => _values.Add(BJsonValue.Create(value));
        public void Add(short value) => _values.Add(BJsonValue.Create(value));
        public void Add(int value) => _values.Add(BJsonValue.Create(value));
        public void Add(long value) => _values.Add(BJsonValue.Create(value));
        public void Add(byte value) => _values.Add(BJsonValue.Create(value));
        public void Add(ushort value) => _values.Add(BJsonValue.Create(value));
        public void Add(uint value) => _values.Add(BJsonValue.Create(value));
        public void Add(ulong value) => _values.Add(BJsonValue.Create(value));
        public void Add(float value) => _values.Add(BJsonValue.Create(value));
        public void Add(double value) => _values.Add(BJsonValue.Create(value));
        public void Add(bool value) => _values.Add(BJsonValue.Create(value));
        public void Add(string? value) => _values.Add(BJsonValue.Create(value));
        public void Add(BJsonArray? value) => _values.Add(BJsonValue.Create(value));
        public void Add(BJsonArray? value, bool asCopy) => _values.Add(BJsonValue.Create(value, asCopy));
        public void Add(BJsonObject? value) => _values.Add(BJsonValue.Create(value));
        public void Add(BJsonObject? value, bool asCopy) => _values.Add(BJsonValue.Create(value, asCopy));
        public void Add(BJsonBinary? value) => _values.Add(BJsonValue.Create(value));

        public void AddRange(IEnumerable<BJsonValue> values) { foreach (var value in values) _values.Add(value); }
        public void AddRange(params BJsonValue[] values) { foreach (var value in values) _values.Add(value); }

        public void AddRange(IEnumerable<sbyte> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<short> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<int> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<long> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<byte> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<ushort> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<uint> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<ulong> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<float> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<double> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<bool> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<string?> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<BJsonArray?> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<BJsonArray?> values, bool asCopy) { foreach (var value in values) _values.Add(BJsonValue.Create(value, asCopy)); }
        public void AddRange(IEnumerable<BJsonObject?> values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(IEnumerable<BJsonObject?> values, bool asCopy) { foreach (var value in values) _values.Add(BJsonValue.Create(value, asCopy)); }

        public void AddRange(params sbyte[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params short[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params int[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params long[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params byte[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params ushort[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params uint[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params ulong[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params float[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params double[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params bool[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params string?[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params BJsonArray?[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }
        public void AddRange(params BJsonObject?[] values) { foreach (var value in values) _values.Add(BJsonValue.Create(value)); }

        public bool TryGetValue(int index, out BJsonValue value)
        {
            if (index < 0 || index >= _values.Count)
            {
                value = BJsonValue.Null;
                return false;
            }
            value = _values[index];
            return true;
        }
        
        public bool TryGetSByte(int index, out sbyte value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetSByte(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetShort(int index, out short value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetShort(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetInt(int index, out int value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetInt(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetLong(int index, out long value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetLong(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetByte(int index, out byte value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetByte(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetUShort(int index, out ushort value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetUShort(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetUInt(int index, out uint value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetUInt(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetULong(int index, out ulong value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetULong(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetFloat(int index, out float value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetFloat(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetDouble(int index, out double value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetDouble(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetBool(int index, out bool value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetBool(out value))
                return true;
            value = false;
            return false;
        }
        public bool TryGetString(int index, out string value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetString(out value))
                return true;
            value = null!;
            return false;
        }
        public bool TryGetArray(int index, out BJsonArray value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetArray(out value))
                return true;
            value = null!;
            return false;
        }
        public bool TryGetObject(int index, out BJsonObject value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetObject(out value))
                return true;
            value = null!;
            return false;
        }
        public bool TryGetBinary(int index, out BJsonBinary value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetBinary(out value))
                return true;
            value = null!;
            return false;
        }

        // Numeric helpers: delegate to contained BJsonValue
        public bool TryGetNumberAsDouble(int index, out double value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetNumberAsDouble(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetNumberAsFloat(int index, out float value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetNumberAsFloat(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetNumberAsInt(int index, out int value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetNumberAsInt(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetNumberAsLong(int index, out long value)
        {
            if (TryGetValue(index, out var jsonValue) && jsonValue.TryGetNumberAsLong(out value))
                return true;
            value = 0;
            return false;
        }

        #endregion

        #region Equality

        public override bool Equals(object? obj) => obj is BJsonArray other && Equals(other);

        public bool Equals(BJsonArray? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_values.Count != other._values.Count) return false;

            for (int i = 0; i < _values.Count; i++)
            {
                if (!_values[i].Equals(other._values[i]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < _values.Count; i++)
                {
                    hash = hash * 31 + _values[i].GetHashCode();
                }
                return hash;
            }
        }

        #endregion
    }
}
