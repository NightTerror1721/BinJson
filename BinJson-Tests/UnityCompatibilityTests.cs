using System;
using Krampus.BinJson;
using Krampus.BinJson.Binary;
using Krampus.BinJson.Text;
using Xunit;

namespace Krampus.BinJson.Tests
{
    /// <summary>
    /// Tests that validate Unity-compatible usage patterns:
    /// - No System.Text.Json dependency
    /// - Standard .NET Standard 2.1 APIs only
    /// - Safe serialization/deserialization roundtrips
    /// </summary>
    public class UnityCompatibilityTests
    {
        [Fact]
        public void Binary_Serialization_WorksWithoutExternalDependencies()
        {
            var obj = new BJsonObject
            {
                ["playerName"] = BJsonValue.Create("Hero"),
                ["health"] = BJsonValue.Create(100),
                ["position"] = BJsonValue.Create(new BJsonArray { 10.5, 20.0, 30.25 })
            };
            var value = BJsonValue.Create(obj);

            byte[] bytes = BJsonBinaryWriter.Serialize(value);
            var deserialized = BJsonBinaryReader.Deserialize(bytes);

            Assert.Equal(value, deserialized);
        }

        [Fact]
        public void Text_Serialization_WorksWithoutSystemTextJson()
        {
            var obj = new BJsonObject
            {
                ["level"] = BJsonValue.Create(5),
                ["score"] = BJsonValue.Create(12345),
                ["completed"] = BJsonValue.True
            };
            var value = BJsonValue.Create(obj);

            string json = BJsonTextWriter.Serialize(value);
            var deserialized = BJsonTextReader.Deserialize(json);

            Assert.Equal(value, deserialized);
            Assert.Contains("\"level\":5", json);
            Assert.Contains("\"score\":12345", json);
            Assert.Contains("\"completed\":true", json);
        }

        [Fact]
        public void RoundTrip_ComplexObject_PreservesAllData()
        {
            var inventory = new BJsonArray();
            inventory.Add(new BJsonObject
            {
                ["id"] = BJsonValue.Create(1),
                ["name"] = BJsonValue.Create("Sword"),
                ["damage"] = BJsonValue.Create(50)
            });
            inventory.Add(new BJsonObject
            {
                ["id"] = BJsonValue.Create(2),
                ["name"] = BJsonValue.Create("Shield"),
                ["defense"] = BJsonValue.Create(30)
            });

            var gameState = new BJsonObject
            {
                ["version"] = BJsonValue.Create("1.0.0"),
                ["timestamp"] = BJsonValue.Create(1234567890L),
                ["player"] = BJsonValue.Create(new BJsonObject
                {
                    ["name"] = BJsonValue.Create("Alice"),
                    ["level"] = BJsonValue.Create(10),
                    ["experience"] = BJsonValue.Create(5000.5)
                }),
                ["inventory"] = BJsonValue.Create(inventory)
            };

            var original = BJsonValue.Create(gameState);

            byte[] binaryData = BJsonBinaryWriter.Serialize(original);
            var fromBinary = BJsonBinaryReader.Deserialize(binaryData);

            string jsonText = BJsonTextWriter.Serialize(original);
            var fromJson = BJsonTextReader.Deserialize(jsonText);

            Assert.Equal(original, fromBinary);
            Assert.Equal(original, fromJson);
        }

        [Fact]
        public void Array_CanStoreVector3Like_Values()
        {
            var position = new BJsonArray { 10.5, 20.0, 30.25 };
            var rotation = new BJsonArray { 0.0, 90.0, 0.0 };
            var scale = new BJsonArray { 1.0, 1.0, 1.0 };

            var transform = new BJsonObject
            {
                ["position"] = BJsonValue.Create(position),
                ["rotation"] = BJsonValue.Create(rotation),
                ["scale"] = BJsonValue.Create(scale)
            };

            var value = BJsonValue.Create(transform);
            var json = BJsonTextWriter.Serialize(value);
            var parsed = BJsonTextReader.Deserialize(json);

            Assert.Equal(value, parsed);
            var parsedTransform = parsed.ObjectValue;
            Assert.True(parsedTransform.TryGetValue("position", out var posValue));
            Assert.True(posValue.IsArray);
            Assert.Equal(3, posValue.ArrayValue.Count);
            Assert.Equal(10.5, posValue.ArrayValue[0].DoubleValue);
        }

        [Fact]
        public void BinaryData_CanSerializeAssetData()
        {
            byte[] assetData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var binary = new BJsonBinary(assetData);
            var asset = new BJsonObject
            {
                ["assetId"] = BJsonValue.Create("texture_001"),
                ["data"] = BJsonValue.Create(binary)
            };

            var value = BJsonValue.Create(asset);

            byte[] serialized = BJsonBinaryWriter.Serialize(value);
            var deserialized = BJsonBinaryReader.Deserialize(serialized);

            Assert.Equal(value, deserialized);
            Assert.True(deserialized.ObjectValue.TryGetValue("data", out var dataValue));
            Assert.True(dataValue.IsBinary);
            Assert.Equal(assetData, dataValue.BinaryValue.AsSpan().ToArray());
        }

        [Fact]
        public void TextSerialization_DoesNotAllowBinaryByDefault()
        {
            var binary = new BJsonBinary(new byte[] { 1, 2, 3 });
            var value = BJsonValue.Create(binary);

            var ex = Assert.Throws<InvalidOperationException>(() => BJsonTextWriter.Serialize(value));
            Assert.Contains("Binary values are not allowed", ex.Message);
        }

        [Fact]
        public void PrettyPrint_ProducesReadableConfigFiles()
        {
            var config = new BJsonObject
            {
                ["graphics"] = BJsonValue.Create(new BJsonObject
                {
                    ["quality"] = BJsonValue.Create("High"),
                    ["vsync"] = BJsonValue.True,
                    ["resolution"] = BJsonValue.Create(new BJsonArray { 1920, 1080 })
                }),
                ["audio"] = BJsonValue.Create(new BJsonObject
                {
                    ["masterVolume"] = BJsonValue.Create(0.8),
                    ["musicVolume"] = BJsonValue.Create(0.6)
                })
            };

            var value = BJsonValue.Create(config);
            var options = new BJsonTextWriterOptions { Indented = true, IndentSize = 2 };

            var json = BJsonTextWriter.Serialize(value, options);

            Assert.Contains(Environment.NewLine, json);
            Assert.Contains("  \"graphics\":", json);
            Assert.Contains("    \"quality\":", json);
        }

        [Fact]
        public void Facade_BJson_WorksForCommonScenarios()
        {
            var data = new BJsonObject
            {
                ["gameId"] = BJsonValue.Create("session_123"),
                ["players"] = BJsonValue.Create(new BJsonArray { "Alice", "Bob" }),
                ["settings"] = BJsonValue.Create(new BJsonObject
                {
                    ["difficulty"] = BJsonValue.Create("Normal")
                })
            };
            var value = BJsonValue.Create(data);

            byte[] binary = BJson.SerializeToBytes(value);
            var fromBinary = BJson.Deserialize(binary);

            string json = BJson.Stringify(value);
            var fromJson = BJson.Parse(json);

            Assert.Equal(value, fromBinary);
            Assert.Equal(value, fromJson);
        }

        [Fact]
        public void Primitives_CoverCommonUnityTypes()
        {
            var data = new BJsonObject
            {
                ["intValue"] = BJsonValue.Create(42),
                ["longValue"] = BJsonValue.Create(9876543210L),
                ["floatValue"] = BJsonValue.Create(3.14f),
                ["doubleValue"] = BJsonValue.Create(2.718281828),
                ["boolValue"] = BJsonValue.True,
                ["stringValue"] = BJsonValue.Create("Unity"),
                ["nullValue"] = BJsonValue.Null
            };

            var value = BJsonValue.Create(data);
            var json = BJsonTextWriter.Serialize(value);
            var parsed = BJsonTextReader.Deserialize(json);

            Assert.Equal(value, parsed);
        }
    }
}
