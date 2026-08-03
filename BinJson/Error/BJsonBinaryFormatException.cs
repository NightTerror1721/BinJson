#nullable enable

using System;
using System.Collections.Generic;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonBinaryFormatException : BJsonException
    {
        public long? ByteOffset { get; }

        public string? Section { get; }

        public BJsonBinaryFormatException(string message) : base(message)
        {
        }

        public BJsonBinaryFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BJsonBinaryFormatException(
            string message,
            long? byteOffset = null,
            string? section = null,
            string? documentPath = null,
            BJsonErrorCode? errorCode = null,
            Exception? innerException = null,
            IReadOnlyDictionary<string, object?>? details = null)
            : base(message, errorCode, documentPath, innerException, details)
        {
            ByteOffset = byteOffset;
            Section = section;
        }
    }
}
