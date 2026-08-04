#nullable enable

using System.Runtime.CompilerServices;

namespace Krampus.BinJson.Binary
{
    public static class BJsonBinaryTypeSizes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSize(BJsonValueTypeCode type)
        {
            return type switch
            {
                // Fixed-size types
                BJsonValueTypeCode.Int8 => sizeof(sbyte),       // 1 byte
                BJsonValueTypeCode.UInt8 => sizeof(byte),       // 1 byte
                BJsonValueTypeCode.Int16 => sizeof(short),      // 2 bytes
                BJsonValueTypeCode.UInt16 => sizeof(ushort),    // 2 bytes
                BJsonValueTypeCode.Int32 => sizeof(int),        // 4 bytes
                BJsonValueTypeCode.UInt32 => sizeof(uint),      // 4 bytes
                BJsonValueTypeCode.Int64 => sizeof(long),       // 8 bytes
                BJsonValueTypeCode.UInt64 => sizeof(ulong),     // 8 bytes
                BJsonValueTypeCode.Float32 => sizeof(float),    // 4 bytes
                BJsonValueTypeCode.Float64 => sizeof(double),   // 8 bytes

                // Special cases for types that do not have a fixed size
                BJsonValueTypeCode.Null => 0,                   // 0 bytes
                BJsonValueTypeCode.BoolFalse => 0,              // 0 bytes
                BJsonValueTypeCode.BoolTrue => 0,               // 0 bytes

                // For variable-size types, the size is not fixed and depends on the actual data.
                _ => 0
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSize(byte typeCode)
        {
            return GetSize((BJsonValueTypeCode)typeCode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFixedSize(BJsonValueTypeCode type)
        {
            return GetSize(type) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFixedSize(byte typeCode)
        {
            return IsFixedSize((BJsonValueTypeCode)typeCode);
        }
    }
}
