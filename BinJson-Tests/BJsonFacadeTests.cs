using Krampus.BinJson;
using Krampus.BinJson.Binary;
using Krampus.BinJson.Text;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonFacadeTests
    {
        [Fact]
        public void TryParse_ReturnsTrue_ForValidJson()
        {
            var ok = BJson.TryParse("{\"x\":1}", out var value);

            Assert.True(ok);
            Assert.True(value.IsObject);
            Assert.Equal(1, value.ObjectValue["x"].IntValue);
        }

        [Fact]
        public void TryParse_ReturnsFalse_ForInvalidJson()
        {
            var ok = BJson.TryParse("{\"x\":", out var value);

            Assert.False(ok);
            Assert.True(value.IsNull);
        }

        [Fact]
        public void TryParse_WithOptions_AllowsComments()
        {
            var ok = BJson.TryParse("{/*c*/\"x\":2}", new BJsonTextReaderOptions { AllowComments = true }, out var value);

            Assert.True(ok);
            Assert.True(value.IsObject);
            Assert.Equal(2, value.ObjectValue["x"].IntValue);
        }

        [Fact]
        public void TryDeserialize_ReturnsFalse_ForInvalidPayload()
        {
            var ok = BJson.TryDeserialize(new byte[] { 0x8F }, out var value);

            Assert.False(ok);
            Assert.True(value.IsNull);
        }

        [Fact]
        public void Transform_AppliesRecursively()
        {
            var obj = new BJsonObject
            {
                ["a"] = 1,
                ["nested"] = new BJsonArray { 2, 3 }
            };

            var transformed = BJson.Transform(BJsonValue.Create(obj), v =>
            {
                if (v.IsInteger)
                    return BJsonValue.Create(v.IntValue + 10);
                return v;
            });

            Assert.Equal(11, transformed.ObjectValue["a"].IntValue);
            Assert.Equal(12, transformed.ObjectValue["nested"].ArrayValue[0].IntValue);
            Assert.Equal(13, transformed.ObjectValue["nested"].ArrayValue[1].IntValue);
        }

        [Fact]
        public void SyncBinaryFileApis_RoundTrip()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                var original = BJsonValue.Create(new BJsonObject
                {
                    ["id"] = 7,
                    ["name"] = "hero"
                });

                BJson.SerializeToFile(filePath, original);
                var parsed = BJson.DeserializeFromFile(filePath);

                Assert.Equal(original, parsed);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void SyncTextFileApis_RoundTrip()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                var original = BJsonValue.Create(new BJsonObject
                {
                    ["enabled"] = true,
                    ["level"] = 3
                });

                BJson.StringifyToFile(filePath, original);
                var parsed = BJson.ParseFile(filePath);

                Assert.Equal(original, parsed);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public async Task AsyncBinaryFileApis_RoundTrip()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                var original = BJsonValue.Create(new BJsonArray { 1, 2, 3, "x" });

                await BJson.SerializeToFileAsync(filePath, original);
                var parsed = await BJson.DeserializeFromFileAsync(filePath);

                Assert.Equal(original, parsed);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public async Task AsyncTextFileApis_RoundTrip()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                var original = BJsonValue.Create(new BJsonObject
                {
                    ["name"] = "alice",
                    ["score"] = 9.5
                });

                await BJson.StringifyToFileAsync(filePath, original);
                var parsed = await BJson.ParseFileAsync(filePath);

                Assert.Equal(original, parsed);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public async Task BinaryStreamAsync_LeaveOpen_Respected()
        {
            var value = BJsonValue.Create(123);

            using var openStream = new MemoryStream();
            await BJson.SerializeAsync(value, openStream, leaveOpen: true);
            Assert.True(openStream.CanRead);

            var closedStream = new MemoryStream();
            await BJson.SerializeAsync(value, closedStream, leaveOpen: false);
            Assert.False(closedStream.CanRead);
        }

        [Fact]
        public async Task TextWriterAsync_LeaveOpen_Respected()
        {
            var value = BJsonValue.Create(new BJsonObject { ["x"] = 1 });

            var openWriter = new TrackingStringWriter();
            await BJson.StringifyAsync(openWriter, value, leaveOpen: true);
            Assert.False(openWriter.IsDisposed);

            var closedWriter = new TrackingStringWriter();
            await BJson.StringifyAsync(closedWriter, value, leaveOpen: false);
            Assert.True(closedWriter.IsDisposed);
        }

        [Fact]
        public async Task TryDeserializeAsync_ReturnsFalse_ForInvalidPayload()
        {
            var result = await BJson.TryDeserializeAsync(new byte[] { 0x8F });

            Assert.False(result.Success);
            Assert.True(result.Value.IsNull);
        }

        [Fact]
        public void SerializeToBytes_WithWriterOptions_CanDisablePackedArrays()
        {
            var value = BJsonValue.Create(new BJsonArray { true, false, true, false, true });
            var options = new BJsonBinaryWriterOptions { EnablePackedArrays = false, EnableStringTable = true };

            byte[] bytes = BJson.SerializeToBytes(value, options);

            Assert.Equal((byte)(BJsonBinaryTypeRanges.FixArrayMin + 5), bytes[0]);
            var parsed = BJson.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Deserialize_WithReaderOptions_CanCoerceInvalidStringRefToNull()
        {
            byte[] payload = { (byte)BJsonValueTypeCode.StringRef, 0x00 };

            var strictOptions = new BJsonBinaryReaderOptions { InvalidStringRefPolicy = BJsonInvalidStringRefPolicy.Strict };
            Assert.Throws<Krampus.BinJson.Error.BJsonBinaryFormatException>(() => BJson.Deserialize(payload, strictOptions));

            var coerceOptions = new BJsonBinaryReaderOptions { InvalidStringRefPolicy = BJsonInvalidStringRefPolicy.CoerceNull };
            var parsed = BJson.Deserialize(payload, coerceOptions);
            Assert.True(parsed.IsNull);
        }

        [Fact]
        public async Task DeserializeAsync_WithReaderOptions_CanCoerceInvalidStringRefToNull()
        {
            byte[] payload = { (byte)BJsonValueTypeCode.StringRef, 0x00 };
            var coerceOptions = new BJsonBinaryReaderOptions { InvalidStringRefPolicy = BJsonInvalidStringRefPolicy.CoerceNull };

            var parsed = await BJson.DeserializeAsync(new ReadOnlyMemory<byte>(payload), coerceOptions);

            Assert.True(parsed.IsNull);
        }

        [Fact]
        public async Task DeserializeAsync_HeaderAndExtContainer_AreHandled()
        {
            byte[] payload =
            {
                (byte)BJsonValueTypeCode.HeaderMarker,
                (byte)'B', (byte)'J',
                0x01,
                0x00,
                (byte)BJsonValueTypeCode.ExtContainer,
                0x03,
                0xAA, 0xBB, 0xCC,
                (byte)BJsonValueTypeCode.Null,
            };

            var parsed = await BJson.DeserializeAsync(new ReadOnlyMemory<byte>(payload));

            Assert.True(parsed.IsNull);
        }

        private sealed class TrackingStringWriter : StringWriter
        {
            public bool IsDisposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
