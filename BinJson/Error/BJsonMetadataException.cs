#nullable enable

using System;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonMetadataException : BJsonException
    {
        public BJsonMetadataException(string message) : base(message)
        {
        }

        public BJsonMetadataException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
