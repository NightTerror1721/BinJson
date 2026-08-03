#nullable enable

using System;
using System.Collections.Generic;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonConverterException : BJsonException
    {
        public Type? ConverterType { get; }

        public Type? TargetType { get; }

        public BJsonConverterException(string message) : base(message)
        {
        }

        public BJsonConverterException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BJsonConverterException(
            string message,
            Type? converterType = null,
            Type? targetType = null,
            BJsonErrorCode? errorCode = null,
            string? documentPath = null,
            Exception? innerException = null,
            IReadOnlyDictionary<string, object?>? details = null)
            : base(message, errorCode, documentPath, innerException, details)
        {
            ConverterType = converterType;
            TargetType = targetType;
        }
    }
}
