#nullable enable

using System;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonParseException : BJsonException
    {
        public BJsonParseException(string message) : base(message)
        {
        }

        public BJsonParseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
