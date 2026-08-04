#nullable enable

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Krampus.BinJson.Utilities
{
    public sealed class BufferReaderStream : IDisposable
    {
        public const int DefaultBufferSize = 8192; // 8 KB
        private const int MaxBufferSize = 1024 * 1024; // 1 MB

        private readonly Stream? _stream;
        private readonly bool _leaveOpen;
        private byte[] _buffer;
        private int _bufferPos;
        private int _bufferLen;
        private long _bytesRead;
        private readonly bool _ownsPooledBuffer;
        private readonly bool _isMemoryBacked;
        private readonly int _memoryStart;
        private readonly int _memoryEnd;
        private bool _disposed;

        public BufferReaderStream(Stream stream, bool leaveOpen = false, int bufferSize = DefaultBufferSize)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _leaveOpen = leaveOpen;
            _buffer = ArrayPool<byte>.Shared.Rent(Math.Min(bufferSize, MaxBufferSize));
            _bufferPos = 0;
            _bufferLen = 0;
            _bytesRead = 0;
            _ownsPooledBuffer = true;
            _isMemoryBacked = false;
            _memoryStart = 0;
            _memoryEnd = 0;
        }

        public BufferReaderStream(ReadOnlyMemory<byte> data)
        {
            _stream = null;
            _leaveOpen = true;

            if (MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment) && segment.Array is not null)
            {
                _buffer = segment.Array;
                _bufferPos = segment.Offset;
                _bufferLen = segment.Offset + segment.Count;
                _memoryStart = segment.Offset;
                _memoryEnd = _bufferLen;
            }
            else
            {
                _buffer = data.ToArray();
                _bufferPos = 0;
                _bufferLen = _buffer.Length;
                _memoryStart = 0;
                _memoryEnd = _bufferLen;
            }

            _bytesRead = 0;
            _ownsPooledBuffer = false;
            _isMemoryBacked = true;
        }

        public long BytesRead => _isMemoryBacked ? _bufferPos - _memoryStart : _bytesRead;

        public int BufferPos => _bufferPos;
        public int BufferSize => _bufferLen;
        public int RemainingBufferSpace => _bufferLen - _bufferPos;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            ThrowIfDisposed();
            if (_isMemoryBacked)
            {
                if (_bufferPos >= _memoryEnd)
                    ThrowUnexpectedEnd(1, 0, "ReadByte");
                return _buffer[_bufferPos++];
            }

            if (_bufferPos >= _bufferLen)
                FillBuffer(1);
            return _buffer[_bufferPos++];
        }

        public ReadOnlySpan<byte> ReadSpanInline(int length)
        {
            ThrowIfDisposed();
            if (_isMemoryBacked)
            {
                int available = _memoryEnd - _bufferPos;
                if (available < length)
                    ThrowUnexpectedEnd(length, available, "ReadSpanInline");

                var inlineSpan = _buffer.AsSpan(_bufferPos, length);
                _bufferPos += length;
                return inlineSpan;
            }

            if (_bufferLen - _bufferPos < length)
                FillBuffer(length);

            var span = _buffer.AsSpan(_bufferPos, length);
            _bufferPos += length;
            return span;
        }

        public RentedBuffer ReadRentedBuffer(int length)
        {
            ThrowIfDisposed();

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            if (length == 0)
                return RentedBuffer.Empty;

            if (_isMemoryBacked)
            {
                int available = _memoryEnd - _bufferPos;
                if (available < length)
                    ThrowUnexpectedEnd(length, available, "ReadRentedBuffer");

                var inlineSpan = _buffer.AsSpan(_bufferPos, length);
                _bufferPos += length;
                return inlineSpan;
            }

            if (_bufferLen - _bufferPos >= length)
            {
                var span = _buffer.AsSpan(_bufferPos, length);
                _bufferPos += length;
                return span;
            }

            if (length <= _buffer.Length)
            {
                FillBuffer(length);
                var span = _buffer.AsSpan(_bufferPos, length);
                _bufferPos += length;
                return span;
            }

            var rented = RentedBuffer.Rent(length);
            ReadExactly(rented.Array, length);
            return rented;
        }

        public void Skip(int length)
        {
            ThrowIfDisposed();

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            if (_isMemoryBacked)
            {
                int available = _memoryEnd - _bufferPos;
                if (available < length)
                    ThrowUnexpectedEnd(length, available, "Skip");

                _bufferPos += length;
                return;
            }

            while (length > 0)
            {
                int available = _bufferLen - _bufferPos;
                if (available >= length)
                {
                    _bufferPos += length;
                    return;
                }

                length -= available;
                _bytesRead += _bufferLen;
                _bufferPos = 0;
                _bufferLen = 0;

                if (length > _buffer.Length && _stream is not null && _stream.CanSeek)
                {
                    _stream.Seek(length, SeekOrigin.Current);
                    _bytesRead += length;
                    return;
                }

                FillBuffer(1);
            }
        }

        private void FillBuffer(int minimumBytes)
        {
            if (_stream is null)
                throw new InvalidOperationException("Cannot refill a memory-backed reader.");

            int remaining = _bufferLen - _bufferPos;
            if (remaining > 0)
                Array.Copy(_buffer, _bufferPos, _buffer, 0, remaining);

            _bytesRead += _bufferPos;
            _bufferPos = 0;
            _bufferLen = remaining;

            while (_bufferLen < minimumBytes)
            {
                int read = _stream.Read(_buffer, _bufferLen, _buffer.Length - _bufferLen);
                if (read == 0)
                    ThrowUnexpectedEnd(minimumBytes, _bufferLen, "FillBuffer");
                _bufferLen += read;
            }
        }

        private void ReadExactly(byte[] destination, int length)
        {
            if (_stream is null)
                throw new InvalidOperationException("Cannot read exactly from a memory-backed reader stream.");

            int totalRead = 0;

            // First, read from the buffer if there's any data available
            int available = _bufferLen - _bufferPos;
            if (available > 0)
            {
                int toCopy = Math.Min(available, length);
                Array.Copy(_buffer, _bufferPos, destination, 0, toCopy);
                _bufferPos += toCopy;
                totalRead += toCopy;
            }

            _bytesRead += _bufferPos;
            _bufferPos = 0;
            _bufferLen = 0;

            while (totalRead < length)
            {
                int read = _stream.Read(destination, totalRead, length - totalRead);
                if (read == 0)
                    ThrowUnexpectedEnd(length, totalRead, "ReadExactly");
                totalRead += read;
                _bytesRead += read;
            }
        }

        private static void ThrowUnexpectedEnd(int expectedBytes, int actualBytes, string operation)
        {
            var ex = new EndOfStreamException("Unexpected end of stream.");
            ex.Data["expectedBytes"] = expectedBytes;
            ex.Data["actualBytes"] = actualBytes;
            ex.Data["operation"] = operation;
            throw ex;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_ownsPooledBuffer && _buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null!;
            }

            if (_stream is not null && !_leaveOpen)
                _stream.Dispose();

            _disposed = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BufferReaderStream));
        }
    }
}
