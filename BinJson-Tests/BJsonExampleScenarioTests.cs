using System;
using System.Collections.Generic;
using System.Globalization;
using Krampus.BinJson.Serialization;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonExampleScenarioTests
    {
        [Fact]
        public void Example_ApiDtos_PlayerResponse_RoundTrips_WithCustomConverter()
        {
            var response = new ExamplePlayerResponse
            {
                Id = 7,
                DisplayName = "mage",
                CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
            };

            var payload = BJson.Serialize(response);
            var roundTrip = BJson.Deserialize<ExamplePlayerResponse>(payload);

            Assert.NotNull(roundTrip);
            Assert.Equal(7, roundTrip!.Id);
            Assert.Equal("mage", roundTrip.DisplayName);
            Assert.Equal(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), roundTrip.CreatedAt);
            Assert.Equal("mage", payload.ObjectValue["name"].StringValue);
            var createdAtKey = payload.ObjectValue.ContainsKey("createdAt") ? "createdAt" : "CreatedAt";
            Assert.Equal("2026-01-02", payload.ObjectValue[createdAtKey].StringValue);
        }

        [Fact]
        public void Example_ConfigurationFiles_PreservesExtensionData()
        {
            var incoming = new BJsonObject
            {
                ["environment"] = BJsonValue.Create("production"),
                ["featureFlag"] = BJsonValue.Create(true),
                ["retryCount"] = BJsonValue.Create(5)
            };

            var config = BJson.Deserialize<ExampleConfigurationDocument>(BJsonValue.Create(incoming));
            Assert.NotNull(config);

            config!.AppName = "BinJson Demo";

            var outgoing = BJson.Serialize(config);
            Assert.True(outgoing.TryGetObject(out var obj));
            var appNameKey = obj.ContainsKey("appName") ? "appName" : "AppName";
            Assert.Equal("BinJson Demo", obj[appNameKey].StringValue);
            Assert.Equal("production", obj["environment"].StringValue);
            Assert.True(obj.ContainsKey("featureFlag"));
            Assert.True(obj.ContainsKey("retryCount"));
        }

        [Fact]
        public void Example_GameStateSerialization_RoundTrip()
        {
            var save = new ExampleGameSave
            {
                PlayerName = "Hero",
                Level = 12,
                LastCheckpoint = "Crystal Cave",
                Inventory = new List<ExampleInventoryItem>
                {
                    new ExampleInventoryItem { Name = "Potion", Quantity = 3 },
                    new ExampleInventoryItem { Name = "Key", Quantity = 1 }
                }
            };

            var value = BJson.Serialize(save, new BJsonSerializerOptions());
            var roundTrip = BJson.Deserialize<ExampleGameSave>(value, new BJsonSerializerOptions());

            Assert.NotNull(roundTrip);
            Assert.Equal("Hero", roundTrip!.PlayerName);
            Assert.Equal(12, roundTrip.Level);
            Assert.Equal("Crystal Cave", roundTrip.LastCheckpoint);
            Assert.Equal(2, roundTrip.Inventory.Count);
            Assert.Equal("Potion", roundTrip.Inventory[0].Name);
            Assert.Equal(3, roundTrip.Inventory[0].Quantity);
        }

        [BJsonSerializable(NamingPolicy = NamingPolicy.CamelCase)]
        private sealed class ExamplePlayerResponse
        {
            public int Id { get; set; }

            [BJsonPropertyName("name")]
            public string DisplayName { get; set; } = string.Empty;

            [BJsonConverter(typeof(ExampleDateOnlyStringConverter))]
            public DateTime CreatedAt { get; set; }
        }

        private sealed class ExampleDateOnlyStringConverter : BJsonConverter<DateTime>
        {
            public override BJsonValue Serialize(DateTime value, BJsonSerializationContext context)
            {
                return BJsonValue.Create(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            public override DateTime Deserialize(BJsonValue value, BJsonSerializationContext context)
            {
                return DateTime.ParseExact(
                    value.StringValue,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            }
        }

        [BJsonSerializable(NamingPolicy = NamingPolicy.CamelCase)]
        private sealed class ExampleConfigurationDocument
        {
            public string AppName { get; set; } = string.Empty;

            public string Environment { get; set; } = string.Empty;

            [BJsonExtensionData]
            public Dictionary<string, BJsonValue>? ExtraData { get; set; }
        }

        [BJsonSerializable]
        private sealed class ExampleGameSave
        {
            public string PlayerName { get; set; } = string.Empty;

            public int Level { get; set; }

            public string LastCheckpoint { get; set; } = string.Empty;

            public List<ExampleInventoryItem> Inventory { get; set; } = new List<ExampleInventoryItem>();
        }

        [BJsonSerializable]
        private sealed class ExampleInventoryItem
        {
            public string Name { get; set; } = string.Empty;

            public int Quantity { get; set; }
        }
    }
}
