#nullable enable

using System;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonValidationException : BJsonException
    {
        public BJsonValidationException(string message) : base(message)
        {
        }

        public BJsonValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
