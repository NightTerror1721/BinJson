#nullable enable

using System;
using System.Collections.Generic;

namespace Krampus.BinJson.Error
{
    public sealed class BJsonMetadataException : BJsonException
    {
        public Type? RelatedType { get; }

        public string? MemberName { get; }

        public BJsonMetadataException(string message) : base(message)
        {
        }

        public BJsonMetadataException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BJsonMetadataException(
            string message,
            Type? relatedType = null,
            string? memberName = null,
            BJsonErrorCode? errorCode = null,
            string? documentPath = null,
            Exception? innerException = null,
            IReadOnlyDictionary<string, object?>? details = null)
            : base(message, errorCode, documentPath, innerException, details)
        {
            RelatedType = relatedType;
            MemberName = memberName;
        }
    }
}
