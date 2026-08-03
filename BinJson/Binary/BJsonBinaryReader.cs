#nullable enable

using System;
using System.IO;

namespace Krampus.BinJson.Binary
{
    public sealed class BJsonBinaryReader : BJsonBinaryReaderBase
    {
        private readonly BJsonBinaryReaderCore _core;

        public BJsonBinaryReader(Stream stream, bool leaveOpen = false, BJsonBinaryReaderOptions? options = null)
            : base(stream, leaveOpen, options)
        {
            _core = new BJsonBinaryReaderCore(stream, leaveOpen: true, Options);
        }

        public BJsonValue Read()
        {
            return _core.Read();
        }

        public override void Dispose()
        {
            _core.Dispose();
            base.Dispose();
        }

        public static BJsonValue Deserialize(Stream stream, bool leaveOpen = false, BJsonBinaryReaderOptions? options = null)
        {
            using var reader = new BJsonBinaryReader(stream, leaveOpen, options);
            return reader.Read();
        }

        public static BJsonValue Deserialize(ReadOnlySpan<byte> data, BJsonBinaryReaderOptions? options = null)
        {
            using var stream = new MemoryStream(data.ToArray(), writable: false);
            using var reader = new BJsonBinaryReader(stream, leaveOpen: true, options);
            return reader.Read();
        }
    }
}
