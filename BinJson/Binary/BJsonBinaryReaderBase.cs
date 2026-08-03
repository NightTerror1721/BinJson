#nullable enable

using System;
using System.IO;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Binary
{
    public abstract class BJsonBinaryReaderBase : IDisposable
    {
        protected readonly Stream Stream;
        protected readonly bool LeaveOpen;
        protected readonly BJsonBinaryReaderOptions Options;

        protected BJsonBinaryReaderBase(Stream stream, bool leaveOpen = false, BJsonBinaryReaderOptions? options = null)
        {
            if (stream is null)
                throw new BJsonValidationException("Parameter 'stream' cannot be null.");
            if (!stream.CanRead)
                throw new BJsonValidationException("Stream must be readable.");

            Stream = stream;
            LeaveOpen = leaveOpen;
            Options = options ?? BJsonBinaryReaderOptions.Default;
        }

        public virtual void Dispose()
        {
            if (!LeaveOpen)
                Stream.Dispose();
        }
    }
}
