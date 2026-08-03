#nullable enable

using System;
using System.Globalization;
using System.IO;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Text
{
    public sealed class BJsonTextWriter : BJsonTextWriterBase
    {
        private readonly BJsonTextWriterCore _core;

        public BJsonTextWriter(TextWriter writer, bool leaveOpen = false)
            : this(writer, BJsonTextWriterOptions.Default, leaveOpen)
        {
        }

        public BJsonTextWriter(TextWriter writer, BJsonTextWriterOptions? options, bool leaveOpen = false)
            : base(writer, options, leaveOpen)
        {
            _core = new BJsonTextWriterCore(Writer, Options);
        }

        public void Write(BJsonValue value)
        {
            try
            {
                _core.Write(value);
            }
            catch (Exception ex) when (!(ex is BJsonException))
            {
                throw new BJsonSerializationException(
                    "Failed to serialize BinJson value to JSON text.",
                    operation: "Write",
                    errorCode: BJsonErrorCode.TextSerializationError,
                    innerException: ex);
            }
        }

        public void Flush()
        {
            try
            {
                _core.Flush();
            }
            catch (Exception ex) when (!(ex is BJsonException))
            {
                throw new BJsonSerializationException(
                    "Failed to flush JSON text writer.",
                    operation: "Flush",
                    errorCode: BJsonErrorCode.TextSerializationError,
                    innerException: ex);
            }
        }

        public static string Serialize(BJsonValue value, BJsonTextWriterOptions? options = null)
        {
            return BJsonTextWriterCore.SerializeToString(value, options);
        }

        public static void Serialize(TextWriter writer, BJsonValue value, BJsonTextWriterOptions? options = null, bool leaveOpen = false)
        {
            using var jsonWriter = new BJsonTextWriter(writer, options, leaveOpen);
            jsonWriter.Write(value);
            jsonWriter.Flush();
        }
    }
}
