#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Krampus.BinJson
{
    public sealed class BJsonBinary : IReadOnlyList<byte>, IEnumerable<byte>
    {
        private readonly byte[] _data;

        // Public constructor always copies to prevent aliasing
        public BJsonBinary(byte[] data)
        {
            _data = (byte[])data.Clone();
        }

        public BJsonBinary(ReadOnlySpan<byte> data)
        {
            _data = data.ToArray();
        }

        // Internal constructor for zero-copy scenarios when caller guarantees immutability
        private BJsonBinary(byte[] data, bool trusted)
        {
            _data = data;
        }

        public byte this[int index]
        {
            get => _data[index];
        }

        public int Count => _data.Length;

        public bool IsReadOnly => true;

        public IEnumerator<byte> GetEnumerator() => ((IEnumerable<byte>)_data).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region QoL Methods

        public ReadOnlySpan<byte> AsSpan() => _data.AsSpan();

        #endregion

        #region Equality

        public override bool Equals(object? obj) => obj is BJsonBinary other && Equals(other);

        public bool Equals(BJsonBinary? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_data.Length != other._data.Length) return false;

            for (int i = 0; i < _data.Length; i++)
            {
                if (_data[i] != other._data[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < _data.Length; i++)
                {
                    hash = hash * 31 + _data[i];
                }
                return hash;
            }
        }

        #endregion

        public static BJsonBinary Create(ReadOnlySpan<byte> data) => new(data);
        public static BJsonBinary Create(byte[] data) => new(data);

        // Unsafe: caller guarantees the array will not be modified after this call
        internal static BJsonBinary CreateUnsafe(byte[] data) => new(data, trusted: true);
    }
}
