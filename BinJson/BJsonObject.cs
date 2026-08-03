#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using Krampus.BinJson.Error;

namespace Krampus.BinJson
{
    /// <summary>
    /// A JSON object container backed by <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Memory:</b> Uses Dictionary&lt;string, BJsonValue&gt; for storage.</para>
    /// <para>Each entry allocates space for string key + 16-byte BJsonValue + dictionary overhead (~48 bytes/entry on 64-bit).</para>
    /// <para>Consider pre-sizing with capacity constructor if final property count is known to reduce rehashing.</para>
    /// </remarks>
    public sealed class BJsonObject : IDictionary<string, BJsonValue>, ICollection<KeyValuePair<string, BJsonValue>>, IEnumerable<KeyValuePair<string, BJsonValue>>
    {
        private readonly Dictionary<string, BJsonValue> _values;

        public BJsonObject()
        {
            _values = new Dictionary<string, BJsonValue>();
        }
        public BJsonObject(int capacity)
        {
            _values = new Dictionary<string, BJsonValue>(capacity);
        }
        public BJsonObject(IDictionary<string, BJsonValue> values)
        {
            _values = new Dictionary<string, BJsonValue>(values);
        }
        public BJsonObject(IEnumerable<KeyValuePair<string, BJsonValue>> values)
        {
            _values = new Dictionary<string, BJsonValue>();
            foreach (var kvp in values)
                _values.Add(kvp.Key, kvp.Value);
        }

        public BJsonValue this[string key]
        {
            get => _values[key];
            set => _values[key] = value;
        }

        public ICollection<string> Keys => _values.Keys;

        public ICollection<BJsonValue> Values => _values.Values;

        public int Count => _values.Count;

        public bool IsReadOnly => false;

        public void Add(string key, BJsonValue value) => _values.Add(key, value);

        public void Clear() => _values.Clear();

        public bool ContainsKey(string key) => _values.ContainsKey(key);

        public bool Remove(string key) => _values.Remove(key);

        public bool TryGetValue(string key, out BJsonValue value) => _values.TryGetValue(key, out value);

        public void Add(KeyValuePair<string, BJsonValue> item) => _values.Add(item.Key, item.Value);

        public bool Contains(KeyValuePair<string, BJsonValue> item) => _values.ContainsKey(item.Key) && _values[item.Key].Equals(item.Value);

        public void CopyTo(KeyValuePair<string, BJsonValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, BJsonValue>>)_values).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, BJsonValue> item)
        {
            if (_values.TryGetValue(item.Key, out var existing) && existing.Equals(item.Value))
                return _values.Remove(item.Key);
            return false;
        }

        public IEnumerator<KeyValuePair<string, BJsonValue>> GetEnumerator() => _values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        #region QoL Methods

        public void AddNull(string key) => _values.Add(key, BJsonValue.Null);
        public void Add(string key, sbyte value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, short value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, int value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, long value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, byte value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, ushort value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, uint value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, ulong value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, float value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, double value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, bool value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, string? value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, BJsonArray? value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, BJsonArray? value, bool asCopy) => _values.Add(key, BJsonValue.Create(value, asCopy));
        public void Add(string key, BJsonObject? value) => _values.Add(key, BJsonValue.Create(value));
        public void Add(string key, BJsonObject? value, bool asCopy) => _values.Add(key, BJsonValue.Create(value, asCopy));
        public void Add(string key, BJsonBinary? value) => _values.Add(key, BJsonValue.Create(value));

        public bool TryAdd(string key, BJsonValue value)
        {
            if (_values.ContainsKey(key))
                return false;
            _values.Add(key, value);
            return true;
        }

        public void AddOrUpdate(string key, BJsonValue value)
        {
            _values[key] = value;
        }

        public BJsonValue GetValueOrDefault(string key, BJsonValue defaultValue = default)
        {
            return _values.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public int GetIntOrDefault(string key, int defaultValue = 0)
        {
            return TryGetInt(key, out var value) ? value : defaultValue;
        }

        public long GetLongOrDefault(string key, long defaultValue = 0)
        {
            return TryGetLong(key, out var value) ? value : defaultValue;
        }

        public double GetDoubleOrDefault(string key, double defaultValue = 0)
        {
            return TryGetDouble(key, out var value) ? value : defaultValue;
        }

        public bool GetBoolOrDefault(string key, bool defaultValue = false)
        {
            return TryGetBool(key, out var value) ? value : defaultValue;
        }

        public string? GetStringOrDefault(string key, string? defaultValue = null)
        {
            return TryGetString(key, out var value) ? value : defaultValue;
        }

        public BJsonObject Clone() => new(_values);

        public BJsonObject DeepClone(int maxDepth = 256)
        {
            if (maxDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(maxDepth));

            var copy = new BJsonObject(_values.Count);
            foreach (var kvp in _values)
            {
                copy.Add(kvp.Key, kvp.Value.DeepClone(maxDepth));
            }
            return copy;
        }

        public void Merge(BJsonObject other, bool overwrite = true)
        {
            Merge(other, overwrite ? BJsonMergeStrategy.Overwrite : BJsonMergeStrategy.KeepExisting);
        }

        public void Merge(BJsonObject other, BJsonMergeStrategy strategy, int maxDepth = 256)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (maxDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(maxDepth));

            switch (strategy)
            {
                case BJsonMergeStrategy.Overwrite:
                    foreach (var kvp in other._values)
                        _values[kvp.Key] = kvp.Value;
                    return;

                case BJsonMergeStrategy.KeepExisting:
                    foreach (var kvp in other._values)
                    {
                        if (!_values.ContainsKey(kvp.Key))
                            _values.Add(kvp.Key, kvp.Value);
                    }
                    return;

                case BJsonMergeStrategy.DeepMerge:
                    MergeDeep(other, maxDepth);
                    return;

                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy));
            }
        }

        private void MergeDeep(BJsonObject other, int depth)
        {
            if (depth <= 0)
                throw new BJsonValidationException("Maximum merge depth exceeded.");

            foreach (var kvp in other._values)
            {
                if (_values.TryGetValue(kvp.Key, out var existing)
                    && existing.TryGetObject(out var existingObj)
                    && kvp.Value.TryGetObject(out var incomingObj))
                {
                    existingObj.Merge(incomingObj, BJsonMergeStrategy.DeepMerge, depth - 1);
                    continue;
                }

                _values[kvp.Key] = kvp.Value;
            }
        }

        public bool Update(string key, Func<BJsonValue, BJsonValue> updater)
        {
            if (updater is null)
                throw new ArgumentNullException(nameof(updater));

            if (!_values.TryGetValue(key, out var existing))
                return false;

            _values[key] = updater(existing);
            return true;
        }

        public bool RenameKey(string key, string newKey, bool overwrite = false)
        {
            if (key == newKey)
                return _values.ContainsKey(key);

            if (!_values.TryGetValue(key, out var value))
                return false;

            if (!overwrite && _values.ContainsKey(newKey))
                return false;

            _values.Remove(key);
            _values[newKey] = value;
            return true;
        }

        public IEnumerable<string> GetKeysByType(BJsonValueType type)
        {
            foreach (var kvp in _values)
            {
                if (kvp.Value.Type == type)
                    yield return kvp.Key;
            }
        }

        public bool TryGetSByte(string key, out sbyte value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetSByte(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetShort(string key, out short value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetShort(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetInt(string key, out int value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetInt(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetLong(string key, out long value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetLong(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetFloat(string key, out float value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetFloat(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetDouble(string key, out double value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetDouble(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetBool(string key, out bool value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetBool(out value))
                return true;
            value = false;
            return false;
        }
        public bool TryGetString(string key, out string value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetString(out value))
                return true;
            value = null!;
            return false;
        }
        public bool TryGetArray(string key, out BJsonArray value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetArray(out value))
                return true;
            value = null!;
            return false;
        }
        public bool TryGetObject(string key, out BJsonObject value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetObject(out value))
                return true;
            value = null!;
            return false;
        }
        public bool TryGetBinary(string key, out BJsonBinary value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetBinary(out value))
                return true;
            value = null!;
            return false;
        }

        // Numeric helpers
        public bool TryGetNumberAsDouble(string key, out double value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetNumberAsDouble(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetNumberAsFloat(string key, out float value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetNumberAsFloat(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetNumberAsInt(string key, out int value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetNumberAsInt(out value))
                return true;
            value = 0;
            return false;
        }
        public bool TryGetNumberAsLong(string key, out long value)
        {
            if (TryGetValue(key, out var jsonValue) && jsonValue.TryGetNumberAsLong(out value))
                return true;
            value = 0;
            return false;
        }

        #endregion

        #region Equality

        public override bool Equals(object? obj) => obj is BJsonObject other && Equals(other);

        public bool Equals(BJsonObject? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_values.Count != other._values.Count) return false;

            foreach (var kvp in _values)
            {
                if (!other._values.TryGetValue(kvp.Key, out var otherValue))
                    return false;
                if (!kvp.Value.Equals(otherValue))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var kvp in _values)
                {
                    hash ^= kvp.Key.GetHashCode();
                    hash ^= kvp.Value.GetHashCode();
                }
                return hash;
            }
        }

        #endregion
    }
}
