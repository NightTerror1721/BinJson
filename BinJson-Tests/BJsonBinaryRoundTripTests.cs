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
                0x92, (byte)'i', (byte)'d', 0x01,
                0x92, (byte)'i', (byte)'d', 0x02
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
        public void Serialize_StringArray_DoesNotUsePackedArray()
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

            Assert.NotEqual((byte)BJsonValueTypeCode.PackedArray, bytes[0]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Serialize_BinaryArray_DoesNotUsePackedArray()
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

            Assert.NotEqual((byte)BJsonValueTypeCode.PackedArray, bytes[0]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Serialize_StringLength_32_UsesString8()
        {
            string text = new string('a', 32);
            var value = BJsonValue.Create(text);

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions
            {
                EnableStringTable = false,
                EnablePackedArrays = false,
            });

            Assert.Equal((byte)BJsonValueTypeCode.String8, bytes[0]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Serialize_StringLength_256_UsesString16()
        {
            string text = new string('b', 256);
            var value = BJsonValue.Create(text);

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions
            {
                EnableStringTable = false,
                EnablePackedArrays = false,
            });

            Assert.Equal((byte)BJsonValueTypeCode.String16, bytes[0]);
            Assert.Equal(0x00, bytes[1]);
            Assert.Equal(0x01, bytes[2]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Serialize_StringLength_65536_UsesString32()
        {
            string text = new string('c', 65536);
            var value = BJsonValue.Create(text);

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions
            {
                EnableStringTable = false,
                EnablePackedArrays = false,
            });

            Assert.Equal((byte)BJsonValueTypeCode.String32, bytes[0]);
            Assert.Equal(0x00, bytes[1]);
            Assert.Equal(0x00, bytes[2]);
            Assert.Equal(0x01, bytes[3]);
            Assert.Equal(0x00, bytes[4]);
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

            Assert.DoesNotContain((byte)BJsonValueTypeCode.PackedArray, bytes);
            Assert.Contains((byte)BJsonValueTypeCode.StringRef, bytes);

            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Deserialize_PackedArray_WithStringElementType_Throws()
        {
            byte[] payload =
            {
                (byte)BJsonValueTypeCode.PackedArray,
                (byte)BJsonValueTypeCode.String8,
                0x01,
                0x01,
                (byte)'a',
            };

            using var stream = new MemoryStream(payload);
            var ex = Assert.Throws<Krampus.BinJson.Error.BJsonBinaryFormatException>(() => BJsonBinaryReader.Deserialize(stream));
            Assert.Contains("Unsupported packed element type code", ex.Message);
        }

        [Fact]
        public void RoundTrip_ObjectKeyLength_127_UsesSingleByteVarUInt()
        {
            string key = new string('k', 127);
            var value = BJsonValue.Create(new BJsonObject { [key] = 1 });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions { EnableStringTable = false, EnablePackedArrays = false });

            Assert.Equal(0xC1, bytes[0]);
            Assert.Equal((byte)BJsonValueTypeCode.String8, bytes[1]);
            Assert.Equal(0x7F, bytes[2]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void RoundTrip_ObjectKeyLength_128_UsesString8()
        {
            string key = new string('k', 128);
            var value = BJsonValue.Create(new BJsonObject { [key] = 1 });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions { EnableStringTable = false, EnablePackedArrays = false });

            Assert.Equal(0xC1, bytes[0]);
            Assert.Equal((byte)BJsonValueTypeCode.String8, bytes[1]);
            Assert.Equal(0x80, bytes[2]);
            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void RoundTrip_ObjectKeys_CanUseStringRef_WhenStringTableEnabled()
        {
            var value = BJsonValue.Create(new BJsonArray
            {
                BJsonValue.Create(new BJsonObject { ["id"] = 1 }),
                BJsonValue.Create(new BJsonObject { ["id"] = 2 }),
                BJsonValue.Create(new BJsonObject { ["id"] = 3 }),
                BJsonValue.Create(new BJsonObject { ["id"] = 4 }),
            });

            byte[] bytes = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions
            {
                EnableStringTable = true,
                EnablePackedArrays = false,
            });

            Assert.Contains((byte)BJsonValueTypeCode.StringTable, bytes);

            int firstObjectIndex = Array.IndexOf(bytes, (byte)0xC1);
            Assert.True(firstObjectIndex >= 0);
            Assert.Equal((byte)BJsonValueTypeCode.StringRef, bytes[firstObjectIndex + 1]);

            var parsed = BJsonBinaryReader.Deserialize(bytes);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Deserialize_InvalidVarUIntEncoding_Throws()
        {
            byte[] payload =
            {
                (byte)BJsonValueTypeCode.StringRef,
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

        [Fact]
        public void Visit_NestedPayload_ProducesExpectedEvents()
        {
            var value = BJsonValue.Create(new BJsonObject
            {
                ["id"] = 42,
                ["items"] = new BJsonArray { true, false, 7 },
                ["meta"] = new BJsonObject
                {
                    ["name"] = "alice"
                }
            });

            byte[] payload = BJsonBinaryWriter.Serialize(value);
            var visitor = new RecordingBinaryVisitor();

            BJsonBinaryReader.Visit(payload, visitor);

            Assert.Equal(new[]
            {
                "doc:start",
                "obj:start:3",
                "prop:0:id",
                "uint:42",
                "prop:1:items",
                "arr:start:3:False",
                "bool:True",
                "bool:False",
                "uint:7",
                "arr:end:3:False",
                "prop:2:meta",
                "obj:start:1",
                "prop:0:name",
                "str:alice",
                "obj:end:1",
                "obj:end:3",
                "doc:end"
            }, visitor.Events);
        }

        [Fact]
        public void Visit_RepeatedPayload_AllocatesLessThanDomParse()
        {
            var payload = BJsonBinaryWriter.Serialize(BJsonValue.Create(new BJsonObject
            {
                ["items"] = new BJsonArray
                {
                    new BJsonObject { ["kind"] = "entity", ["zone"] = "overworld", ["name"] = "npc_0" },
                    new BJsonObject { ["kind"] = "entity", ["zone"] = "overworld", ["name"] = "npc_1" },
                    new BJsonObject { ["kind"] = "entity", ["zone"] = "overworld", ["name"] = "npc_2" },
                    new BJsonObject { ["kind"] = "entity", ["zone"] = "overworld", ["name"] = "npc_3" },
                }
            }));

            long domBytes = MeasureAllocatedBytes(() =>
            {
                var parsed = BJsonBinaryReader.Deserialize(payload);
                _ = parsed.ObjectValue.Count;
            }, iterations: 200);

            long visitorBytes = MeasureAllocatedBytes(() =>
            {
                var visitor = new CountingBinaryVisitor();
                BJsonBinaryReader.Visit(payload, visitor);
                _ = visitor.ScalarCount;
            }, iterations: 200);

            Assert.True(visitorBytes < domBytes, $"Expected visitor allocation ({visitorBytes}) to be lower than DOM allocation ({domBytes}).");
        }

        [Fact]
        public void TryReadRootObjectProperty_ReturnsRequestedValue_WithoutFullDom()
        {
            var payload = BJsonBinaryWriter.Serialize(BJsonValue.Create(new BJsonObject
            {
                ["header"] = "v1",
                ["config"] = new BJsonObject
                {
                    ["enabled"] = true,
                    ["retries"] = 3,
                },
                ["items"] = new BJsonArray { 1, 2, 3 },
            }));

            bool found = BJsonBinaryReader.TryReadRootObjectProperty(payload, "config", out var value);

            Assert.True(found);
            Assert.True(value.IsObject);
            Assert.True(value.ObjectValue.TryGetBool("enabled", out var enabled));
            Assert.True(enabled);
            Assert.True(value.ObjectValue.TryGetInt("retries", out var retries));
            Assert.Equal(3, retries);
        }

        [Fact]
        public void TryReadRootObjectProperty_MissingValue_ReturnsFalse()
        {
            var payload = BJsonBinaryWriter.Serialize(BJsonValue.Create(new BJsonObject
            {
                ["id"] = 1,
                ["name"] = "alice",
            }));

            bool found = BJsonBinaryReader.TryReadRootObjectProperty(payload, "missing", out var value);

            Assert.False(found);
            Assert.True(value.IsNull);
        }

        [Fact]
        public void TryReadRootObjectProperty_NonObjectRoot_Throws()
        {
            byte[] payload = BJsonBinaryWriter.Serialize(BJsonValue.Create(new BJsonArray { 1, 2, 3 }));

            var ex = Assert.Throws<Krampus.BinJson.Error.BJsonBinaryFormatException>(() => BJsonBinaryReader.TryReadRootObjectProperty(payload, "id", out _));
            Assert.Contains("Root value is not an object", ex.Message);
        }

        [Fact]
        public void ReadRootObjectProperties_ReturnsOnlyRequestedKeys_InSinglePass()
        {
            var payload = BJsonBinaryWriter.Serialize(BJsonValue.Create(new BJsonObject
            {
                ["id"] = 7,
                ["name"] = "alice",
                ["meta"] = new BJsonObject { ["active"] = true },
                ["tags"] = new BJsonArray { "a", "b" }
            }));

            var selected = BJsonBinaryReader.ReadRootObjectProperties(payload, new[] { "name", "meta", "missing", "name" });

            Assert.Equal(2, selected.Count);
            Assert.True(selected.TryGetString("name", out var name));
            Assert.Equal("alice", name);
            Assert.True(selected.TryGetObject("meta", out var meta));
            Assert.True(meta.TryGetBool("active", out var active));
            Assert.True(active);
            Assert.False(selected.ContainsKey("missing"));
        }

        [Fact]
        public void ReadRootObjectProperties_HandlesStringRefKeys()
        {
            var value = BJsonValue.Create(new BJsonObject
            {
                ["kind"] = "entity",
                ["name"] = "alpha",
                ["details"] = new BJsonObject
                {
                    ["kind"] = "child",
                    ["name"] = "beta"
                }
            });

            byte[] payload = BJsonBinaryWriter.Serialize(value, new BJsonBinaryWriterOptions
            {
                EnableStringTable = true,
                EnablePackedArrays = true,
            });

            var selected = BJsonBinaryReader.ReadRootObjectProperties(payload, new[] { "kind", "name" });

            Assert.Equal(2, selected.Count);
            Assert.True(selected.TryGetString("kind", out var kind));
            Assert.Equal("entity", kind);
            Assert.True(selected.TryGetString("name", out var name));
            Assert.Equal("alpha", name);
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

        private static long MeasureAllocatedBytes(Action action, int iterations)
        {
            for (int i = 0; i < 8; i++)
                action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long start = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
                action();
            long end = GC.GetAllocatedBytesForCurrentThread();
            return end - start;
        }

        private sealed class RecordingBinaryVisitor : BJsonBinaryVisitor
        {
            public string[] Events => _events.ToArray();

            private readonly System.Collections.Generic.List<string> _events = new System.Collections.Generic.List<string>();

            public override void OnDocumentStart() => _events.Add("doc:start");
            public override void OnDocumentEnd() => _events.Add("doc:end");
            public override void OnBoolean(bool value) => _events.Add($"bool:{value}");
            public override void OnUnsignedInteger(ulong value) => _events.Add($"uint:{value}");
            public override void OnSignedInteger(long value) => _events.Add($"int:{value}");
            public override void OnFloat(double value) => _events.Add($"float:{value}");
            public override void OnString(string value) => _events.Add($"str:{value}");
            public override void OnArrayStart(int count, bool isPacked) => _events.Add($"arr:start:{count}:{isPacked}");
            public override void OnArrayEnd(int count, bool isPacked) => _events.Add($"arr:end:{count}:{isPacked}");
            public override void OnObjectStart(int count) => _events.Add($"obj:start:{count}");
            public override void OnObjectProperty(string propertyName, int index) => _events.Add($"prop:{index}:{propertyName}");
            public override void OnObjectEnd(int count) => _events.Add($"obj:end:{count}");
        }

        private sealed class CountingBinaryVisitor : BJsonBinaryVisitor
        {
            public int ScalarCount { get; private set; }

            public override void OnNull() => ScalarCount++;
            public override void OnBoolean(bool value) => ScalarCount++;
            public override void OnSignedInteger(long value) => ScalarCount++;
            public override void OnUnsignedInteger(ulong value) => ScalarCount++;
            public override void OnFloat(double value) => ScalarCount++;
            public override void OnString(string value) => ScalarCount++;
            public override void OnBinary(ReadOnlySpan<byte> data) => ScalarCount++;
        }
    }
}
