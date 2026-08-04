#nullable enable

using System;
using System.Collections.Generic;
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

        public void Visit(BJsonBinaryVisitor visitor)
        {
            _core.Visit(visitor);
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

        public static BJsonValue Deserialize(byte[] data, BJsonBinaryReaderOptions? options = null)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            return Deserialize(data.AsMemory(), options);
        }

        public static BJsonValue Deserialize(ReadOnlyMemory<byte> data, BJsonBinaryReaderOptions? options = null)
        {
            using var core = new BJsonBinaryReaderCore(data, options);
            return core.Read();
        }

        public static BJsonValue Deserialize(ReadOnlySpan<byte> data, BJsonBinaryReaderOptions? options = null)
        {
            return Deserialize(data.ToArray().AsMemory(), options);
        }

        public static void Visit(Stream stream, BJsonBinaryVisitor visitor, bool leaveOpen = false, BJsonBinaryReaderOptions? options = null)
        {
            using var reader = new BJsonBinaryReader(stream, leaveOpen, options);
            reader.Visit(visitor);
        }

        public static bool TryReadRootObjectProperty(Stream stream, string propertyName, out BJsonValue value, bool leaveOpen = false, BJsonBinaryReaderOptions? options = null)
        {
            using var reader = new BJsonBinaryReader(stream, leaveOpen, options);
            return reader._core.TryReadRootObjectProperty(propertyName, out value);
        }

        public static BJsonObject ReadRootObjectProperties(Stream stream, IReadOnlyList<string> propertyNames, bool leaveOpen = false, BJsonBinaryReaderOptions? options = null)
        {
            using var reader = new BJsonBinaryReader(stream, leaveOpen, options);
            return reader._core.ReadRootObjectProperties(propertyNames);
        }

        public static void Visit(ReadOnlyMemory<byte> data, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions? options = null)
        {
            using var core = new BJsonBinaryReaderCore(data, options);
            core.Visit(visitor);
        }

        public static bool TryReadRootObjectProperty(ReadOnlyMemory<byte> data, string propertyName, out BJsonValue value, BJsonBinaryReaderOptions? options = null)
        {
            using var core = new BJsonBinaryReaderCore(data, options);
            return core.TryReadRootObjectProperty(propertyName, out value);
        }

        public static BJsonObject ReadRootObjectProperties(ReadOnlyMemory<byte> data, IReadOnlyList<string> propertyNames, BJsonBinaryReaderOptions? options = null)
        {
            using var core = new BJsonBinaryReaderCore(data, options);
            return core.ReadRootObjectProperties(propertyNames);
        }

        public static void Visit(byte[] data, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions? options = null)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            Visit(data.AsMemory(), visitor, options);
        }

        public static bool TryReadRootObjectProperty(byte[] data, string propertyName, out BJsonValue value, BJsonBinaryReaderOptions? options = null)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            return TryReadRootObjectProperty(data.AsMemory(), propertyName, out value, options);
        }

        public static BJsonObject ReadRootObjectProperties(byte[] data, IReadOnlyList<string> propertyNames, BJsonBinaryReaderOptions? options = null)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            return ReadRootObjectProperties(data.AsMemory(), propertyNames, options);
        }

        public static void Visit(ReadOnlySpan<byte> data, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions? options = null)
        {
            Visit(data.ToArray().AsMemory(), visitor, options);
        }

        public static bool TryReadRootObjectProperty(ReadOnlySpan<byte> data, string propertyName, out BJsonValue value, BJsonBinaryReaderOptions? options = null)
        {
            return TryReadRootObjectProperty(data.ToArray().AsMemory(), propertyName, out value, options);
        }

        public static BJsonObject ReadRootObjectProperties(ReadOnlySpan<byte> data, IReadOnlyList<string> propertyNames, BJsonBinaryReaderOptions? options = null)
        {
            return ReadRootObjectProperties(data.ToArray().AsMemory(), propertyNames, options);
        }
    }
}
