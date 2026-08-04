#nullable enable

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Krampus.BinJson.Utilities
{
    public readonly ref struct RentedBuffer
    {
        public static RentedBuffer Empty => new(0);

        private readonly byte[]? _array;
        private readonly Span<byte> _span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RentedBuffer(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");
            _array = size > 0 ? ArrayPool<byte>.Shared.Rent(size) : null;
            _span = _array is not null ? _array.AsSpan(0, size) : Span<byte>.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RentedBuffer(Span<byte> span)
        {
            _array = null;
            _span = span;
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span.Length;
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span.IsEmpty;
        }

        public Span<byte> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span;
        }

        public byte[] Array
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array ?? throw new InvalidOperationException("This RentedBuffer does not own an array.");
        }

        public byte this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _span[index] = value; 
        }

        public byte this[Index index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _span[index] = value;
        }

        public Span<byte> this[Range range]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span[range];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _span.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<byte> destination)
        {
            _span.CopyTo(destination);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(byte value)
        {
            _span.Fill(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan() => _span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(int start, int length)
        {
            return _span.Slice(start, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(int start)
        {
            return _span.Slice(start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(Range range)
        {
            return _span[range];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToArray()
        {
            return _span.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_array is not null)
                ArrayPool<byte>.Shared.Return(_array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RentedBuffer(Span<byte> span)
        {
            return new RentedBuffer(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<byte>(RentedBuffer rentedBuffer)
        {
            return rentedBuffer._span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(RentedBuffer rentedBuffer)
        {
            return rentedBuffer._span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RentedBuffer Rent(int size)
        {
            return new RentedBuffer(size);
        }
    }
}
