#nullable enable

using System;
using System.Collections.Generic;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonValidationException : BJsonException
    {
        public string? ParameterName { get; }

        public BJsonValidationException(string message) : base(message)
        {
        }

        public BJsonValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BJsonValidationException(
            string message,
            string? parameterName = null,
            BJsonErrorCode? errorCode = null,
            string? documentPath = null,
            Exception? innerException = null,
            IReadOnlyDictionary<string, object?>? details = null)
            : base(message, errorCode, documentPath, innerException, details)
        {
            ParameterName = parameterName;
        }
    }
}
