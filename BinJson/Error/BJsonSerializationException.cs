#nullable enable

using System;
using System.Collections.Generic;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonSerializationException : BJsonException
    {
        public long? ByteOffset { get; }

        public string? Operation { get; }

        public BJsonSerializationException(string message) : base(message)
        {
        }

        public BJsonSerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BJsonSerializationException(
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
