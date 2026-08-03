#nullable enable

using System;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonBinaryFormatException : BJsonException
    {
        public BJsonBinaryFormatException(string message) : base(message)
        {
        }

        public BJsonBinaryFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
