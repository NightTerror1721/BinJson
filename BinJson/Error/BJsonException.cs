#nullable enable

using System;

namespace Krampus.BinJson.Error
{
    public class BJsonException : Exception
    {
        public BJsonException()
        {
        }

        public BJsonException(string message) : base(message)
        {
        }

        public BJsonException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
