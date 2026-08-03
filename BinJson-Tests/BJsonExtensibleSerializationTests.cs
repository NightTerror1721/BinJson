using Krampus.BinJson.Serialization;

namespace Krampus.BinJson.Tests
{
    public class BJsonExtensibleSerializationTests
    {
        [Fact]
        public void Serialize_UsesPropertyAttributes()
        {
            var model = new SampleModel
            {
                Id = 7,
                Name = "alpha",
                Ignored = "secret"
            };

            var value = BJson.Serialize(model);

            Assert.True(value.TryGetObject(out var obj));
            Assert.True(obj.ContainsKey("identifier"));
            Assert.True(obj.ContainsKey("Name"));
            Assert.False(obj.ContainsKey("Ignored"));

            Assert.Equal(7, obj["identifier"].IntValue);
            Assert.Equal("alpha", obj["Name"].StringValue);
        }

        [Fact]
        public void Deserialize_UsesPropertyAttributes_AndCaseInsensitiveOption()
        {
            var json = new BJsonObject
            {
                ["IDENTIFIER"] = BJsonValue.Create(5),
                ["name"] = BJsonValue.Create("beta")
            };

            var options = new BJsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var model = BJson.Deserialize<SampleModel>(BJsonValue.Create(json), options);

            Assert.NotNull(model);
            Assert.Equal(5, model!.Id);
            Assert.Equal("beta", model.Name);
        }

        [Fact]
        public void SerializeAndDeserialize_UsesConverterAttribute()
        {
            var model = new ModelWithConverter
            {
                CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
            };

            var serialized = BJson.Serialize(model);

            Assert.True(serialized.TryGetObject(out var obj));
            Assert.Equal("2026-01-02", obj["CreatedAt"].StringValue);

            var deserialized = BJson.Deserialize<ModelWithConverter>(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), deserialized!.CreatedAt);
        }

        [Fact]
        public void SerializeAndDeserialize_UsesOptionsConverter()
        {
            var options = new BJsonSerializerOptions();
            options.AddConverter(new TrimmedStringConverter());

            var value = BJson.Serialize("  hello  ", typeof(string), options);
            Assert.Equal("hello", value.StringValue);

            var result = BJson.Deserialize<string>(value, options);
            Assert.Equal("hello", result);
        }

        [Fact]
        public void SerializeAndDeserialize_UsesInterfaceContracts()
        {
            var model = new InterfaceModel { Name = "hero", Level = 9 };

            var value = BJson.Serialize(model);
            var roundTrip = BJson.Deserialize<InterfaceModel>(value);

            Assert.True(value.TryGetObject(out var obj));
            Assert.Equal("hero", obj["customName"].StringValue);
            Assert.Equal(9, obj["customLevel"].IntValue);
            Assert.NotNull(roundTrip);
            Assert.Equal("hero", roundTrip!.Name);
            Assert.Equal(9, roundTrip.Level);
        }

        [Fact]
        public void Deserialize_UsesParameterizedConstructor()
        {
            var json = new BJsonObject
            {
                ["name"] = BJsonValue.Create("mage"),
                ["level"] = BJsonValue.Create(12)
            };

            var result = BJson.Deserialize<ParameterizedConstructorModel>(BJsonValue.Create(json));

            Assert.NotNull(result);
            Assert.Equal("mage", result!.Name);
            Assert.Equal(12, result.Level);
        }

        [Fact]
        public void Deserialize_MissingRequiredMember_Throws()
        {
            var json = new BJsonObject();

            Assert.Throws<Krampus.BinJson.Error.BJsonDeserializationException>(() => BJson.Deserialize<RequiredModel>(BJsonValue.Create(json)));
        }

        [Fact]
        public void Serialize_PolymorphicBase_WritesTypeDiscriminator_AndDeserializeRestoresDerivedType()
        {
            Animal model = new Dog
            {
                Name = "Rex",
                Breed = "Shepherd"
            };

            var value = BJson.Serialize<Animal>(model);
            Assert.True(value.TryGetObject(out var obj));
            Assert.True(obj.ContainsKey("$type"));
            Assert.Equal(typeof(Dog).FullName, obj["$type"].StringValue);

            var result = BJson.Deserialize<Animal>(value);
            Assert.NotNull(result);
            var dog = Assert.IsType<Dog>(result);
            Assert.Equal("Rex", dog.Name);
            Assert.Equal("Shepherd", dog.Breed);
        }

        [Fact]
        public void Serialize_IgnoreConditions_SkipNullAndDefaultMembers()
        {
            var model = new IgnoreConditionModel
            {
                Name = "unit",
                Alias = null,
                Count = 0
            };

            var value = BJson.Serialize(model);
            Assert.True(value.TryGetObject(out var obj));
            Assert.True(obj.ContainsKey("Name"));
            Assert.False(obj.ContainsKey("Alias"));
            Assert.False(obj.ContainsKey("Count"));
        }

        [Fact]
        public void Deserialize_AndSerialize_ExtensionData_RoundTripsUnknownMembers()
        {
            var json = new BJsonObject
            {
                ["Name"] = BJsonValue.Create("config"),
                ["Unknown1"] = BJsonValue.Create(123),
                ["Unknown2"] = BJsonValue.Create("abc")
            };

            var model = BJson.Deserialize<ExtensionDataModel>(BJsonValue.Create(json));
            Assert.NotNull(model);
            Assert.Equal("config", model!.Name);
            Assert.NotNull(model.ExtraData);
            Assert.Equal(2, model.ExtraData!.Count);
            Assert.Equal(123, model.ExtraData["Unknown1"].IntValue);
            Assert.Equal("abc", model.ExtraData["Unknown2"].StringValue);

            var roundTrip = BJson.Serialize(model);
            Assert.True(roundTrip.TryGetObject(out var roundTripObject));
            Assert.Equal("config", roundTripObject["Name"].StringValue);
            Assert.Equal(123, roundTripObject["Unknown1"].IntValue);
            Assert.Equal("abc", roundTripObject["Unknown2"].StringValue);
        }

        [Fact]
        public void SerializeAndDeserialize_UsesBuiltInConverters()
        {
            var expectedGuid = Guid.Parse("9d0f6f26-4a6f-4b9d-9e12-2fd704c547f2");
            var expectedDate = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);
            var model = new BuiltInConverterModel
            {
                Id = expectedGuid,
                CreatedAt = expectedDate,
                Duration = TimeSpan.FromMinutes(90),
                Link = new Uri("https://example.com/docs")
            };

            var value = BJson.Serialize(model);
            Assert.True(value.TryGetObject(out var obj));
            Assert.Equal(expectedGuid.ToString("D"), obj["Id"].StringValue);
            Assert.Equal(expectedDate.ToString("O"), obj["CreatedAt"].StringValue);
            Assert.Equal(TimeSpan.FromMinutes(90).ToString("c"), obj["Duration"].StringValue);
            Assert.Equal("https://example.com/docs", obj["Link"].StringValue);

            var result = BJson.Deserialize<BuiltInConverterModel>(value);
            Assert.NotNull(result);
            Assert.Equal(expectedGuid, result!.Id);
            Assert.Equal(expectedDate, result.CreatedAt);
            Assert.Equal(TimeSpan.FromMinutes(90), result.Duration);
            Assert.Equal(new Uri("https://example.com/docs"), result.Link);
        }

        public sealed class SampleModel
        {
            [BJsonPropertyName("identifier")]
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            [BJsonIgnore]
            public string? Ignored { get; set; }
        }

        public sealed class ModelWithConverter
        {
            [BJsonConverter(typeof(DateOnlyStringConverter))]
            public DateTime CreatedAt { get; set; }
        }

        public sealed class DateOnlyStringConverter : BJsonConverter<DateTime>
        {
            public override BJsonValue Serialize(DateTime value, BJsonSerializationContext context)
            {
                return BJsonValue.Create(value.ToString("yyyy-MM-dd"));
            }

            public override DateTime Deserialize(BJsonValue value, BJsonSerializationContext context)
            {
                return DateTime.ParseExact(value.StringValue, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
            }
        }

        public sealed class TrimmedStringConverter : BJsonConverter<string>
        {
            public override BJsonValue Serialize(string? value, BJsonSerializationContext context)
            {
                return BJsonValue.Create(value?.Trim());
            }

            public override string? Deserialize(BJsonValue value, BJsonSerializationContext context)
            {
                if (value.IsNull)
                    return null;

                return value.StringValue.Trim();
            }
        }

        public sealed class InterfaceModel : IBJsonSerializable, IBJsonDeserializable
        {
            public string Name { get; set; } = string.Empty;

            public int Level { get; set; }

            public BJsonValue Serialize(BJsonSerializationContext context)
            {
                return BJsonValue.Create(new BJsonObject
                {
                    ["customName"] = BJsonValue.Create(Name),
                    ["customLevel"] = BJsonValue.Create(Level)
                });
            }

            public void Deserialize(BJsonValue value, BJsonDeserializationContext context)
            {
                var obj = value.ObjectValue;
                Name = obj["customName"].StringValue;
                Level = obj["customLevel"].IntValue;
            }
        }

        [BJsonSerializable]
        public sealed class ParameterizedConstructorModel
        {
            [BJsonConstructor]
            public ParameterizedConstructorModel(string name, int level)
            {
                Name = name;
                Level = level;
            }

            public string Name { get; }

            public int Level { get; }
        }

        [BJsonSerializable]
        public sealed class RequiredModel
        {
            [BJsonRequired]
            public string Name { get; set; } = string.Empty;
        }

        [BJsonPolymorphic]
        [BJsonDerivedType(typeof(Dog))]
        public abstract class Animal
        {
            public string Name { get; set; } = string.Empty;
        }

        public sealed class Dog : Animal
        {
            public string Breed { get; set; } = string.Empty;
        }

        [BJsonSerializable]
        public sealed class IgnoreConditionModel
        {
            public string Name { get; set; } = string.Empty;

            [BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingNull)]
            public string? Alias { get; set; }

            [BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingDefault)]
            public int Count { get; set; }
        }

        [BJsonSerializable]
        public sealed class ExtensionDataModel
        {
            public string Name { get; set; } = string.Empty;

            [BJsonExtensionData]
            public Dictionary<string, BJsonValue>? ExtraData { get; set; }
        }

        [BJsonSerializable]
        public sealed class BuiltInConverterModel
        {
            public Guid Id { get; set; }

            public DateTime CreatedAt { get; set; }

            public TimeSpan Duration { get; set; }

            public Uri? Link { get; set; }
        }
    }
}
