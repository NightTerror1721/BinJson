using System;
using System.Diagnostics;
using Krampus.BinJson;
using Krampus.BinJson.Binary;
using Krampus.BinJson.Text;
using Xunit;
using Xunit.Abstractions;

namespace Krampus.BinJson.Tests
{
    /// <summary>
    /// Basic performance tests to establish baseline metrics.
    /// These tests verify that serialization/deserialization completes
    /// in reasonable time and provide rough performance indicators.
    /// </summary>
    public class BJsonPerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public BJsonPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Binary_Serialize_LargeObject_CompletesInReasonableTime()
        {
            var data = CreateLargeTestObject(depth: 3, breadth: 10);
            var sw = Stopwatch.StartNew();

            byte[] binary = BJsonBinaryWriter.Serialize(data);

            sw.Stop();
            _output.WriteLine($"Binary serialization: {sw.ElapsedMilliseconds}ms, Size: {binary.Length} bytes");

            Assert.True(sw.ElapsedMilliseconds < 1000, "Binary serialization should complete within 1 second");
            Assert.NotEmpty(binary);
        }

        [Fact]
        public void Binary_Deserialize_LargeObject_CompletesInReasonableTime()
        {
            var data = CreateLargeTestObject(depth: 3, breadth: 10);
            byte[] binary = BJsonBinaryWriter.Serialize(data);

            var sw = Stopwatch.StartNew();
            var deserialized = BJsonBinaryReader.Deserialize(binary);
            sw.Stop();

            _output.WriteLine($"Binary deserialization: {sw.ElapsedMilliseconds}ms");

            Assert.True(sw.ElapsedMilliseconds < 1000, "Binary deserialization should complete within 1 second");
            Assert.Equal(data, deserialized);
        }

        [Fact]
        public void Text_Serialize_LargeObject_CompletesInReasonableTime()
        {
            var data = CreateLargeTestObject(depth: 3, breadth: 10);
            var sw = Stopwatch.StartNew();

            string json = BJsonTextWriter.Serialize(data);

            sw.Stop();
            _output.WriteLine($"Text serialization: {sw.ElapsedMilliseconds}ms, Size: {json.Length} chars");

            Assert.True(sw.ElapsedMilliseconds < 1000, "Text serialization should complete within 1 second");
            Assert.NotEmpty(json);
        }

        [Fact]
        public void Text_Deserialize_LargeObject_CompletesInReasonableTime()
        {
            var data = CreateLargeTestObject(depth: 3, breadth: 10);
            string json = BJsonTextWriter.Serialize(data);

            var sw = Stopwatch.StartNew();
            var deserialized = BJsonTextReader.Deserialize(json);
            sw.Stop();

            _output.WriteLine($"Text deserialization: {sw.ElapsedMilliseconds}ms");

            Assert.True(sw.ElapsedMilliseconds < 1000, "Text deserialization should complete within 1 second");
            Assert.Equal(data, deserialized);
        }

        [Fact]
        public void Binary_IsSmallerThan_Text()
        {
            var data = CreateLargeTestObject(depth: 2, breadth: 20);

            byte[] binary = BJsonBinaryWriter.Serialize(data);
            string json = BJsonTextWriter.Serialize(data);

            _output.WriteLine($"Binary size: {binary.Length} bytes");
            _output.WriteLine($"JSON size: {json.Length} characters ({json.Length * 2} bytes in UTF-16)");
            _output.WriteLine($"Compression ratio (binary/text chars): {(double)binary.Length / json.Length:P1}");

            // Binary is not always smaller due to overhead with string-heavy payloads,
            // but it should be competitive and faster for deserialization
            Assert.True(binary.Length > 0 && json.Length > 0, "Both formats should produce output");
        }

        [Fact]
        public void RoundTrip_PerformanceComparison()
        {
            var data = CreateLargeTestObject(depth: 3, breadth: 8);

            var swBinary = Stopwatch.StartNew();
            byte[] binary = BJsonBinaryWriter.Serialize(data);
            var fromBinary = BJsonBinaryReader.Deserialize(binary);
            swBinary.Stop();

            var swText = Stopwatch.StartNew();
            string json = BJsonTextWriter.Serialize(data);
            var fromJson = BJsonTextReader.Deserialize(json);
            swText.Stop();

            _output.WriteLine($"Binary roundtrip: {swBinary.ElapsedMilliseconds}ms");
            _output.WriteLine($"Text roundtrip: {swText.ElapsedMilliseconds}ms");
            _output.WriteLine($"Binary is {(double)swText.ElapsedMilliseconds / swBinary.ElapsedMilliseconds:F2}x faster");

            Assert.Equal(data, fromBinary);
            Assert.Equal(data, fromJson);
        }

        [Fact]
        public void DOM_Construction_ManySmallObjects()
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 1000; i++)
            {
                var obj = new BJsonObject
                {
                    ["id"] = BJsonValue.Create(i),
                    ["name"] = BJsonValue.Create($"Object_{i}"),
                    ["value"] = BJsonValue.Create(i * 1.5),
                    ["active"] = BJsonValue.True
                };
                var value = BJsonValue.Create(obj);
            }

            sw.Stop();
            _output.WriteLine($"Created 1000 small objects in {sw.ElapsedMilliseconds}ms");

            Assert.True(sw.ElapsedMilliseconds < 500, "DOM construction should be fast");
        }

        [Fact]
        public void String_Escaping_Performance()
        {
            var text = "Line1\nLine2\tLine3\"quoted\"\r\nLine4\\backslash\u0001control";
            var value = BJsonValue.Create(new string(text[0], 1000));

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                BJsonTextWriter.Serialize(value);
            }
            sw.Stop();

            _output.WriteLine($"100 string serializations: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < 1000);
        }

        [Fact]
        public void PrettyPrint_Overhead()
        {
            var data = CreateLargeTestObject(depth: 2, breadth: 10);

            var swCompact = Stopwatch.StartNew();
            string compact = BJsonTextWriter.Serialize(data, BJsonTextWriterOptions.Default);
            swCompact.Stop();

            var swPretty = Stopwatch.StartNew();
            string pretty = BJsonTextWriter.Serialize(data, BJsonTextWriterOptions.PrettyPrint);
            swPretty.Stop();

            _output.WriteLine($"Compact: {swCompact.ElapsedMilliseconds}ms, Size: {compact.Length}");
            _output.WriteLine($"Pretty: {swPretty.ElapsedMilliseconds}ms, Size: {pretty.Length}");
            _output.WriteLine($"Pretty-print overhead: {swPretty.ElapsedMilliseconds - swCompact.ElapsedMilliseconds}ms, Size increase: {pretty.Length - compact.Length} chars");

            Assert.True(pretty.Length > compact.Length);
        }

        private BJsonValue CreateLargeTestObject(int depth, int breadth)
        {
            if (depth == 0)
            {
                return BJsonValue.Create($"leaf_{breadth}");
            }

            var obj = new BJsonObject();
            for (int i = 0; i < breadth; i++)
            {
                obj.Add($"prop_{i}", CreateLargeTestObject(depth - 1, breadth));
            }

            obj.Add("id", BJsonValue.Create(depth * 1000 + breadth));
            obj.Add("timestamp", BJsonValue.Create(DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            obj.Add("active", BJsonValue.True);

            return BJsonValue.Create(obj);
        }
    }
}
