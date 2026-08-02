using System;
using System.IO;
using Krampus.BinJson;
using Krampus.BinJson.Binary;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonBinaryRoundTripTests
    {
        [Fact]
        public void RoundTrip_Primitives_PreservesValues()
        {
            AssertRoundTrip(BJsonValue.Null);
            AssertRoundTrip(BJsonValue.True);
            AssertRoundTrip(BJsonValue.False);
            AssertRoundTrip(BJsonValue.Create(-5));
            AssertRoundTrip(BJsonValue.Create(255UL));
            AssertRoundTrip(BJsonValue.Create(65536UL));
            AssertRoundTrip(BJsonValue.Create(3.5));
            AssertRoundTrip(BJsonValue.Create("hello 👋"));
        }

        [Fact]
        public void RoundTrip_Array_PreservesStructure()
        {
            var array = new BJsonArray();
            array.Add(1);
            array.Add("two");
            array.Add(false);
            array.Add(new BJsonArray { BJsonValue.Create(3), BJsonValue.Create(4) });

            var value = BJsonValue.Create(array);
            var roundTripped = RoundTrip(value);

            Assert.Equal(value, roundTripped);
            Assert.True(roundTripped.IsArray);
            Assert.Equal(4, roundTripped.ArrayValue.Count);
        }

        [Fact]
        public void RoundTrip_Object_PreservesStructure()
        {
            var obj = new BJsonObject();
            obj.Add("id", 42);
            obj.Add("name", "alice");
            obj.Add("enabled", true);

            var nested = new BJsonObject();
            nested.Add("score", 9.5);
            obj.Add("meta", nested);

            var value = BJsonValue.Create(obj);
            var roundTripped = RoundTrip(value);

            Assert.True(value.Equals(roundTripped));
            Assert.True(roundTripped.IsObject);
            Assert.True(roundTripped.ObjectValue.TryGetObject("meta", out var meta));
            Assert.True(meta.TryGetDouble("score", out var score));
            Assert.Equal(9.5, score);
        }

        [Fact]
        public void RoundTrip_Binary_PreservesBytes()
        {
            var value = BJsonValue.Create(new BJsonBinary(new byte[] { 0xFF, 0xAA, 0x55, 0x00 }));

            var roundTripped = RoundTrip(value);

            Assert.True(roundTripped.IsBinary);
            Assert.Equal(value, roundTripped);
            Assert.Equal(4, roundTripped.BinaryValue.Count);
            Assert.Equal(0xFF, roundTripped.BinaryValue[0]);
            Assert.Equal(0xAA, roundTripped.BinaryValue[1]);
            Assert.Equal(0x55, roundTripped.BinaryValue[2]);
            Assert.Equal(0x00, roundTripped.BinaryValue[3]);
        }

        [Fact]
        public void RoundTrip_NegativeZero_PreservesExactFloatBits()
        {
            var value = BJsonValue.Create(-0.0);

            var roundTripped = RoundTrip(value);

            Assert.True(roundTripped.IsFloat);
            Assert.Equal(BitConverter.DoubleToInt64Bits(value.DoubleValue), BitConverter.DoubleToInt64Bits(roundTripped.DoubleValue));
        }

        [Fact]
        public void Deserialize_InvalidTypeCode_Throws()
        {
            using var stream = new MemoryStream(new byte[] { 0x7F });

            Assert.Throws<InvalidDataException>(() => BJsonBinaryReader.Deserialize(stream));
        }

        [Fact]
        public void Deserialize_TruncatedPayload_Throws()
        {
            using var stream = new MemoryStream(new byte[] { 0x0D, 0x05, 0x00, 0x00, 0x00, 0x41, 0x42 });

            Assert.Throws<EndOfStreamException>(() => BJsonBinaryReader.Deserialize(stream));
        }

        [Fact]
        public void Deserialize_DuplicateObjectKeys_Throws()
        {
            byte[] payload =
            {
                0x0F,
                0x02, 0x00, 0x00, 0x00,
                0x02, 0x00, 0x00, 0x00, (byte)'i', (byte)'d', 0x05, 0x01,
                0x02, 0x00, 0x00, 0x00, (byte)'i', (byte)'d', 0x05, 0x02
            };

            using var stream = new MemoryStream(payload);

            Assert.Throws<InvalidDataException>(() => BJsonBinaryReader.Deserialize(stream));
        }

        private static void AssertRoundTrip(BJsonValue value)
        {
            var roundTripped = RoundTrip(value);
            Assert.Equal(value, roundTripped);
            Assert.Equal(value.GetHashCode(), roundTripped.GetHashCode());
        }

        private static BJsonValue RoundTrip(BJsonValue value)
        {
            byte[] bytes = BJsonBinaryWriter.Serialize(value);
            return BJsonBinaryReader.Deserialize(bytes);
        }
    }
}
