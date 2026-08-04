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
        public void Deserialize_UsesFactoryMethodParameterMapping()
        {
            var json = new BJsonObject
            {
                ["coord_x"] = BJsonValue.Create(10),
                ["coord_y"] = BJsonValue.Create(20)
            };

            var result = BJson.Deserialize<FactoryMappedPoint>(BJsonValue.Create(json));

            Assert.NotNull(result);
            Assert.Equal(10, result!.X);
            Assert.Equal(20, result.Y);
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

        [Fact]
        public void Deserialize_NullToken_UsesConfiguredDefault_ForNonNullableValueType()
        {
            var json = new BJsonObject
            {
                ["Count"] = BJsonValue.Null
            };

            var result = BJson.Deserialize<NullDefaultModel>(BJsonValue.Create(json));

            Assert.NotNull(result);
            Assert.Equal(7, result!.Count);
        }

        [Fact]
        public void Serialize_WhenWritingCustomDefault_UsesProviderDefaultValue()
        {
            var sameAsProvider = new CustomDefaultIgnoreModel { Count = 42 };
            var differentThanProvider = new CustomDefaultIgnoreModel { Count = 0 };

            var sameSerialized = BJson.Serialize(sameAsProvider);
            var differentSerialized = BJson.Serialize(differentThanProvider);

            Assert.True(sameSerialized.TryGetObject(out var sameObj));
            Assert.False(sameObj.ContainsKey("Count"));

            Assert.True(differentSerialized.TryGetObject(out var differentObj));
            Assert.True(differentObj.ContainsKey("Count"));
            Assert.Equal(0, differentObj["Count"].IntValue);
        }

        [Fact]
        public void Serialize_TypeLevelVersion_GuardsMembers_And_ComposesWithMemberVersion()
        {
            var model = new TypeVersionedModel
            {
                AlwaysInTypeRange = 10,
                LegacyInType = 20
            };

            var belowTypeRangeOptions = new BJsonSerializerOptions
            {
                Version = new Version(1, 0, 0)
            };

            var inTypeRangeOptions = new BJsonSerializerOptions
            {
                Version = new Version(2, 0, 0)
            };

            var afterMemberRemovalOptions = new BJsonSerializerOptions
            {
                Version = new Version(3, 0, 0)
            };

            var belowTypeRange = BJson.Serialize(model, typeof(TypeVersionedModel), belowTypeRangeOptions);
            var inTypeRange = BJson.Serialize(model, typeof(TypeVersionedModel), inTypeRangeOptions);
            var afterMemberRemoval = BJson.Serialize(model, typeof(TypeVersionedModel), afterMemberRemovalOptions);

            Assert.True(belowTypeRange.TryGetObject(out var belowObj));
            Assert.False(belowObj.ContainsKey("AlwaysInTypeRange"));
            Assert.False(belowObj.ContainsKey("LegacyInType"));

            Assert.True(inTypeRange.TryGetObject(out var inObj));
            Assert.True(inObj.ContainsKey("AlwaysInTypeRange"));
            Assert.True(inObj.ContainsKey("LegacyInType"));

            Assert.True(afterMemberRemoval.TryGetObject(out var removedObj));
            Assert.True(removedObj.ContainsKey("AlwaysInTypeRange"));
            Assert.False(removedObj.ContainsKey("LegacyInType"));
        }

        [Fact]
        public void Deserialize_UsesMultipleAliases_ForLegacyMemberNames()
        {
            var json = new BJsonObject
            {
                ["legacy_count_2"] = BJsonValue.Create(9)
            };

            var model = BJson.Deserialize<AliasedModel>(BJsonValue.Create(json));

            Assert.NotNull(model);
            Assert.Equal(9, model!.Count);
        }

        [Fact]
        public void Deserialize_RequiredWhen_UsesVersionState()
        {
            var json = new BJsonObject();
            var v1 = new BJsonSerializerOptions { Version = new Version(1, 0, 0) };
            var v2 = new BJsonSerializerOptions { Version = new Version(2, 0, 0) };

            var modelV1 = BJson.Deserialize<ConditionalRequiredModel>(BJsonValue.Create(json), v1);
            Assert.NotNull(modelV1);

            Assert.Throws<Krampus.BinJson.Error.BJsonDeserializationException>(() =>
                BJson.Deserialize<ConditionalRequiredModel>(BJsonValue.Create(json), v2));
        }

        [Fact]
        public void Deserialize_DefaultProvider_CanUseActiveVersion()
        {
            var json = new BJsonObject();
            var v1 = new BJsonSerializerOptions { Version = new Version(1, 0, 0) };
            var v3 = new BJsonSerializerOptions { Version = new Version(3, 0, 0) };

            var legacy = BJson.Deserialize<VersionAwareDefaultProviderModel>(BJsonValue.Create(json), v1);
            var modern = BJson.Deserialize<VersionAwareDefaultProviderModel>(BJsonValue.Create(json), v3);

            Assert.NotNull(legacy);
            Assert.NotNull(modern);
            Assert.Equal(10, legacy!.Value);
            Assert.Equal(30, modern!.Value);
        }

        [Fact]
        public void SerializeAndDeserialize_UsesOpenGenericConverterFactory()
        {
            var model = new FactoryConverterModel
            {
                Age = new Wrapped<int>(17)
            };

            var serialized = BJson.Serialize(model);
            Assert.True(serialized.TryGetObject(out var obj));
            Assert.Equal(17, obj["Age"].IntValue);

            var roundTrip = BJson.Deserialize<FactoryConverterModel>(serialized);
            Assert.NotNull(roundTrip);
            Assert.Equal(17, roundTrip!.Age.Value);
        }

        [Fact]
        public void Polymorphic_DiscriminatorValue_WorksWithoutDerivedRegistration()
        {
            Vehicle vehicle = new Car
            {
                Name = "Falcon",
                Doors = 4
            };

            var serialized = BJson.Serialize(vehicle, typeof(Vehicle));
            Assert.True(serialized.TryGetObject(out var obj));
            Assert.Equal("car", obj["$type"].StringValue);

            var roundTrip = BJson.Deserialize<Vehicle>(serialized);
            var car = Assert.IsType<Car>(roundTrip);
            Assert.Equal("Falcon", car.Name);
            Assert.Equal(4, car.Doors);
        }

        [Fact]
        public void LifecycleHooks_RunOnSerializeAndDeserialize()
        {
            var model = new LifecycleModel { Count = 2 };

            var serialized = BJson.Serialize(model);
            Assert.True(serialized.TryGetObject(out var obj));
            Assert.Equal(3, obj["Count"].IntValue);

            var deserialized = BJson.Deserialize<LifecycleModel>(serialized);
            Assert.NotNull(deserialized);
            Assert.True(deserialized!.AfterDeserializeRan);
        }

        [Fact]
        public void NumberHandling_AllowsStringRead_AndLosslessStringWrite()
        {
            var json = new BJsonObject
            {
                ["Count"] = BJsonValue.Create("42"),
                ["Amount"] = BJsonValue.Create("123.5")
            };

            var model = BJson.Deserialize<NumberHandlingModel>(BJsonValue.Create(json));
            Assert.NotNull(model);
            Assert.Equal(42, model!.Count);
            Assert.Equal(123.5m, model.Amount);

            var serialized = BJson.Serialize(model);
            Assert.True(serialized.TryGetObject(out var outObj));
            Assert.Equal("123.5", outObj["Amount"].StringValue);
        }

        [Fact]
        public void Deserialize_MultipleFactoryMethods_ThrowsMetadataException()
        {
            var json = new BJsonObject
            {
                ["Value"] = BJsonValue.Create(10)
            };

            Assert.Throws<Krampus.BinJson.Error.BJsonMetadataException>(() =>
                BJson.Deserialize<MultipleFactoryMethodsModel>(BJsonValue.Create(json)));
        }

        [Fact]
        public void Deserialize_InvalidFactorySignature_ThrowsMetadataException()
        {
            var json = new BJsonObject
            {
                ["Value"] = BJsonValue.Create(10)
            };

            Assert.Throws<Krampus.BinJson.Error.BJsonMetadataException>(() =>
                BJson.Deserialize<InvalidFactorySignatureModel>(BJsonValue.Create(json)));
        }

        [Fact]
        public void Deserialize_FactoryParameterMapping_UnknownParameter_ThrowsMetadataException()
        {
            var json = new BJsonObject
            {
                ["coord_x"] = BJsonValue.Create(1)
            };

            Assert.Throws<Krampus.BinJson.Error.BJsonMetadataException>(() =>
                BJson.Deserialize<InvalidFactoryUnknownParameterMappingModel>(BJsonValue.Create(json)));
        }

        [Fact]
        public void Deserialize_FactoryParameterMapping_DuplicateJsonKey_ThrowsMetadataException()
        {
            var json = new BJsonObject
            {
                ["coord"] = BJsonValue.Create(1)
            };

            Assert.Throws<Krampus.BinJson.Error.BJsonMetadataException>(() =>
                BJson.Deserialize<InvalidFactoryDuplicateJsonKeyMappingModel>(BJsonValue.Create(json)));
        }

        [Fact]
        public void Deserialize_PrivateFactoryMethod_IsSupported()
        {
            var json = new BJsonObject
            {
                ["value"] = BJsonValue.Create(5)
            };

            var result = BJson.Deserialize<PrivateFactoryMethodModel>(BJsonValue.Create(json));
            Assert.NotNull(result);
            Assert.Equal(5, result!.Value);
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

        public sealed class FactoryMappedPoint
        {
            public int X { get; set; }

            public int Y { get; set; }

            [BJsonFactoryMethod(ParameterMapping = new[] { "x", "coord_x", "y", "coord_y" })]
            public static FactoryMappedPoint Create(int x, int y)
            {
                return new FactoryMappedPoint { X = x, Y = y };
            }
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

        [BJsonSerializable]
        public sealed class NullDefaultModel
        {
            [BJsonDefaultValue(7)]
            public int Count { get; set; }
        }

        [BJsonSerializable]
        public sealed class CustomDefaultIgnoreModel
        {
            [BJsonDefaultProvider(nameof(GetDefaultCount))]
            [BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingCustomDefault)]
            public int Count { get; set; }

            internal static int GetDefaultCount()
            {
                return 42;
            }
        }

        [BJsonSerializable]
        [BJsonVersionContext(typeof(Version), "2.0.0")]
        [BJsonVersion(typeof(Version), introducedIn: "2.0.0")]
        public sealed class TypeVersionedModel
        {
            public int AlwaysInTypeRange { get; set; }

            [BJsonVersion(typeof(Version), removedIn: "3.0.0")]
            public int LegacyInType { get; set; }
        }

        [BJsonSerializable]
        public sealed class MultipleFactoryMethodsModel
        {
            public int Value { get; set; }

            [BJsonFactoryMethod]
            public static MultipleFactoryMethodsModel CreateA(int value)
            {
                return new MultipleFactoryMethodsModel { Value = value };
            }

            [BJsonFactoryMethod]
            public static MultipleFactoryMethodsModel CreateB(int value)
            {
                return new MultipleFactoryMethodsModel { Value = value };
            }
        }

        [BJsonSerializable]
        public sealed class InvalidFactorySignatureModel
        {
            public int Value { get; set; }

            [BJsonFactoryMethod]
            public static string Create(int value)
            {
                return value.ToString();
            }
        }

        [BJsonSerializable]
        public sealed class InvalidFactoryUnknownParameterMappingModel
        {
            public int X { get; set; }

            [BJsonFactoryMethod(ParameterMapping = new[] { "missing", "coord_x" })]
            public static InvalidFactoryUnknownParameterMappingModel Create(int x)
            {
                return new InvalidFactoryUnknownParameterMappingModel { X = x };
            }
        }

        [BJsonSerializable]
        public sealed class InvalidFactoryDuplicateJsonKeyMappingModel
        {
            public int X { get; set; }

            public int Y { get; set; }

            [BJsonFactoryMethod(ParameterMapping = new[] { "x", "coord", "y", "coord" })]
            public static InvalidFactoryDuplicateJsonKeyMappingModel Create(int x, int y)
            {
                return new InvalidFactoryDuplicateJsonKeyMappingModel { X = x, Y = y };
            }
        }

        [BJsonSerializable]
        public sealed class PrivateFactoryMethodModel
        {
            private PrivateFactoryMethodModel(int value)
            {
                Value = value;
            }

            public int Value { get; }

            [BJsonFactoryMethod]
            private static PrivateFactoryMethodModel Create(int value)
            {
                return new PrivateFactoryMethodModel(value);
            }
        }

        [BJsonSerializable]
        public sealed class AliasedModel
        {
            [BJsonAlias("legacy_count_1")]
            [BJsonAlias("legacy_count_2")]
            public int Count { get; set; }
        }

        [BJsonSerializable]
        public sealed class ConditionalRequiredModel
        {
            [BJsonRequiredWhen(nameof(IsNameRequired))]
            public string? Name { get; set; }

            private static bool IsNameRequired(string memberName, IComparable? version)
            {
                if (!string.Equals(memberName, "Name", StringComparison.Ordinal))
                    return false;

                return version is Version semantic && semantic >= new Version(2, 0, 0);
            }
        }

        [BJsonSerializable]
        public sealed class VersionAwareDefaultProviderModel
        {
            [BJsonDefaultProvider(nameof(GetDefaultValue))]
            public int Value { get; set; }

            private static object GetDefaultValue(IComparable? version)
            {
                if (version is Version semantic && semantic >= new Version(3, 0, 0))
                    return 30;

                return 10;
            }
        }

        [BJsonSerializable]
        public sealed class FactoryConverterModel
        {
            [BJsonConverterFactory(typeof(WrappedConverterFactory))]
            public Wrapped<int> Age { get; set; }
        }

        public readonly struct Wrapped<T>
        {
            public Wrapped(T value)
            {
                Value = value;
            }

            public T Value { get; }
        }

        public sealed class WrappedConverterFactory : IBJsonConverterFactory
        {
            public bool CanConvert(Type type)
            {
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Wrapped<>);
            }

            public IBJsonConverter? CreateConverter(Type type)
            {
                if (!CanConvert(type))
                    return null;

                var itemType = type.GetGenericArguments()[0];
                var converterType = typeof(WrappedConverter<>).MakeGenericType(itemType);
                return Activator.CreateInstance(converterType) as IBJsonConverter;
            }
        }

        public sealed class WrappedConverter<T> : BJsonConverter<Wrapped<T>>
        {
            public override BJsonValue Serialize(Wrapped<T> value, BJsonSerializationContext context)
            {
                return context.Serialize(value.Value, typeof(T));
            }

            public override Wrapped<T> Deserialize(BJsonValue value, BJsonSerializationContext context)
            {
                return new Wrapped<T>(context.Deserialize<T>(value)!);
            }
        }

        [BJsonPolymorphic]
        public abstract class Vehicle
        {
            public string Name { get; set; } = string.Empty;
        }

        [BJsonDiscriminatorValue("car")]
        public sealed class Car : Vehicle
        {
            public int Doors { get; set; }
        }

        [BJsonSerializable]
        public sealed class LifecycleModel
        {
            public int Count { get; set; }

            public bool AfterDeserializeRan { get; private set; }

            [BJsonOnSerializing]
            private void OnSerializing(BJsonSerializationContext context)
            {
                Count += 1;
            }

            [BJsonOnDeserialized]
            private void OnDeserialized()
            {
                AfterDeserializeRan = true;
            }
        }

        [BJsonSerializable]
        public sealed class NumberHandlingModel
        {
            [BJsonNumberHandling(BJsonNumberHandling.AllowReadingFromString)]
            public int Count { get; set; }

            [BJsonNumberHandling(BJsonNumberHandling.AllowReadingFromString | BJsonNumberHandling.Lossless)]
            public decimal Amount { get; set; }
        }
    }
}
