using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Krampus.BinJson.Serialization;
using Krampus.BinJson.SourceGenerators;
using Krampus.BinJson.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Krampus.BinJson.Tests
{
    public class BJsonSourceGeneratorTests
    {
    private static readonly MetadataReference[] SharedReferences = BuildSharedReferences();
    private static readonly CSharpCompilationOptions SharedLibraryCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
    private static readonly ISourceGenerator SharedSourceGenerator = new BJsonSourceGenerator().AsSourceGenerator();

        [Fact]
        public void CodeEmitter_EmitSerializer_GeneratesBridgeBasedSerializer()
        {
            var config = new TypeConfiguration(false, false, Krampus.BinJson.SourceGenerators.Models.NamingPolicy.Default);
            var model = new GeneratedTypeModel("Demo.Models", "PlayerState", false, false, config);

            var source = CodeEmitter.EmitSerializer(model);

            Assert.Contains("namespace Demo.Models", source, StringComparison.Ordinal);
            Assert.Contains("internal sealed class PlayerState_BJsonSerializer : BJsonConverter<PlayerState>", source, StringComparison.Ordinal);
            // TODO: Update assertions once new code generation is implemented
        }

        [Fact]
        public void CodeEmitter_EmitSerializer_ForGlobalNamespace_DoesNotEmitIndentedTopLevelType()
        {
            var config = new TypeConfiguration(false, false, Krampus.BinJson.SourceGenerators.Models.NamingPolicy.Default);
            var model = new GeneratedTypeModel(string.Empty, "PlayerState", false, false, config);

            var source = CodeEmitter.EmitSerializer(model);

            Assert.DoesNotContain("namespace ", source, StringComparison.Ordinal);
            Assert.Contains(Environment.NewLine + "internal sealed class PlayerState_BJsonSerializer", source, StringComparison.Ordinal);
        }

        [Fact]
        public void SourceGenerator_EmitsSerializer_And_RuntimeUsesIt_ForAttributedType()
        {
            const string source = @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class GeneratedPerson
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

            var generatedAssembly = CompileWithGenerator(source);
            var generatedType = generatedAssembly.GetType("GeneratedRuntime.GeneratedPerson", throwOnError: true)!;
            var serializerType = generatedAssembly.GetType("GeneratedRuntime.GeneratedPerson_BJsonSerializer", throwOnError: true);

            Assert.NotNull(serializerType);

            var instance = Activator.CreateInstance(generatedType)!;
            generatedType.GetProperty("Id")!.SetValue(instance, 42);
            generatedType.GetProperty("Name")!.SetValue(instance, "mage");

            var serialized = BJson.Serialize(instance, generatedType);
            Assert.True(serialized.TryGetObject(out var obj));
            Assert.Equal(42, obj["Id"].IntValue);
            Assert.Equal("mage", obj["Name"].StringValue);

            var roundTrip = BJson.Deserialize(serialized, generatedType);
            Assert.NotNull(roundTrip);
            Assert.Equal(42, generatedType.GetProperty("Id")!.GetValue(roundTrip));
            Assert.Equal("mage", generatedType.GetProperty("Name")!.GetValue(roundTrip));
        }

        [Fact]
        public void SourceGenerator_FactoryParameterMapping_IsUsedByGeneratedDeserializer()
        {
            const string source = @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class GeneratedPoint
    {
        public int X { get; }
        public int Y { get; }

        private GeneratedPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        [BJsonFactoryMethod(ParameterMapping = new[] { ""x"", ""coord_x"", ""y"", ""coord_y"" })]
        public static GeneratedPoint Create(int x, int y) => new GeneratedPoint(x, y);
    }
}";

            var generatedAssembly = CompileWithGenerator(source);
            var generatedType = generatedAssembly.GetType("GeneratedRuntime.GeneratedPoint", throwOnError: true)!;
            var serializerType = generatedAssembly.GetType("GeneratedRuntime.GeneratedPoint_BJsonSerializer", throwOnError: true);

            Assert.NotNull(serializerType);

            var json = new BJsonObject
            {
                ["coord_x"] = BJsonValue.Create(7),
                ["coord_y"] = BJsonValue.Create(11)
            };

            var result = BJson.Deserialize(BJsonValue.Create(json), generatedType);
            Assert.NotNull(result);
            Assert.Equal(7, generatedType.GetProperty("X")!.GetValue(result));
            Assert.Equal(11, generatedType.GetProperty("Y")!.GetValue(result));
        }

        [Fact]
        public void SourceGenerator_IgnoreConditionNever_DoesNotDropMember()
        {
            const string source = @"
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class IgnoreNeverModel
    {
        [BJsonIgnore(Condition = BJsonIgnoreCondition.Never)]
        public int Keep { get; set; }

        [BJsonIgnore]
        public int Drop { get; set; }
    }
}";

            var generatedAssembly = CompileWithGenerator(source);
            var generatedType = generatedAssembly.GetType("GeneratedRuntime.IgnoreNeverModel", throwOnError: true)!;

            var instance = Activator.CreateInstance(generatedType)!;
            generatedType.GetProperty("Keep")!.SetValue(instance, 9);
            generatedType.GetProperty("Drop")!.SetValue(instance, 5);

            var serialized = BJson.Serialize(instance, generatedType);
            Assert.True(serialized.TryGetObject(out var obj));
            Assert.True(obj.ContainsKey("Keep"));
            Assert.False(obj.ContainsKey("Drop"));
            Assert.Equal(9, obj["Keep"].IntValue);
        }

        [Fact]
        public void SourceGenerator_BothDefaultAttributes_EmitsWarning()
        {
            const string source = @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class DualDefaultModel
    {
        [BJsonDefaultValue(7)]
        [BJsonDefaultProvider(nameof(GetDefaultValue))]
        public int Value { get; set; }

        internal static int GetDefaultValue() => 99;
    }
}";

            var diagnostics = GetGeneratorDiagnostics(source);
            Assert.Contains(diagnostics, d => d.Id == "BJSON013");
        }

        [Fact]
        public void SourceGenerator_PolymorphicAttribute_GeneratesSerializer_And_ResolvesDerivedType()
        {
            const string source = @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    [BJsonPolymorphic(TypeDiscriminatorPropertyName = ""$kind"")]
    [BJsonDerivedType(typeof(PolyDerived), TypeDiscriminator = ""derived"")]
    public abstract class PolyBase
    {
        public int Id { get; set; }
    }

    [BJsonSerializable]
    public sealed class PolyDerived : PolyBase
    {
        public string Name { get; set; } = string.Empty;
    }
}";

            var generatedAssembly = CompileWithGenerator(source);
            var baseType = generatedAssembly.GetType("GeneratedRuntime.PolyBase", throwOnError: true)!;
            var derivedType = generatedAssembly.GetType("GeneratedRuntime.PolyDerived", throwOnError: true)!;

            var baseSerializerType = generatedAssembly.GetType("GeneratedRuntime.PolyBase_BJsonSerializer", throwOnError: true);
            var derivedSerializerType = generatedAssembly.GetType("GeneratedRuntime.PolyDerived_BJsonSerializer", throwOnError: true);

            Assert.NotNull(baseSerializerType);
            Assert.NotNull(derivedSerializerType);

            var instance = Activator.CreateInstance(derivedType)!;
            derivedType.GetProperty("Id")!.SetValue(instance, 42);
            derivedType.GetProperty("Name")!.SetValue(instance, "mage");

            var serialized = BJson.Serialize(instance, baseType);
            Assert.True(serialized.TryGetObject(out var obj));
            Assert.True(obj.ContainsKey("$kind"));
            Assert.Equal("derived", obj["$kind"].StringValue);

            var roundTrip = BJson.Deserialize(serialized, baseType);
            Assert.NotNull(roundTrip);
            Assert.Equal("PolyDerived", roundTrip!.GetType().Name);
            Assert.Equal("mage", roundTrip.GetType().GetProperty("Name")!.GetValue(roundTrip));
            Assert.Equal(42, roundTrip.GetType().GetProperty("Id")!.GetValue(roundTrip));
        }

        [Fact]
        public void SourceGenerator_PreprocessorAttribute_GeneratesSerializer_And_AppliesRuntimePipeline()
        {
            const string source = @"
using System.IO;
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    [BJsonPreprocessor]
    public sealed class PreModel
    {
        [BJsonAnchor(""primaryColor"")]
        public string PrimaryColor { get; set; } = string.Empty;

        public string Display { get; set; } = string.Empty;

        [BJsonExternalRef]
        public ExternalItem? Inventory { get; set; }
    }

    public sealed class ExternalItem
    {
        public string Name { get; set; } = string.Empty;
    }
}";

            var tempDirectory = Path.Combine(Path.GetTempPath(), "binjson-generated-preprocessor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var externalPath = Path.Combine(tempDirectory, "inventory.bjson");
                var externalDocument = new BJsonObject
                {
                    ["Name"] = BJsonValue.Create("Sword")
                };
                File.WriteAllBytes(externalPath, BJson.SerializeToBytes(BJsonValue.Create(externalDocument)));

                var generatedAssembly = CompileWithGenerator(source);
                var generatedType = generatedAssembly.GetType("GeneratedRuntime.PreModel", throwOnError: true)!;
                var serializerType = generatedAssembly.GetType("GeneratedRuntime.PreModel_BJsonSerializer", throwOnError: true);

                Assert.NotNull(serializerType);

                var document = BJsonValue.Create(new BJsonObject
                {
                    ["PrimaryColor"] = BJsonValue.Create("#FF00FF"),
                    ["Display"] = BJsonValue.Create(new BJsonObject { ["$ref"] = BJsonValue.Create("primaryColor") }),
                    ["Inventory"] = BJsonValue.Create(externalPath)
                });

                var result = BJson.Deserialize(document, generatedType);
                Assert.NotNull(result);
                Assert.Equal("#FF00FF", generatedType.GetProperty("PrimaryColor")!.GetValue(result));
                Assert.Equal("#FF00FF", generatedType.GetProperty("Display")!.GetValue(result));
                Assert.NotNull(generatedType.GetProperty("Inventory")!.GetValue(result));
                Assert.Equal("Sword", generatedType.GetProperty("Inventory")!.GetValue(result)!.GetType().GetProperty("Name")!.GetValue(generatedType.GetProperty("Inventory")!.GetValue(result)));
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }
        }

        [Fact]
        public void SourceGenerator_MixedAttributeParity_UsesReflectionAndGeneratedPathsConsistently()
        {
            const string source = @"
using System;
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class MixedAttributeModel
    {
        [BJsonDefaultValue(7)]
        public int Count { get; set; }

        [BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingDefault)]
        public int Optional { get; set; }

        [BJsonValueMapper(nameof(MapLabel))]
        public string Label { get; set; } = string.Empty;

        public static BJsonValue MapLabel(BJsonValue value, string jsonName, IComparable? version, bool isReading)
            => value.IsString ? BJsonValue.Create(value.StringValue.ToUpperInvariant()) : value;
    }
}";

            var generatedAssembly = CompileWithGenerator(source);
            var generatedType = generatedAssembly.GetType("GeneratedRuntime.MixedAttributeModel", throwOnError: true)!;

            var reflectionInstance = new ReflectionCompatibilityModel
            {
                Count = 3,
                Optional = 0,
                Label = "hello"
            };

            var generatedInstance = Activator.CreateInstance(generatedType)!;
            generatedType.GetProperty("Count")!.SetValue(generatedInstance, 3);
            generatedType.GetProperty("Optional")!.SetValue(generatedInstance, 0);
            generatedType.GetProperty("Label")!.SetValue(generatedInstance, "hello");

            var reflectionSerialized = BJson.Serialize(reflectionInstance);
            var generatedSerialized = BJson.Serialize(generatedInstance, generatedType);

            Assert.True(reflectionSerialized.TryGetObject(out var reflectionObj));
            Assert.True(generatedSerialized.TryGetObject(out var generatedObj));
            Assert.Equal(reflectionObj["Count"].IntValue, generatedObj["Count"].IntValue);
            Assert.Equal(reflectionObj["Label"].StringValue, generatedObj["Label"].StringValue);
            Assert.False(reflectionObj.ContainsKey("Optional"));
            Assert.False(generatedObj.ContainsKey("Optional"));

            var roundTripValue = BJsonValue.Create(new BJsonObject
            {
                ["Count"] = BJsonValue.Create(9),
                ["Label"] = BJsonValue.Create("world")
            });

            var reflectionRoundTrip = BJson.Deserialize(roundTripValue, typeof(ReflectionCompatibilityModel));
            var generatedRoundTrip = BJson.Deserialize(roundTripValue, generatedType);

            Assert.NotNull(reflectionRoundTrip);
            Assert.NotNull(generatedRoundTrip);
            Assert.Equal(9, reflectionRoundTrip!.GetType().GetProperty("Count")!.GetValue(reflectionRoundTrip));
            Assert.Equal(9, generatedType.GetProperty("Count")!.GetValue(generatedRoundTrip));
            Assert.Equal("WORLD", reflectionRoundTrip.GetType().GetProperty("Label")!.GetValue(reflectionRoundTrip));
            Assert.Equal("WORLD", generatedType.GetProperty("Label")!.GetValue(generatedRoundTrip));
        }

        [Fact]
        public void SourceGenerator_NullToken_UsesConfiguredDefault_ForNonNullableValueType()
        {
            const string source = @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class GeneratedNullDefaultModel
    {
        [BJsonDefaultValue(7)]
        public int Count { get; set; }
    }
}";

            var generatedAssembly = CompileWithGenerator(source);
            var generatedType = generatedAssembly.GetType("GeneratedRuntime.GeneratedNullDefaultModel", throwOnError: true)!;

            var json = new BJsonObject
            {
                ["Count"] = BJsonValue.Null
            };

            var result = BJson.Deserialize(BJsonValue.Create(json), generatedType);
            Assert.NotNull(result);
            Assert.Equal(7, generatedType.GetProperty("Count")!.GetValue(result));
        }

        [Fact]
        public void SourceGenerator_WhenWritingCustomDefault_UsesProviderValue()
        {
            const string source = @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class GeneratedCustomDefaultModel
    {
        [BJsonDefaultProvider(nameof(GetDefaultCount))]
        [BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingCustomDefault)]
        public int Count { get; set; }

        internal static int GetDefaultCount() => 42;
    }
}";

            var generatedAssembly = CompileWithGenerator(source);
            var generatedType = generatedAssembly.GetType("GeneratedRuntime.GeneratedCustomDefaultModel", throwOnError: true)!;

            var sameAsProvider = Activator.CreateInstance(generatedType)!;
            generatedType.GetProperty("Count")!.SetValue(sameAsProvider, 42);

            var different = Activator.CreateInstance(generatedType)!;
            generatedType.GetProperty("Count")!.SetValue(different, 0);

            var sameSerialized = BJson.Serialize(sameAsProvider, generatedType);
            var differentSerialized = BJson.Serialize(different, generatedType);

            Assert.True(sameSerialized.TryGetObject(out var sameObj));
            Assert.False(sameObj.ContainsKey("Count"));

            Assert.True(differentSerialized.TryGetObject(out var differentObj));
            Assert.True(differentObj.ContainsKey("Count"));
            Assert.Equal(0, differentObj["Count"].IntValue);
        }

        [Fact]
        public void SourceGenerator_TypeLevelVersion_GuardsMembers_And_ComposesWithMemberVersion()
        {
            const string source = @"
using System;
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    [BJsonVersionContext(typeof(Version), ""2.0.0"")]
    [BJsonVersion(typeof(Version), introducedIn: ""2.0.0"")]
    public sealed class GeneratedTypeVersionedModel
    {
        public int AlwaysInTypeRange { get; set; }

        [BJsonVersion(typeof(Version), removedIn: ""3.0.0"")]
        public int LegacyInType { get; set; }
    }
}";

            var generatedAssembly = CompileWithGenerator(source);
            var generatedType = generatedAssembly.GetType("GeneratedRuntime.GeneratedTypeVersionedModel", throwOnError: true)!;

            var instance = Activator.CreateInstance(generatedType)!;
            generatedType.GetProperty("AlwaysInTypeRange")!.SetValue(instance, 10);
            generatedType.GetProperty("LegacyInType")!.SetValue(instance, 20);

            var belowTypeOptions = new BJsonSerializerOptions { Version = new Version(1, 0, 0) };
            var inTypeOptions = new BJsonSerializerOptions { Version = new Version(2, 0, 0) };
            var removedMemberOptions = new BJsonSerializerOptions { Version = new Version(3, 0, 0) };

            var belowType = BJson.Serialize(instance, generatedType, belowTypeOptions);
            var inType = BJson.Serialize(instance, generatedType, inTypeOptions);
            var removedMember = BJson.Serialize(instance, generatedType, removedMemberOptions);

            Assert.True(belowType.TryGetObject(out var belowObj));
            Assert.False(belowObj.ContainsKey("AlwaysInTypeRange"));
            Assert.False(belowObj.ContainsKey("LegacyInType"));

            Assert.True(inType.TryGetObject(out var inObj));
            Assert.True(inObj.ContainsKey("AlwaysInTypeRange"));
            Assert.True(inObj.ContainsKey("LegacyInType"));

            Assert.True(removedMember.TryGetObject(out var removedObj));
            Assert.True(removedObj.ContainsKey("AlwaysInTypeRange"));
            Assert.False(removedObj.ContainsKey("LegacyInType"));
        }

        [Fact]
        public void SourceGenerator_MultipleFactoryMethods_EmitsError()
        {
            const string source = @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class DuplicateFactoryModel
    {
        public int Value { get; set; }

        [BJsonFactoryMethod]
        public static DuplicateFactoryModel CreateA(int value) => new DuplicateFactoryModel { Value = value };

        [BJsonFactoryMethod]
        public static DuplicateFactoryModel CreateB(int value) => new DuplicateFactoryModel { Value = value };
    }
}";

            var diagnostics = GetGeneratorDiagnostics(source);
            Assert.Contains(diagnostics, d => d.Id == "BJSON014");
        }

        [Fact]
        public void SourceGenerator_InvalidFactorySignature_EmitsError()
        {
            const string source = @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class InvalidFactoryModel
    {
        public int Value { get; set; }

        [BJsonFactoryMethod]
        public static string Create(int value) => value.ToString();
    }
}";

            var diagnostics = GetGeneratorDiagnostics(source);
            Assert.Contains(diagnostics, d => d.Id == "BJSON015");
        }

        [Fact]
        public void SourceGenerator_UnknownFactoryParameterMappingTarget_EmitsWarning()
        {
            const string source = @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class InvalidFactoryMappingModel
    {
        public int X { get; set; }

        [BJsonFactoryMethod(ParameterMapping = new[] { ""missing"", ""coord_x"" })]
        public static InvalidFactoryMappingModel Create(int x) => new InvalidFactoryMappingModel { X = x };
    }
}";

            var diagnostics = GetGeneratorDiagnostics(source);
            Assert.Contains(diagnostics, d => d.Id == "BJSON016");
        }

        [Fact]
        public void SourceGenerator_DuplicateFactoryJsonKeyMapping_EmitsWarning()
        {
            const string source = @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class DuplicateFactoryJsonKeyModel
    {
        public int X { get; set; }
        public int Y { get; set; }

        [BJsonFactoryMethod(ParameterMapping = new[] { ""x"", ""coord"", ""y"", ""coord"" })]
        public static DuplicateFactoryJsonKeyModel Create(int x, int y) => new DuplicateFactoryJsonKeyModel { X = x, Y = y };
    }
}";

            var diagnostics = GetGeneratorDiagnostics(source);
            Assert.Contains(diagnostics, d => d.Id == "BJSON016");
        }

        [Fact]
        public void SourceGenerator_AllActiveDiagnostics_HaveNegativeCoverage()
        {
            var diagnosticSources = new[]
            {
                (Id: "BJSON001", Source: @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class InvalidExtensionDataTypeModel
    {
        [BJsonExtensionData]
        public int Extra { get; set; }
    }
}"),
                (Id: "BJSON002", Source: @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class MultipleCtorModel
    {
        [BJsonConstructor]
        public MultipleCtorModel(int a) { }

        [BJsonConstructor]
        public MultipleCtorModel(string b) { }
    }
}"),
                (Id: "BJSON003", Source: @"
using System.Collections.Generic;
using Krampus.BinJson;
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class MultipleExtensionDataModel
    {
        [BJsonExtensionData]
        public Dictionary<string, BJsonValue>? ExtraA { get; set; }

        [BJsonExtensionData]
        public Dictionary<string, BJsonValue>? ExtraB { get; set; }
    }
}"),
                (Id: "BJSON004", Source: @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    [BJsonConverter(typeof(MissingConverterType))]
    public sealed class MissingConverterModel
    {
        public int Value { get; set; }
    }
}"),
                (Id: "BJSON005", Source: @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class ConflictingNameModel
    {
        [BJsonPropertyName(""value"")]
        public int A { get; set; }

        [BJsonPropertyName(""value"")]
        public int B { get; set; }
    }
}"),
                (Id: "BJSON006", Source: @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class UnmatchedCtorParameterModel
    {
        [BJsonConstructor]
        public UnmatchedCtorParameterModel(int ghost)
        {
            Real = 0;
        }

        public int Real { get; }
    }
}"),
                (Id: "BJSON007", Source: @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class MissingMethodModel
    {
        [BJsonIgnoreWhen(""MissingMethod"")]
        public int Value { get; set; }
    }
}"),
                (Id: "BJSON008", Source: @"
using System;
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class InaccessibleMethodModel
    {
        [BJsonIgnoreWhen(nameof(ShouldIgnore))]
        public int Value { get; set; }

        private static bool ShouldIgnore(object? value, string name, IComparable? version) => false;
    }
}"),
                (Id: "BJSON009", Source: @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class InvalidSignatureMethodModel
    {
        [BJsonIgnoreWhen(nameof(ShouldIgnore))]
        public int Value { get; set; }

        public static int ShouldIgnore(int value) => 0;
    }
}"),
                (Id: "BJSON010", Source: @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    public sealed class Outer
    {
        [BJsonSerializable]
        public sealed class Inner
        {
            public int Value { get; set; }
        }
    }
}"),
                (Id: "BJSON012", Source: @"
using Krampus.BinJson.Serialization;

namespace GeneratedRuntime
{
    [BJsonSerializable]
    public sealed class InvalidFactoryMappingPairsModel
    {
        public int X { get; set; }

        [BJsonFactoryMethod(ParameterMapping = new[] { ""x"", ""coord_x"", ""orphan"" })]
        public static InvalidFactoryMappingPairsModel Create(int x) => new InvalidFactoryMappingPairsModel { X = x };
    }
}")
            };

            foreach (var item in diagnosticSources)
            {
                var diagnostics = GetGeneratorDiagnostics(item.Source);
                Assert.Contains(diagnostics, d => d.Id == item.Id);
            }
        }

        private sealed class ReflectionCompatibilityModel
        {
            [BJsonDefaultValue(7)]
            public int Count { get; set; }

            [BJsonIgnore(Condition = BJsonIgnoreCondition.WhenWritingDefault)]
            public int Optional { get; set; }

            [BJsonValueMapper(nameof(MapLabel))]
            public string Label { get; set; } = string.Empty;

            internal static BJsonValue MapLabel(BJsonValue value, string jsonName, IComparable? version, bool isReading)
                => value.IsString ? BJsonValue.Create(value.StringValue.ToUpperInvariant()) : value;
        }

        private static Diagnostic[] GetGeneratorDiagnostics(string userSource)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(userSource);
            var compilation = CSharpCompilation.Create(
                assemblyName: "GeneratedRuntimeDiagnostics",
                syntaxTrees: new[] { syntaxTree },
                references: SharedReferences,
                options: SharedLibraryCompilationOptions);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(SharedSourceGenerator);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var _, out var diagnostics);

            return diagnostics.ToArray();
        }

        private static Assembly CompileWithGenerator(string userSource)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(userSource);
            var compilation = CSharpCompilation.Create(
                assemblyName: "GeneratedRuntimeTests",
                syntaxTrees: new[] { syntaxTree },
                references: SharedReferences,
                options: SharedLibraryCompilationOptions);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(SharedSourceGenerator);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Empty(updatedCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

            using var stream = new System.IO.MemoryStream();
            var emitResult = updatedCompilation.Emit(stream);
            Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Select(d => d.ToString())));
            stream.Position = 0;
            return Assembly.Load(stream.ToArray());
        }

        private static MetadataReference[] BuildSharedReferences()
        {
            var references = new List<MetadataReference>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.Location))
                    continue;

                if (!seen.Add(assembly.Location))
                    continue;

                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }

            return references.ToArray();
        }
    }
}
