#nullable enable

using System;
using System.IO;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Text
{
    public abstract class BJsonTextWriterBase : IDisposable
    {
        protected readonly TextWriter Writer;
        protected readonly bool LeaveOpen;
        protected readonly BJsonTextWriterOptions Options;

        protected BJsonTextWriterBase(TextWriter writer, BJsonTextWriterOptions? options = null, bool leaveOpen = false)
        {
            Writer = writer ?? throw new BJsonValidationException("Parameter 'writer' cannot be null.");
            Options = options ?? BJsonTextWriterOptions.Default;
            LeaveOpen = leaveOpen;
        }

        public virtual void Dispose()
        {
            if (!LeaveOpen)
                Writer.Dispose();
        }
    }
}
