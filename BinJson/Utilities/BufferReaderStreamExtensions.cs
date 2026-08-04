#nullable enable

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Krampus.BinJson.Utilities
{
    public static class BufferReaderStreamExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16LE(this BufferReaderStream stream)
        {
            var span = stream.ReadSpanInline(sizeof(short));
            return BinaryPrimitives.ReadInt16LittleEndian(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16LE(this BufferReaderStream stream)
        {
            var span = stream.ReadSpanInline(sizeof(ushort));
            return BinaryPrimitives.ReadUInt16LittleEndian(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32LE(this BufferReaderStream stream)
        {
            var span = stream.ReadSpanInline(sizeof(int));
            return BinaryPrimitives.ReadInt32LittleEndian(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32LE(this BufferReaderStream stream)
        {
            var span = stream.ReadSpanInline(sizeof(uint));
            return BinaryPrimitives.ReadUInt32LittleEndian(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64LE(this BufferReaderStream stream)
        {
            var span = stream.ReadSpanInline(sizeof(long));
            return BinaryPrimitives.ReadInt64LittleEndian(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64LE(this BufferReaderStream stream)
        {
            var span = stream.ReadSpanInline(sizeof(ulong));
            return BinaryPrimitives.ReadUInt64LittleEndian(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingleLE(this BufferReaderStream stream)
        {
            var span = stream.ReadSpanInline(sizeof(float));
            return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDoubleLE(this BufferReaderStream stream)
        {
            var span = stream.ReadSpanInline(sizeof(double));
            return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(span));
        }

        public static bool ReadVarUInt(this BufferReaderStream stream, out ulong result)
        {
            ulong tempResult = 0;
            int shift = 0;

            while (true)
            {
                byte b = stream.ReadByte();
                tempResult |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    break;

                shift += 7;
                if (shift >= 64)
                {
                    result = 0;
                    return false; // Overflow
                }
            }
            
            result = tempResult;
            return true;
        }
    }
}
