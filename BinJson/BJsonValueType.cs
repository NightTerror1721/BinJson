#nullable enable

using Krampus.BinJson.Error;

namespace Krampus.BinJson
{
    public enum BJsonValueType
    {
        Null    = 0,
        Integer = 1,
        Float   = 2,
        Boolean = 3,
        String  = 4,
        Array   = 5,
        Object  = 6,
        Binary  = 7
    }

    public enum BJsonValueTypeCode : byte
    {
        Null            = 0x80,
        BoolFalse       = 0x81,
        BoolTrue        = 0x82,
        Int8            = 0x83,
        Int16           = 0x84,
        Int32           = 0x85,
        Int64           = 0x86,
        UInt8           = 0x87,
        UInt16          = 0x88,
        UInt32          = 0x89,
        UInt64          = 0x8A,
        Float32         = 0x8B,
        Float64         = 0x8C,
        VarInt          = 0x8D,
        VarUInt         = 0x8E,
        String8         = 0xD0,
        String16        = 0xD1,
        String32        = 0xD2,
        StringRef       = 0xD3,
        ArrayVar        = 0xD4,
        ObjectVar       = 0xD5,
        PackedArray     = 0xD6,
        Binary          = 0xD7,
        HeaderMarker    = 0xE0,
        StringTable     = 0xE1,
        ExtContainer    = 0xE2,
    }

    public static class BJsonBinaryTypeRanges
    {
        public const byte PositiveFixIntMax = 0x7F;

        public const byte FixStrMin = 0x90;
        public const byte FixStrMax = 0xAF;

        public const byte FixArrayMin = 0xB0;
        public const byte FixArrayMax = 0xBF;

        public const byte FixObjectMin = 0xC0;
        public const byte FixObjectMax = 0xCF;

        public static bool IsPositiveFixInt(byte code) => code <= PositiveFixIntMax;

        public static bool IsFixStr(byte code) => code >= FixStrMin && code <= FixStrMax;

        public static bool IsFixArray(byte code) => code >= FixArrayMin && code <= FixArrayMax;

        public static bool IsFixObject(byte code) => code >= FixObjectMin && code <= FixObjectMax;
    }

    public static class BJsonValueTypeExtensions
    {
        public static BJsonValueType ToType(this BJsonValueTypeCode code) => code switch
        {
            BJsonValueTypeCode.Null => BJsonValueType.Null,
            BJsonValueTypeCode.Int8 or
            BJsonValueTypeCode.Int16 or
            BJsonValueTypeCode.Int32 or
            BJsonValueTypeCode.Int64 or
            BJsonValueTypeCode.UInt8 or
            BJsonValueTypeCode.UInt16 or
            BJsonValueTypeCode.UInt32 or
            BJsonValueTypeCode.UInt64 => BJsonValueType.Integer,
            BJsonValueTypeCode.Float32 or
            BJsonValueTypeCode.Float64 => BJsonValueType.Float,
            BJsonValueTypeCode.BoolTrue or
            BJsonValueTypeCode.BoolFalse => BJsonValueType.Boolean,
            BJsonValueTypeCode.String8 or
            BJsonValueTypeCode.String16 or
            BJsonValueTypeCode.String32 or
            BJsonValueTypeCode.StringRef => BJsonValueType.String,
            BJsonValueTypeCode.ArrayVar or
            BJsonValueTypeCode.PackedArray => BJsonValueType.Array,
            BJsonValueTypeCode.ObjectVar => BJsonValueType.Object,
            BJsonValueTypeCode.Binary => BJsonValueType.Binary,
            _ => throw new BJsonValidationException($"Invalid BJsonValueTypeCode: {code}"),
        };
    }
}
