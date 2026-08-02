using System;
using System.IO;
using Krampus.BinJson;
using Krampus.BinJson.Text;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonTextRoundTripTests
    {
        [Fact]
        public void Serialize_Primitives_ProducesStandardJson()
        {
            Assert.Equal("null", BJsonTextWriter.Serialize(BJsonValue.Null));
            Assert.Equal("true", BJsonTextWriter.Serialize(BJsonValue.True));
            Assert.Equal("false", BJsonTextWriter.Serialize(BJsonValue.False));
            Assert.Equal("42", BJsonTextWriter.Serialize(BJsonValue.Create(42)));
            Assert.Equal("3.5", BJsonTextWriter.Serialize(BJsonValue.Create(3.5)));
            Assert.Equal("\"hello\"", BJsonTextWriter.Serialize(BJsonValue.Create("hello")));
        }

        [Fact]
        public void Deserialize_Primitives_ParsesStandardJson()
        {
            Assert.True(BJsonTextReader.Deserialize("null").IsNull);
            Assert.Equal(BJsonValue.True, BJsonTextReader.Deserialize("true"));
            Assert.Equal(BJsonValue.False, BJsonTextReader.Deserialize("false"));
            Assert.Equal(BJsonValue.Create(42L), BJsonTextReader.Deserialize("42"));
            Assert.Equal(BJsonValue.Create(3.5), BJsonTextReader.Deserialize("3.5"));
            Assert.Equal(BJsonValue.Create("hello"), BJsonTextReader.Deserialize("\"hello\""));
        }

        [Fact]
        public void RoundTrip_Array_PreservesStructure()
        {
            var array = new BJsonArray();
            array.Add(1);
            array.Add("two");
            array.Add(false);

            var original = BJsonValue.Create(array);
            var json = BJsonTextWriter.Serialize(original);
            var parsed = BJsonTextReader.Deserialize(json);

            Assert.Equal(original, parsed);
            Assert.Equal("[1,\"two\",false]", json);
        }

        [Fact]
        public void RoundTrip_Object_PreservesStructure()
        {
            var obj = new BJsonObject();
            obj.Add("id", 42);
            obj.Add("name", "alice");
            obj.Add("active", true);

            var nested = new BJsonArray();
            nested.Add(1);
            nested.Add(2);
            obj.Add("items", nested);

            var original = BJsonValue.Create(obj);
            var json = BJsonTextWriter.Serialize(original);
            var parsed = BJsonTextReader.Deserialize(json);

            Assert.True(original.Equals(parsed));
            Assert.Contains("\"id\":42", json);
            Assert.Contains("\"name\":\"alice\"", json);
            Assert.Contains("\"active\":true", json);
            Assert.Contains("\"items\":[1,2]", json);
        }

        [Fact]
        public void Serialize_String_EscapesSpecialCharacters()
        {
            var value = BJsonValue.Create("line1\nline2\t\"quoted\"");

            var json = BJsonTextWriter.Serialize(value);
            var parsed = BJsonTextReader.Deserialize(json);

            Assert.Contains("\\n", json);
            Assert.Contains("\\t", json);
            Assert.Contains("\\\"quoted\\\"", json);
            Assert.Equal(value, parsed);
        }

        [Fact]
        public void Deserialize_Stream_ParsesJson()
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{\"value\":123,\"name\":\"test\"}"));

            var result = BJsonTextReader.Deserialize(stream);

            Assert.True(result.IsObject);
            Assert.True(result.ObjectValue.TryGetInt("value", out var value));
            Assert.Equal(123, value);
            Assert.True(result.ObjectValue.TryGetString("name", out var name));
            Assert.Equal("test", name);
        }

        [Fact]
        public void Serialize_TextWriter_WritesCompactJson()
        {
            var value = BJsonValue.Create(new BJsonObject
            {
                ["a"] = BJsonValue.Create(1),
                ["b"] = BJsonValue.Create("x")
            });

            using var writer = new StringWriter();
            BJsonTextWriter.Serialize(writer, value, leaveOpen: true);

            var json = writer.ToString();
            Assert.Contains("\"a\":1", json);
            Assert.Contains("\"b\":\"x\"", json);
            Assert.DoesNotContain(Environment.NewLine, json);
        }

        [Fact]
        public void Serialize_NaN_Throws()
        {
            var value = BJsonValue.Create(double.NaN);

            Assert.Throws<InvalidOperationException>(() => BJsonTextWriter.Serialize(value));
        }

        [Fact]
        public void Serialize_Binary_WithoutAllowBinaryAsBase64_Throws()
        {
            var binary = new BJsonBinary(new byte[] { 1, 2, 3, 4 });
            var value = BJsonValue.Create(binary);

            var ex = Assert.Throws<InvalidOperationException>(() => BJsonTextWriter.Serialize(value));
            Assert.Contains("Binary values are not allowed", ex.Message);
            Assert.Contains("AllowBinaryAsBase64", ex.Message);
        }

        [Fact]
        public void Serialize_Binary_WithAllowBinaryAsBase64_SerializesAsBase64()
        {
            var binary = new BJsonBinary(new byte[] { 1, 2, 3, 4 });
            var value = BJsonValue.Create(binary);
            var options = new BJsonTextWriterOptions { AllowBinaryAsBase64 = true };

            var json = BJsonTextWriter.Serialize(value, options);

            Assert.Equal("\"AQIDBA==\"", json);
        }

        [Fact]
        public void Serialize_WithIndented_ProducesPrettyPrintedJson()
        {
            var obj = new BJsonObject
            {
                ["name"] = BJsonValue.Create("Alice"),
                ["age"] = BJsonValue.Create(30),
                ["active"] = BJsonValue.True
            };
            var value = BJsonValue.Create(obj);
            var options = new BJsonTextWriterOptions { Indented = true, IndentSize = 2 };

            var json = BJsonTextWriter.Serialize(value, options);

            Assert.Contains(Environment.NewLine, json);
            Assert.Contains("  \"name\": \"Alice\"", json);
            Assert.Contains("  \"age\": 30", json);
            Assert.Contains("  \"active\": true", json);
        }

        [Fact]
        public void Serialize_Array_WithIndented_ProducesPrettyPrintedJson()
        {
            var array = new BJsonArray();
            array.Add(1);
            array.Add(2);
            array.Add(3);
            var value = BJsonValue.Create(array);
            var options = new BJsonTextWriterOptions { Indented = true, IndentSize = 2 };

            var json = BJsonTextWriter.Serialize(value, options);

            Assert.Contains(Environment.NewLine, json);
            Assert.Contains("  1", json);
            Assert.Contains("  2", json);
            Assert.Contains("  3", json);
        }

        [Fact]
        public void Serialize_NaN_WithSkipValidation_DoesNotThrow()
        {
            var value = BJsonValue.Create(double.NaN);
            var options = new BJsonTextWriterOptions { SkipValidation = true };

            var json = BJsonTextWriter.Serialize(value, options);

            Assert.NotEmpty(json);
        }

        [Fact]
        public void Serialize_Infinity_WithoutSkipValidation_Throws()
        {
            var value = BJsonValue.Create(double.PositiveInfinity);

            Assert.Throws<InvalidOperationException>(() => BJsonTextWriter.Serialize(value));
        }

        [Fact]
        public void RoundTrip_Binary_WithAllowBinaryAsBase64_PreservesData()
        {
            var original = new BJsonBinary(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF });
            var value = BJsonValue.Create(original);
            var options = new BJsonTextWriterOptions { AllowBinaryAsBase64 = true };

            var json = BJsonTextWriter.Serialize(value, options);
            var parsed = BJsonTextReader.Deserialize(json);

            Assert.True(parsed.IsString);
            var base64 = parsed.StringValue;
            var decoded = Convert.FromBase64String(base64);
            Assert.Equal(original.AsSpan().ToArray(), decoded);
        }

        [Fact]
        public void Serialize_PrettyPrint_StaticProperty_ProducesIndentedOutput()
        {
            var array = new BJsonArray { 1, 2, 3 };
            var value = BJsonValue.Create(array);

            var json = BJsonTextWriter.Serialize(value, BJsonTextWriterOptions.PrettyPrint);

            Assert.Contains(Environment.NewLine, json);
        }

        [Fact]
        public void Deserialize_WithLineComments_AndAllowComments_ParsesJson()
        {
            const string json = "// header\n{\n// id comment\n\"id\": 42,\n\"name\": \"alice\" // trailing comment\n}";

            var value = BJsonTextReader.Deserialize(json, new BJsonTextReaderOptions { AllowComments = true });

            Assert.True(value.IsObject);
            Assert.Equal(BJsonValue.Create(42), value.ObjectValue["id"]);
            Assert.Equal(BJsonValue.Create("alice"), value.ObjectValue["name"]);
        }

        [Fact]
        public void Deserialize_WithBlockComments_AndAllowComments_ParsesJson()
        {
            const string json = "/* header */ { /* before key */ \"enabled\" : true, \"count\" : /* inline */ 3 }";

            var value = BJsonTextReader.Deserialize(json, new BJsonTextReaderOptions { AllowComments = true });

            Assert.True(value.IsObject);
            Assert.Equal(BJsonValue.True, value.ObjectValue["enabled"]);
            Assert.Equal(BJsonValue.Create(3), value.ObjectValue["count"]);
        }

        [Fact]
        public void Deserialize_WithComments_DisallowedByDefault_Throws()
        {
            const string json = "// header\n{\"id\":1}";

            Assert.Throws<FormatException>(() => BJsonTextReader.Deserialize(json));
        }

        [Fact]
        public void Facade_Parse_WithAllowComments_ParsesJson()
        {
            const string json = "{/*x*/\"name\":\"hero\"}";

            var value = BJson.Parse(json, new BJsonTextReaderOptions { AllowComments = true });

            Assert.True(value.IsObject);
            Assert.Equal(BJsonValue.Create("hero"), value.ObjectValue["name"]);
        }
    }
}
