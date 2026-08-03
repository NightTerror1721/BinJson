#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Krampus.BinJson.Error
{
    public class BJsonException : Exception
    {
        public string? ErrorCode { get; }

        public BJsonErrorCode? ErrorCodeValue { get; }

        public string? DocumentPath { get; }

        public IReadOnlyDictionary<string, object?> Details { get; }

        public BJsonException()
            : this("BinJson error.", errorCode: null, errorCodeValue: null, documentPath: null, innerException: null, details: null)
        {
        }

        public BJsonException(string message)
            : this(message, errorCode: null, errorCodeValue: null, documentPath: null, innerException: null, details: null)
        {
        }

        public BJsonException(string message, Exception innerException)
            : this(message, errorCode: null, errorCodeValue: null, documentPath: null, innerException: innerException, details: null)
        {
        }

        public BJsonException(string message, string? errorCode = null, Exception? innerException = null, IReadOnlyDictionary<string, object?>? details = null)
            : this(message, errorCode: errorCode, errorCodeValue: null, documentPath: null, innerException: innerException, details: details)
        {
        }

        public BJsonException(string message, BJsonErrorCode? errorCode, string? documentPath = null, Exception? innerException = null, IReadOnlyDictionary<string, object?>? details = null)
            : this(message, errorCode: null, errorCodeValue: errorCode, documentPath: documentPath, innerException: innerException, details: details)
        {
        }

        private BJsonException(
            string message,
            string? errorCode,
            BJsonErrorCode? errorCodeValue,
            string? documentPath,
            Exception? innerException,
            IReadOnlyDictionary<string, object?>? details)
            : base(message, innerException)
        {
            ErrorCodeValue = errorCodeValue;
            ErrorCode = errorCodeValue?.ToString() ?? errorCode;
            DocumentPath = documentPath;
            Details = details is null
                ? new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>())
                : new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(details));
        }
    }
}
