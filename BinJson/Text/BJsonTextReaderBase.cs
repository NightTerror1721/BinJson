#nullable enable

using System;
using System.IO;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Text
{
    public abstract class BJsonTextReaderBase : IDisposable
    {
        protected readonly TextReader Reader;
        protected readonly bool LeaveOpen;
        protected readonly BJsonTextReaderOptions Options;

        protected BJsonTextReaderBase(TextReader reader, BJsonTextReaderOptions? options = null, bool leaveOpen = false)
        {
            Reader = reader ?? throw new BJsonValidationException("Parameter 'reader' cannot be null.");
            Options = options ?? BJsonTextReaderOptions.Default;
            LeaveOpen = leaveOpen;
        }

        public virtual void Dispose()
        {
            if (!LeaveOpen)
                Reader.Dispose();
        }
    }
}
