#nullable enable

using System;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonDeserializationException : BJsonException
    {
        public BJsonDeserializationException(string message) : base(message)
        {
        }

        public BJsonDeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
