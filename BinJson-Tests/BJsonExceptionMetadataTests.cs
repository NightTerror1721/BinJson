using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Krampus.BinJson;
using Krampus.BinJson.Binary;
using Krampus.BinJson.Error;
using Krampus.BinJson.Text;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonExceptionMetadataTests
    {
        [Fact]
        public void ParseException_ExposesPositionLineAndColumn()
        {
            string json = "{\n  \"a\": 1,\n  \"b\": ]\n}";

            var ex = Assert.Throws<BJsonParseException>(() => BJsonTextReader.Deserialize(json));

            Assert.NotNull(ex.Position);
            Assert.NotNull(ex.Line);
            Assert.NotNull(ex.Column);
            Assert.Equal(BJsonErrorCode.ParseUnexpectedChar, ex.ErrorCodeValue);
            Assert.Equal("$.b", ex.DocumentPath);
            Assert.True(ex.Line >= 1);
            Assert.True(ex.Column >= 1);
        }

        [Fact]
        public void ParseException_ExposesDetails()
        {
            var ex = Assert.Throws<BJsonParseException>(() => BJsonTextReader.Deserialize("{}x"));

            Assert.Equal(BJsonErrorCode.ParseUnexpectedTrailingChar, ex.ErrorCodeValue);
            Assert.True(ex.Details.ContainsKey("expected"));
            Assert.Equal("end of JSON", ex.Details["expected"] as string);
            Assert.Equal("$", ex.DocumentPath);
        }

        [Fact]
        public void BinaryFormatException_ExposesByteOffsetAndSection()
        {
            using var stream = new MemoryStream(new byte[] { 0x95, 0x41, 0x42 });

            var ex = Assert.Throws<BJsonBinaryFormatException>(() => BJsonBinaryReader.Deserialize(stream));

            Assert.NotNull(ex.ByteOffset);
            Assert.True(ex.ByteOffset >= 0);
            Assert.Equal("ReadExactly", ex.Section);
            Assert.Equal(BJsonErrorCode.BinaryFormatError, ex.ErrorCodeValue);
            Assert.True(ex.Details.ContainsKey("expectedBytes"));
            Assert.True(ex.Details.ContainsKey("actualBytes"));
            Assert.Equal("$", ex.DocumentPath);
        }

        [Fact]
        public void BinarySerializationException_ExposesOperation()
        {
            var value = BJsonValue.Create(new BJsonObject { ["x"] = 1 });
            using var stream = new ThrowingWriteStream();

            var ex = Assert.Throws<BJsonSerializationException>(() => BJsonBinaryWriter.Serialize(stream, value, leaveOpen: true));

            Assert.Equal("WriteValue", ex.Operation);
            Assert.Equal(BJsonErrorCode.BinarySerializationError, ex.ErrorCodeValue);
            Assert.NotNull(ex.ByteOffset);
            Assert.Equal("$", ex.DocumentPath);
        }

        [Fact]
        public void ParseException_ContractSnapshot_IsStable()
        {
            var ex = Assert.Throws<BJsonParseException>(() => BJsonTextReader.Deserialize("{\"a\":[1,]}"));

            var snapshot = BuildSnapshot(ex);
            const string expected = "type=BJsonParseException|code=ParseUnexpectedChar|path=$.a[1]|pos=8|line=1|col=9|details=found=]";
            Assert.Equal(expected, snapshot);
        }

        [Fact]
        public void BinaryFormatException_ContractSnapshot_IsStable()
        {
            using var stream = new MemoryStream(new byte[] { 0xD5, 0x01, 0x91 });

            var ex = Assert.Throws<BJsonBinaryFormatException>(() => BJsonBinaryReader.Deserialize(stream));

            var snapshot = BuildSnapshot(ex);
            Assert.StartsWith("type=BJsonBinaryFormatException|code=BinaryFormatError|path=$|", snapshot);
            Assert.Contains("details=", snapshot);
            Assert.Contains("expectedBytes=", snapshot);
            Assert.Contains("actualBytes=", snapshot);
        }

        [Fact]
        public void BaseException_AcceptsEnumCodeAndPath()
        {
            var ex = new BJsonException(
                "Synthetic error",
                BJsonErrorCode.Unknown,
                documentPath: "$.root",
                details: new Dictionary<string, object?> { ["k"] = "v" });

            Assert.Equal(BJsonErrorCode.Unknown, ex.ErrorCodeValue);
            Assert.Equal("Unknown", ex.ErrorCode);
            Assert.Equal("$.root", ex.DocumentPath);
            Assert.Equal("v", ex.Details["k"] as string);
        }

        private static string BuildSnapshot(BJsonException ex)
        {
            var sb = new StringBuilder();
            sb.Append("type=").Append(ex.GetType().Name);
            sb.Append("|code=").Append(ex.ErrorCode ?? "null");
            sb.Append("|path=").Append(ex.DocumentPath ?? "null");

            if (ex is BJsonParseException parse)
            {
                sb.Append("|pos=").Append(parse.Position?.ToString() ?? "null");
                sb.Append("|line=").Append(parse.Line?.ToString() ?? "null");
                sb.Append("|col=").Append(parse.Column?.ToString() ?? "null");
            }

            var details = string.Join(",", ex.Details.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Key + "=" + (kvp.Value?.ToString() ?? "null")));
            sb.Append("|details=").Append(details);
            return sb.ToString();
        }

        private sealed class ThrowingWriteStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => 0;

            public override long Position
            {
                get => 0;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new IOException("Injected write failure.");
            }

            public override void WriteByte(byte value)
            {
                throw new IOException("Injected write failure.");
            }
        }
    }
}
