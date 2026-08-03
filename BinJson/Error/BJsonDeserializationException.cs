#nullable enable

using System;
using System.Collections.Generic;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonDeserializationException : BJsonException
    {
        public long? ByteOffset { get; }

        public string? Operation { get; }

        public BJsonDeserializationException(string message) : base(message)
        {
        }

        public BJsonDeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BJsonDeserializationException(
            string message,
            long? byteOffset = null,
            string? operation = null,
            string? documentPath = null,
            BJsonErrorCode? errorCode = null,
            Exception? innerException = null,
            IReadOnlyDictionary<string, object?>? details = null)
            : base(message, errorCode, documentPath, innerException, details)
        {
            ByteOffset = byteOffset;
            Operation = operation;
        }
    }
}
