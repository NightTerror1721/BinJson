#nullable enable

using System;
using System.Buffers;
using System.IO;

namespace Krampus.BinJson.Utilities
{
    public sealed class BufferWriterStream : IDisposable
    {
        public const int DefaultBufferSize = 8192; // 8 KB
        private const int MaxBufferSize = 1024 * 1024; // 1 MB

        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private byte[] _buffer;
        private int _bufferPos;
        private long _bytesWritten;
        private bool _disposed;

        public BufferWriterStream(Stream stream, bool leaveOpen = false, int bufferSize = DefaultBufferSize)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _leaveOpen = leaveOpen;
            _buffer = ArrayPool<byte>.Shared.Rent(Math.Min(bufferSize, MaxBufferSize));
            _bufferPos = 0;
            _bytesWritten = 0;
        }

        public long BytesWritten => _bytesWritten;

        public int BufferPos => _bufferPos;
        public int BufferSize => _buffer.Length;
        public int RemainingBufferSpace => _buffer.Length - _bufferPos;

        public void WriteByte(byte value)
        {
            if (_bufferPos >= _buffer.Length)
                FlushBuffer();
            _buffer[_bufferPos++] = value;
        }

        public Span<byte> GetSpan(int sizeHint)
        {
            ThrowIfDisposed();

            if (sizeHint < 0)
                throw new ArgumentOutOfRangeException(nameof(sizeHint));

            if (sizeHint > _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(sizeHint), "sizeHint exceeds the buffer size.");

            if (_buffer.Length - _bufferPos < sizeHint)
                FlushBuffer();
            return _buffer.AsSpan(_bufferPos, _buffer.Length - _bufferPos);
        }

        public Span<byte> GetSpan(int sizeHint, int offset)
        {
            ThrowIfDisposed();

            if (sizeHint < 0)
                throw new ArgumentOutOfRangeException(nameof(sizeHint));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (sizeHint + offset > _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(sizeHint), "sizeHint + offset exceeds the buffer size.");

            if (_buffer.Length - _bufferPos < sizeHint + offset)
                FlushBuffer();
            return _buffer.AsSpan(_bufferPos + offset, _buffer.Length - _bufferPos - offset);
        }

        public void Advance(int count)
        {
            if (count < 0 || _bufferPos + count > _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            _bufferPos += count;
        }

        public void Write(ReadOnlySpan<byte> source)
        {
            while (source.Length > 0)
            {
                if (source.Length > _buffer.Length - _bufferPos)
                    FlushBuffer();

                int toCopy = Math.Min(source.Length, _buffer.Length - _bufferPos);
                source[..toCopy].CopyTo(_buffer.AsSpan(_bufferPos));
                _bufferPos += toCopy;
                source = source[toCopy..];
            }
        }

        public void MoveTo(int sourceOffset, int targetOffset, int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));
            ThrowIfDisposed();

            if (sourceOffset < 0 || sourceOffset + count > _buffer.Length - _bufferPos)
                throw new ArgumentOutOfRangeException(nameof(sourceOffset));

            if (targetOffset < 0 || targetOffset + count > _buffer.Length - _bufferPos)
                throw new ArgumentOutOfRangeException(nameof(targetOffset));

            if (sourceOffset == targetOffset)
                return;

            // Use Array.Copy which correctly handles overlapping ranges within the same array.
            int baseOffset = _bufferPos;
            Array.Copy(_buffer, baseOffset + sourceOffset, _buffer, baseOffset + targetOffset, count);
        }

        public void Flush()
        {
            FlushBuffer();
            _stream.Flush();
        }

        private void FlushBuffer()
        {
            if (_bufferPos > 0)
            {
                _stream.Write(_buffer, 0, _bufferPos);
                _bytesWritten += _bufferPos;
                _bufferPos = 0;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                FlushBuffer();
            }
            finally
            {
                if (_buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = null!;
                }

                if (!_leaveOpen)
                {
                    _stream.Dispose();
                }

                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BufferWriterStream));
        }
    }
}
