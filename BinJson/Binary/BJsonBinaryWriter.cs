#nullable enable

using System.IO;

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
            using var writer = new BJsonBinaryWriter(stream, leaveOpen, options);
            writer.Write(value);
            writer.Flush();
        }

        public static byte[] Serialize(BJsonValue value, BJsonBinaryWriterOptions? options = null)
        {
            using var stream = new MemoryStream();
            using var writer = new BJsonBinaryWriter(stream, leaveOpen: true, options);
            writer.Write(value);
            writer.Flush();
            return stream.ToArray();
        }
    }
}
