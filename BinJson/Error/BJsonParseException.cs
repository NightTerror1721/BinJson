#nullable enable

using System;
using System.Collections.Generic;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonParseException : BJsonException
    {
        public int? Position { get; }

        public int? Line { get; }

        public int? Column { get; }

        public BJsonParseException(string message) : base(message)
        {
        }

        public BJsonParseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BJsonParseException(
            string message,
            int? position = null,
            int? line = null,
            int? column = null,
            BJsonErrorCode? errorCode = null,
            string? documentPath = null,
            Exception? innerException = null,
            IReadOnlyDictionary<string, object?>? details = null)
            : base(message, errorCode, documentPath, innerException, details)
        {
            Position = position;
            Line = line;
            Column = column;
        }
    }
}
