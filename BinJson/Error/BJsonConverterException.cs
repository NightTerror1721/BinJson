#nullable enable

using System;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonConverterException : BJsonException
    {
        public BJsonConverterException(string message) : base(message)
        {
        }

        public BJsonConverterException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
