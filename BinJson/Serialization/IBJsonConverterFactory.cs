#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    public interface IBJsonConverterFactory
    {
        bool CanConvert(Type type);

        IBJsonConverter? CreateConverter(Type type);
    }
}