#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    [Flags]
    public enum BJsonNumberHandling
    {
        Strict = 0,
        AllowReadingFromString = 1,
        WriteAsString = 2,
        Lossless = 4
    }
}