#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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

        public ReadOnlyMemory<byte> AsMemory() => _data;

        public byte[] ToArray() => (byte[])_data.Clone();

        public void CopyTo(byte[] destination, int destinationIndex = 0)
        {
            Array.Copy(_data, 0, destination, destinationIndex, _data.Length);
        }

        public string ToBase64() => Convert.ToBase64String(_data);

        public string ToHex()
        {
            var chars = new char[_data.Length * 2];
            int ci = 0;
            for (int i = 0; i < _data.Length; i++)
            {
                byte b = _data[i];
                chars[ci++] = GetHexChar(b >> 4);
                chars[ci++] = GetHexChar(b & 0xF);
            }
            return new string(chars);
        }

        public override string ToString() => $"Binary[{Count}]";

        private static char GetHexChar(int value)
        {
            return (char)(value < 10 ? ('0' + value) : ('A' + (value - 10)));
        }

        private static byte ParseHexChar(char c)
        {
            if (c >= '0' && c <= '9') return (byte)(c - '0');
            if (c >= 'a' && c <= 'f') return (byte)(10 + (c - 'a'));
            if (c >= 'A' && c <= 'F') return (byte)(10 + (c - 'A'));
            throw new FormatException($"Invalid hex character: '{c}'.");
        }

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
        public static BJsonBinary FromBase64(string base64) => new(Convert.FromBase64String(base64));

        public static BJsonBinary FromHex(string hex)
        {
            if (hex is null)
                throw new ArgumentNullException(nameof(hex));
            if ((hex.Length & 1) != 0)
                throw new FormatException("Hex string length must be even.");

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int hi = ParseHexChar(hex[i * 2]);
                int lo = ParseHexChar(hex[i * 2 + 1]);
                bytes[i] = (byte)((hi << 4) | lo);
            }
            return new BJsonBinary(bytes, trusted: true);
        }

        public static BJsonBinary FromString(string value, Encoding? encoding = null)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            var codec = encoding ?? Encoding.UTF8;
            return new BJsonBinary(codec.GetBytes(value), trusted: true);
        }

        public string DecodeString(Encoding? encoding = null)
        {
            var codec = encoding ?? Encoding.UTF8;
            return codec.GetString(_data, 0, _data.Length);
        }

        public static BJsonBinary operator +(BJsonBinary left, BJsonBinary right)
        {
            if (left is null)
                throw new ArgumentNullException(nameof(left));
            if (right is null)
                throw new ArgumentNullException(nameof(right));

            var result = new byte[left.Count + right.Count];
            left.CopyTo(result, 0);
            right.CopyTo(result, left.Count);
            return new BJsonBinary(result, trusted: true);
        }

        public static bool operator ==(BJsonBinary? left, BJsonBinary? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(BJsonBinary? left, BJsonBinary? right) => !(left == right);

        // Unsafe: caller guarantees the array will not be modified after this call
        internal static BJsonBinary CreateUnsafe(byte[] data) => new(data, trusted: true);
    }
}
