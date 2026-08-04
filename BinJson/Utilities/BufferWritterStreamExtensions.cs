#nullable enable

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Krampus.BinJson.Utilities
{
    public static class BufferWritterStreamExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16LE(this BufferWriterStream stream, short value)
        {
            Span<byte> span = stream.GetSpan(sizeof(short));
            BinaryPrimitives.WriteInt16LittleEndian(span, value);
            stream.Advance(sizeof(short));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16LE(this BufferWriterStream stream, ushort value)
        {
            Span<byte> span = stream.GetSpan(sizeof(ushort));
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);
            stream.Advance(sizeof(ushort));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32LE(this BufferWriterStream stream, int value)
        {
            Span<byte> span = stream.GetSpan(sizeof(int));
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
            stream.Advance(sizeof(int));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32LE(this BufferWriterStream stream, uint value)
        {
            Span<byte> span = stream.GetSpan(sizeof(uint));
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
            stream.Advance(sizeof(uint));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64LE(this BufferWriterStream stream, long value)
        {
            Span<byte> span = stream.GetSpan(sizeof(long));
            BinaryPrimitives.WriteInt64LittleEndian(span, value);
            stream.Advance(sizeof(long));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64LE(this BufferWriterStream stream, ulong value)
        {
            Span<byte> span = stream.GetSpan(sizeof(ulong));
            BinaryPrimitives.WriteUInt64LittleEndian(span, value);
            stream.Advance(sizeof(ulong));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingleLE(this BufferWriterStream stream, float value)
        {
            Span<byte> span = stream.GetSpan(sizeof(float));
            int bits = BitConverter.SingleToInt32Bits(value);
            BinaryPrimitives.WriteInt32LittleEndian(span, bits);
            stream.Advance(sizeof(float));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDoubleLE(this BufferWriterStream stream, double value)
        {
            Span<byte> span = stream.GetSpan(sizeof(double));
            long bits = BitConverter.DoubleToInt64Bits(value);
            BinaryPrimitives.WriteInt64LittleEndian(span, bits);
            stream.Advance(sizeof(double));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarUInt(this BufferWriterStream stream, ulong value)
        {
            while (value >= 0x80)
            {
                stream.WriteByte((byte)(value & 0x7F | 0x80));
                value >>= 7;
            }
            stream.WriteByte((byte)value);
        }
    }
}
