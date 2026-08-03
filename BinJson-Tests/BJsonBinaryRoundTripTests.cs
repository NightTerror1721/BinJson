using System;
using System.IO;
using System.Text;
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
            using var stream = new MemoryStream(new byte[] { 0x8F });

            var ex = Assert.Throws<Krampus.BinJson.Error.BJsonBinaryFormatException>(() => BJsonBinaryReader.Deserialize(stream));
            Assert.Contains("Invalid BJson type code", ex.Message);
        }

        [Fact]
        public void Deserialize_TruncatedPayload_Throws()
        {
            using var stream = new MemoryStream(new byte[] { 0x95, 0x41, 0x42 });

            var ex = Assert.Throws<Krampus.BinJson.Error.BJsonBinaryFormatException>(() => BJsonBinaryReader.Deserialize(stream));
            Assert.Contains("Unexpected end of stream", ex.Message);
        }

        [Fact]
        public void Deserialize_DuplicateObjectKeys_Throws()
        {
            byte[] payload =
            {
                0xC2,
                0x02, (byte)'i', (byte)'d', 0x01,
                0x02, (byte)'i', (byte)'d', 0x02
            };

            using var stream = new MemoryStream(payload);

            var ex = Assert.Throws<Krampus.BinJson.Error.BJsonBinaryFormatException>(() => BJsonBinaryReader.Deserialize(stream));
            Assert.Contains("Duplicate object key", ex.Message);
        }

        [Fact]
        public void BinaryHelpers_ToArray_CopyTo_AndOperators()
        {
            var left = new BJsonBinary(new byte[] { 1, 2, 3 });
            var right = new BJsonBinary(new byte[] { 4, 5 });

            var combined = left + right;
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, combined.AsSpan().ToArray());

            var copy = left.ToArray();
            copy[0] = 200;
            Assert.Equal(1, left[0]);

            var destination = new byte[5];
            right.CopyTo(destination, 2);
            Assert.Equal(new byte[] { 0, 0, 4, 5, 0 }, destination);

            Assert.True(left == new BJsonBinary(new byte[] { 1, 2, 3 }));
            Assert.True(left != right);
        }

        [Fact]
        public void BinaryCodecs_Base64_Hex_String_RoundTrip()
        {
            var original = new BJsonBinary(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

            var b64 = original.ToBase64();
            var fromB64 = BJsonBinary.FromBase64(b64);
            Assert.Equal(original, fromB64);

            var hex = original.ToHex();
            var fromHex = BJsonBinary.FromHex(hex);
            Assert.Equal(original, fromHex);

            var text = "hello";
            var fromText = BJsonBinary.FromString(text, Encoding.UTF8);
            Assert.Equal(text, fromText.DecodeString(Encoding.UTF8));
        }

        [Fact]
        public void Serialize_BoolArray_UsesPackedArray()
        {
            var value = BJsonValue.Create(new BJsonArray { true, false, true, true, false, false, true, false, true });

            byte[] bytes = BJsonBinaryWriter.Serialize(value);

            Assert.Equal((byte)BJsonValueTypeCode.PackedArray, bytes[0]);
            var roundTrip = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, roundTrip);
        }

        [Fact]
        public void Serialize_NullArray_UsesPackedArray()
        {
            var value = BJsonValue.Create(new BJsonArray { BJsonValue.Null, BJsonValue.Null, BJsonValue.Null, BJsonValue.Null, BJsonValue.Null });

            byte[] bytes = BJsonBinaryWriter.Serialize(value);

            Assert.Equal((byte)BJsonValueTypeCode.PackedArray, bytes[0]);
            var roundTrip = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, roundTrip);
        }

        [Fact]
        public void Serialize_BoolArray_DisablePackedArrays_WritesRegularArray()
        {
            var value = BJsonValue.Create(new BJsonArray { true, false, true, false, true });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions { EnablePackedArrays = false, EnableStringTable = true });

            Assert.Equal((byte)(BJsonBinaryTypeRanges.FixArrayMin + 5), bytes[0]);
            var roundTrip = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, roundTrip);
        }

        [Fact]
        public void Serialize_IntegerArray_UsesPackedArray()
        {
            var value = BJsonValue.Create(new BJsonArray { 300, 301, 302, 303, 304, 305 });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions
            {
                EnableStringTable = false,
                EnablePackedArrays = true,
            });

            Assert.Equal((byte)BJsonValueTypeCode.PackedArray, bytes[0]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Serialize_FloatArray_UsesPackedArray()
        {
            var value = BJsonValue.Create(new BJsonArray { 1.5, 2.5, 3.5, 4.5, 5.5 });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions
            {
                EnableStringTable = false,
                EnablePackedArrays = true,
            });

            Assert.Equal((byte)BJsonValueTypeCode.PackedArray, bytes[0]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Serialize_StringArray_UsesPackedArray()
        {
            string a = new string('a', 40);
            string b = new string('b', 40);
            string c = new string('c', 40);
            var value = BJsonValue.Create(new BJsonArray { a, b, c, a });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions
            {
                EnableStringTable = false,
                EnablePackedArrays = true,
            });

            Assert.Equal((byte)BJsonValueTypeCode.PackedArray, bytes[0]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Serialize_BinaryArray_UsesPackedArray()
        {
            var value = BJsonValue.Create(new BJsonArray
            {
                BJsonValue.Create(new BJsonBinary(new byte[] { 1,2,3,4,5,6,7,8,9,10 })),
                BJsonValue.Create(new BJsonBinary(new byte[] { 11,12,13,14,15,16,17,18,19,20 })),
                BJsonValue.Create(new BJsonBinary(new byte[] { 21,22,23,24,25,26,27,28,29,30 })),
            });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions
            {
                EnableStringTable = false,
                EnablePackedArrays = true,
            });

            Assert.Equal((byte)BJsonValueTypeCode.PackedArray, bytes[0]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Serialize_RepeatedLongStrings_WithStringTableAndPackedArrays_UsesPackedStringRefs()
        {
            string repeated = new string('z', 64);
            var value = BJsonValue.Create(new BJsonArray { repeated, repeated, repeated, repeated, repeated, repeated });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions
            {
                EnableStringTable = true,
                EnablePackedArrays = true,
            });

            Assert.Contains((byte)BJsonValueTypeCode.PackedArray, bytes);
            Assert.Contains((byte)BJsonValueTypeCode.StringRef, bytes);

            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void RoundTrip_ObjectKeyLength_127_UsesSingleByteVarUInt()
        {
            string key = new string('k', 127);
            var value = BJsonValue.Create(new BJsonObject { [key] = 1 });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions { EnableStringTable = false, EnablePackedArrays = false });

            Assert.Equal(0xC1, bytes[0]);
            Assert.Equal(0x7F, bytes[1]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void RoundTrip_ObjectKeyLength_128_UsesTwoByteVarUInt()
        {
            string key = new string('k', 128);
            var value = BJsonValue.Create(new BJsonObject { [key] = 1 });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions { EnableStringTable = false, EnablePackedArrays = false });

            Assert.Equal(0xC1, bytes[0]);
            Assert.Equal(0x80, bytes[1]);
            Assert.Equal(0x01, bytes[2]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Deserialize_InvalidVarUIntEncoding_Throws()
        {
            byte[] payload =
            {
                (byte)BJsonValueTypeCode.String32,
                0x80, 0x80, 0x80, 0x80, 0x80,
                0x80, 0x80, 0x80, 0x80, 0x80,
            };

            using var stream = new MemoryStream(payload);
            var ex = Assert.Throws<Krampus.BinJson.Error.BJsonBinaryFormatException>(() => BJsonBinaryReader.Deserialize(stream));
            Assert.Contains("Invalid VarUInt encoding", ex.Message);
        }

        [Fact]
        public void Deserialize_VarUIntCountExceedsInt32_Throws()
        {
            byte[] payload =
            {
                (byte)BJsonValueTypeCode.Binary,
                0x80, 0x80, 0x80, 0x80, 0x08,
            };

            using var stream = new MemoryStream(payload);
            var ex = Assert.Throws<Krampus.BinJson.Error.BJsonBinaryFormatException>(() => BJsonBinaryReader.Deserialize(stream));
            Assert.Contains("exceeds Int32.MaxValue", ex.Message);
        }

        [Fact]
        public void Deserialize_HeaderWithInvalidVersion_Throws()
        {
            byte[] payload =
            {
                (byte)BJsonValueTypeCode.HeaderMarker,
                (byte)'B', (byte)'J',
                0x02,
                0x00,
                (byte)BJsonValueTypeCode.Null,
            };

            using var stream = new MemoryStream(payload);
            var ex = Assert.Throws<Krampus.BinJson.Error.BJsonBinaryFormatException>(() => BJsonBinaryReader.Deserialize(stream));
            Assert.Contains("Unsupported binary version", ex.Message);
        }

        [Fact]
        public void Deserialize_HeaderWithUnsupportedFlags_Throws()
        {
            byte[] payload =
            {
                (byte)BJsonValueTypeCode.HeaderMarker,
                (byte)'B', (byte)'J',
                0x01,
                0x80,
                (byte)BJsonValueTypeCode.Null,
            };

            using var stream = new MemoryStream(payload);
            var ex = Assert.Throws<Krampus.BinJson.Error.BJsonBinaryFormatException>(() => BJsonBinaryReader.Deserialize(stream));
            Assert.Contains("Unsupported header flags", ex.Message);
        }

        [Fact]
        public void Serialize_RepeatedLongStrings_EmitsHeaderAndStringTable()
        {
            string repeated = new string('x', 64);
            var value = BJsonValue.Create(new BJsonArray { repeated, repeated, repeated, repeated });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions { EnableStringTable = true, EnablePackedArrays = false });

            Assert.Equal((byte)BJsonValueTypeCode.HeaderMarker, bytes[0]);
            bool hasStringTableTag = false;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == (byte)BJsonValueTypeCode.StringTable)
                {
                    hasStringTableTag = true;
                    break;
                }
            }

            Assert.True(hasStringTableTag);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Deserialize_ExtContainerBlock_IsSkipped()
        {
            byte[] payload =
            {
                (byte)BJsonValueTypeCode.ExtContainer,
                0x03,
                0xAA, 0xBB, 0xCC,
                (byte)BJsonValueTypeCode.Null,
            };

            var parsed = BJsonBinaryReader.Deserialize(payload);
            Assert.True(parsed.IsNull);
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
