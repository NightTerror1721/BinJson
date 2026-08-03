#nullable enable

using System;
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

        public int Capacity
        {
            get => _values.Capacity;
            set => _values.Capacity = value;
        }

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

        public BJsonValue GetOrDefault(int index, BJsonValue defaultValue = default)
        {
            return index >= 0 && index < _values.Count ? _values[index] : defaultValue;
        }

        public int GetIntOrDefault(int index, int defaultValue = 0)
        {
            return TryGetInt(index, out var value) ? value : defaultValue;
        }

        public long GetLongOrDefault(int index, long defaultValue = 0)
        {
            return TryGetLong(index, out var value) ? value : defaultValue;
        }

        public double GetDoubleOrDefault(int index, double defaultValue = 0)
        {
            return TryGetDouble(index, out var value) ? value : defaultValue;
        }

        public bool GetBoolOrDefault(int index, bool defaultValue = false)
        {
            return TryGetBool(index, out var value) ? value : defaultValue;
        }

        public string? GetStringOrDefault(int index, string? defaultValue = null)
        {
            return TryGetString(index, out var value) ? value : defaultValue;
        }

        public int EnsureCapacity(int capacity)
        {
            if (capacity <= _values.Capacity)
                return _values.Capacity;

            _values.Capacity = capacity;
            return _values.Capacity;
        }

        public void TrimExcess() => _values.TrimExcess();

        public int FindIndex(Func<BJsonValue, bool> predicate)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));
            return _values.FindIndex(value => predicate(value));
        }

        public int FindLastIndex(Func<BJsonValue, bool> predicate)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));
            return _values.FindLastIndex(value => predicate(value));
        }

        public bool Find(Func<BJsonValue, bool> predicate, out BJsonValue value)
        {
            int index = FindIndex(predicate);
            if (index >= 0)
            {
                value = _values[index];
                return true;
            }

            value = BJsonValue.Null;
            return false;
        }

        public BJsonArray FindAll(Func<BJsonValue, bool> predicate)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var matches = new BJsonArray();
            for (int i = 0; i < _values.Count; i++)
            {
                var item = _values[i];
                if (predicate(item))
                    matches.Add(item);
            }
            return matches;
        }

        public bool TryFirst(out BJsonValue value)
        {
            if (_values.Count > 0)
            {
                value = _values[0];
                return true;
            }

            value = BJsonValue.Null;
            return false;
        }

        public bool TryLast(out BJsonValue value)
        {
            if (_values.Count > 0)
            {
                value = _values[_values.Count - 1];
                return true;
            }

            value = BJsonValue.Null;
            return false;
        }

        public bool First(Func<BJsonValue, bool> predicate, out BJsonValue value)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            for (int i = 0; i < _values.Count; i++)
            {
                var current = _values[i];
                if (predicate(current))
                {
                    value = current;
                    return true;
                }
            }

            value = BJsonValue.Null;
            return false;
        }

        public bool Last(Func<BJsonValue, bool> predicate, out BJsonValue value)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            for (int i = _values.Count - 1; i >= 0; i--)
            {
                var current = _values[i];
                if (predicate(current))
                {
                    value = current;
                    return true;
                }
            }

            value = BJsonValue.Null;
            return false;
        }

        public IEnumerable<BJsonValue> Where(Func<BJsonValue, bool> predicate)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            for (int i = 0; i < _values.Count; i++)
            {
                var item = _values[i];
                if (predicate(item))
                    yield return item;
            }
        }

        public IEnumerable<TResult> Select<TResult>(Func<BJsonValue, TResult> selector)
        {
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            for (int i = 0; i < _values.Count; i++)
                yield return selector(_values[i]);
        }

        public BJsonArray Clone() => new(_values);

        public BJsonArray DeepClone(int maxDepth = 256)
        {
            if (maxDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(maxDepth));

            var copy = new BJsonArray(_values.Count);
            for (int i = 0; i < _values.Count; i++)
            {
                copy.Add(_values[i].DeepClone(maxDepth));
            }
            return copy;
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
