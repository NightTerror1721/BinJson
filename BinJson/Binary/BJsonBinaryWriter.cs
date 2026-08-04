#nullable enable

using System.IO;
using Krampus.BinJson.Error;

namespace Krampus.BinJson.Binary
{
    public sealed class BJsonBinaryWriter : BJsonBinaryWriterBase
    {
        private readonly BJsonBinaryWriterCore _core;

        public BJsonBinaryWriter(Stream stream, bool leaveOpen = false, BJsonBinaryWriterOptions? options = null)
            : base(stream, leaveOpen, options)
        {
            _core = new BJsonBinaryWriterCore(stream, leaveOpen: true, Options);
        }

        public void Write(BJsonValue value)
        {
            _core.Write(value);
        }

        public void Flush()
        {
            _core.Flush();
        }

        public override void Dispose()
        {
            _core.Dispose();
            base.Dispose();
        }

        public static void Serialize(Stream stream, BJsonValue value, bool leaveOpen = false, BJsonBinaryWriterOptions? options = null)
        {
            BJsonBinaryWriter? writer = null;
            bool completed = false;
            try
            {
                writer = new BJsonBinaryWriter(stream, leaveOpen, options);
                writer.Write(value);
                writer.Flush();
                completed = true;
            }
            catch (BJsonSerializationException ex) when (ex.Operation == "Flush")
            {
                throw new BJsonSerializationException(
                    "Failed to serialize BinJson value to binary format.",
                    byteOffset: ex.ByteOffset,
                    operation: "WriteValue",
                    documentPath: ex.DocumentPath,
                    errorCode: ex.ErrorCodeValue ?? BJsonErrorCode.BinarySerializationError,
                    innerException: ex.InnerException ?? ex,
                    details: ex.Details);
            }
            finally
            {
                if (writer is not null)
                {
                    try
                    {
                        writer.Dispose();
                    }
                    catch when (!completed)
                    {
                        // Preserve the original write/flush failure when dispose also fails.
                    }
                }
            }
        }

        public static byte[] Serialize(BJsonValue value, BJsonBinaryWriterOptions? options = null)
        {
            using var stream = new MemoryStream();
            BJsonBinaryWriter? writer = null;
            bool completed = false;
            try
            {
                writer = new BJsonBinaryWriter(stream, leaveOpen: true, options);
                writer.Write(value);
                writer.Flush();
                completed = true;
                return stream.ToArray();
            }
            catch (BJsonSerializationException ex) when (ex.Operation == "Flush")
            {
                throw new BJsonSerializationException(
                    "Failed to serialize BinJson value to binary format.",
                    byteOffset: ex.ByteOffset,
                    operation: "WriteValue",
                    documentPath: ex.DocumentPath,
                    errorCode: ex.ErrorCodeValue ?? BJsonErrorCode.BinarySerializationError,
                    innerException: ex.InnerException ?? ex,
                    details: ex.Details);
            }
            finally
            {
                if (writer is not null)
                {
                    try
                    {
                        writer.Dispose();
                    }
                    catch when (!completed)
                    {
                        // Preserve the original write/flush failure when dispose also fails.
                    }
                }
            }
        }
    }
}
