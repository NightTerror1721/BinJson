#nullable enable

using System;
using System.IO;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Binary
{
    public abstract class BJsonBinaryWriterBase : IDisposable
    {
        protected readonly Stream Stream;
        protected readonly bool LeaveOpen;
        protected readonly BJsonBinaryWriterOptions Options;

        protected BJsonBinaryWriterBase(Stream stream, bool leaveOpen = false, BJsonBinaryWriterOptions? options = null)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");
            if (!stream.CanWrite)
                throw new BJsonValidationException("Stream must be writable.");

            Stream = stream;
            LeaveOpen = leaveOpen;
            Options = options ?? BJsonBinaryWriterOptions.Default;
        }

        public virtual void Dispose()
        {
            if (!LeaveOpen)
                Stream.Dispose();
        }
    }
}
