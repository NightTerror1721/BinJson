#nullable enable

using System;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonSerializationException : BJsonException
    {
        public BJsonSerializationException(string message) : base(message)
        {
        }

        public BJsonSerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
