#nullable enable

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
        Null        = 0x00,
        Int8        = 0x01,
        Int16       = 0x02,
        Int32       = 0x03,
        Int64       = 0x04,
        UInt8       = 0x05,
        UInt16      = 0x06,
        UInt32      = 0x07,
        UInt64      = 0x08,
        Float32     = 0x09,
        Float64     = 0x0A,
        BoolTrue    = 0x0B,
        BoolFalse   = 0x0C,
        String      = 0x0D,
        Array       = 0x0E,
        Object      = 0x0F,
        Binary      = 0x10,
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
            BJsonValueTypeCode.String => BJsonValueType.String,
            BJsonValueTypeCode.Array => BJsonValueType.Array,
            BJsonValueTypeCode.Object => BJsonValueType.Object,
            BJsonValueTypeCode.Binary => BJsonValueType.Binary,
            _ => throw new System.InvalidOperationException($"Invalid BJsonValueTypeCode: {code}"),
        };
    }
}
