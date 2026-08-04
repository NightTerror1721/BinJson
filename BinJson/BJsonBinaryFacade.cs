#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Krampus.BinJson.Binary;
using Krampus.BinJson.Error;

namespace Krampus.BinJson
{
    /// <summary>
    /// Specialized facade for binary DOM serialization/deserialization and binary visitor operations.
    /// </summary>
    public static class BJsonBinaryFacade
    {
        public static void Serialize(BJsonValue value, Stream stream, bool leaveOpen = false)
        {
            BJsonBinaryWriter.Serialize(stream, value, leaveOpen);
        }

        public static void Serialize(BJsonValue value, Stream stream, BJsonBinaryWriterOptions options, bool leaveOpen = false)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            BJsonBinaryWriter.Serialize(stream, value, leaveOpen, options);
        }

        public static Task SerializeAsync(BJsonValue value, Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonBinaryWriterAsync.SerializeAsync(stream, value, leaveOpen, cancellationToken);
        }

        public static Task SerializeAsync(BJsonValue value, Stream stream, BJsonBinaryWriterOptions options, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryWriterAsync.SerializeAsync(stream, value, leaveOpen, cancellationToken, options);
        }

        public static BJsonValue Deserialize(Stream stream, bool leaveOpen = false)
        {
            return BJsonBinaryReader.Deserialize(stream, leaveOpen);
        }

        public static BJsonValue Deserialize(Stream stream, BJsonBinaryReaderOptions options, bool leaveOpen = false)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.Deserialize(stream, leaveOpen, options);
        }

        public static Task<BJsonValue> DeserializeAsync(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            return BJsonBinaryReaderAsync.DeserializeAsync(stream, leaveOpen, cancellationToken);
        }

        public static Task<BJsonValue> DeserializeAsync(Stream stream, BJsonBinaryReaderOptions options, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReaderAsync.DeserializeAsync(stream, leaveOpen, cancellationToken, options);
        }

        public static byte[] SerializeToBytes(BJsonValue value)
        {
            return BJsonBinaryWriter.Serialize(value);
        }

        public static byte[] SerializeToBytes(BJsonValue value, BJsonBinaryWriterOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryWriter.Serialize(value, options);
        }

        public static Task<byte[]> SerializeToBytesAsync(BJsonValue value, CancellationToken cancellationToken = default)
        {
            return BJsonBinaryWriterAsync.SerializeAsync(value, cancellationToken);
        }

        public static Task<byte[]> SerializeToBytesAsync(BJsonValue value, BJsonBinaryWriterOptions options, CancellationToken cancellationToken = default)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryWriterAsync.SerializeAsync(value, cancellationToken, options);
        }

        public static BJsonValue Deserialize(ReadOnlySpan<byte> data)
        {
            return BJsonBinaryReader.Deserialize(data);
        }

        public static BJsonValue Deserialize(byte[] data)
        {
            return BJsonBinaryReader.Deserialize(data);
        }

        public static BJsonValue Deserialize(ReadOnlyMemory<byte> data)
        {
            return BJsonBinaryReader.Deserialize(data);
        }

        public static BJsonValue Deserialize(ReadOnlySpan<byte> data, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.Deserialize(data, options);
        }

        public static BJsonValue Deserialize(byte[] data, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.Deserialize(data, options);
        }

        public static BJsonValue Deserialize(ReadOnlyMemory<byte> data, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.Deserialize(data, options);
        }

        public static Task<BJsonValue> DeserializeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            return BJsonBinaryReaderAsync.DeserializeAsync(data, cancellationToken);
        }

        public static Task<BJsonValue> DeserializeAsync(ReadOnlyMemory<byte> data, BJsonBinaryReaderOptions options, CancellationToken cancellationToken = default)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReaderAsync.DeserializeAsync(data, cancellationToken, options);
        }

        public static void VisitBinary(Stream stream, BJsonBinaryVisitor visitor, bool leaveOpen = false)
        {
            BJsonBinaryReader.Visit(stream, visitor, leaveOpen);
        }

        public static void VisitBinary(Stream stream, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions options, bool leaveOpen = false)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            BJsonBinaryReader.Visit(stream, visitor, leaveOpen, options);
        }

        public static void VisitBinary(ReadOnlyMemory<byte> data, BJsonBinaryVisitor visitor)
        {
            BJsonBinaryReader.Visit(data, visitor);
        }

        public static void VisitBinary(byte[] data, BJsonBinaryVisitor visitor)
        {
            BJsonBinaryReader.Visit(data, visitor);
        }

        public static void VisitBinary(ReadOnlyMemory<byte> data, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            BJsonBinaryReader.Visit(data, visitor, options);
        }

        public static void VisitBinary(byte[] data, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            BJsonBinaryReader.Visit(data, visitor, options);
        }

        public static void VisitBinary(ReadOnlySpan<byte> data, BJsonBinaryVisitor visitor)
        {
            BJsonBinaryReader.Visit(data, visitor);
        }

        public static void VisitBinary(ReadOnlySpan<byte> data, BJsonBinaryVisitor visitor, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            BJsonBinaryReader.Visit(data, visitor, options);
        }

        public static bool TryDeserialize(ReadOnlySpan<byte> data, out BJsonValue value)
        {
            try
            {
                value = BJsonBinaryReader.Deserialize(data);
                return true;
            }
            catch
            {
                value = BJsonValue.Null;
                return false;
            }
        }

        public static bool TryDeserialize(byte[] data, out BJsonValue value)
        {
            try
            {
                value = BJsonBinaryReader.Deserialize(data);
                return true;
            }
            catch
            {
                value = BJsonValue.Null;
                return false;
            }
        }

        public static async Task<(bool Success, BJsonValue Value)> TryDeserializeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await BJsonBinaryReaderAsync.DeserializeAsync(data, cancellationToken).ConfigureAwait(false);
                return (true, value);
            }
            catch
            {
                return (false, BJsonValue.Null);
            }
        }

        public static bool TryReadBinaryRootObjectProperty(ReadOnlyMemory<byte> data, string propertyName, out BJsonValue value)
        {
            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value);
        }

        public static bool TryReadBinaryRootObjectProperty(byte[] data, string propertyName, out BJsonValue value)
        {
            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value);
        }

        public static bool TryReadBinaryRootObjectProperty(ReadOnlySpan<byte> data, string propertyName, out BJsonValue value)
        {
            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value);
        }

        public static bool TryReadBinaryRootObjectProperty(ReadOnlyMemory<byte> data, string propertyName, out BJsonValue value, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value, options);
        }

        public static bool TryReadBinaryRootObjectProperty(byte[] data, string propertyName, out BJsonValue value, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value, options);
        }

        public static bool TryReadBinaryRootObjectProperty(ReadOnlySpan<byte> data, string propertyName, out BJsonValue value, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.TryReadRootObjectProperty(data, propertyName, out value, options);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(ReadOnlyMemory<byte> data, IReadOnlyList<string> propertyNames)
        {
            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(byte[] data, IReadOnlyList<string> propertyNames)
        {
            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(ReadOnlySpan<byte> data, IReadOnlyList<string> propertyNames)
        {
            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(ReadOnlyMemory<byte> data, IReadOnlyList<string> propertyNames, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames, options);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(byte[] data, IReadOnlyList<string> propertyNames, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames, options);
        }

        public static BJsonObject ReadBinaryRootObjectProperties(ReadOnlySpan<byte> data, IReadOnlyList<string> propertyNames, BJsonBinaryReaderOptions options)
        {
            if (options is null)
                throw new BJsonValidationException("Parameter 'options' cannot be null.");

            return BJsonBinaryReader.ReadRootObjectProperties(data, propertyNames, options);
        }

        public static void SerializeToFile(string filePath, BJsonValue value)
        {
            ValidateFilePath(filePath);
            using var stream = File.Create(filePath);
            Serialize(value, stream, leaveOpen: false);
        }

        public static async Task SerializeToFileAsync(string filePath, BJsonValue value, CancellationToken cancellationToken = default)
        {
            ValidateFilePath(filePath);
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await SerializeAsync(value, stream, leaveOpen: false, cancellationToken).ConfigureAwait(false);
        }

        public static BJsonValue DeserializeFromFile(string filePath)
        {
            ValidateFilePath(filePath);
            using var stream = File.OpenRead(filePath);
            return Deserialize(stream);
        }

        public static async Task<BJsonValue> DeserializeFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ValidateFilePath(filePath);
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            return await DeserializeAsync(stream, leaveOpen: false, cancellationToken).ConfigureAwait(false);
        }

        private static void ValidateFilePath(string filePath)
        {
            if (filePath is null)
                throw new BJsonValidationException("Parameter 'filePath' cannot be null.");
            if (filePath.Length == 0)
                throw new BJsonValidationException("Parameter 'filePath' cannot be empty.");
        }
    }
}
