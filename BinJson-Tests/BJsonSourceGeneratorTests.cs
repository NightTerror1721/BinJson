using System;
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
        [Fact]
        public void CodeEmitter_EmitSerializer_GeneratesBridgeBasedSerializer()
        {
            var config = new TypeConfiguration(false, false, Krampus.BinJson.SourceGenerators.Models.NamingPolicy.Default);
            var model = new GeneratedTypeModel("Demo.Models", "PlayerState", false, config);

            var source = CodeEmitter.EmitSerializer(model);

            Assert.Contains("namespace Demo.Models", source, StringComparison.Ordinal);
            Assert.Contains("internal sealed class PlayerState_BJsonSerializer : BJsonConverter<PlayerState>", source, StringComparison.Ordinal);
            // TODO: Update assertions once new code generation is implemented
        }

        [Fact]
        public void CodeEmitter_EmitSerializer_ForGlobalNamespace_DoesNotEmitIndentedTopLevelType()
        {
            var config = new TypeConfiguration(false, false, Krampus.BinJson.SourceGenerators.Models.NamingPolicy.Default);
            var model = new GeneratedTypeModel(string.Empty, "PlayerState", false, config);

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

        private static Assembly CompileWithGenerator(string userSource)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(userSource);
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName: "GeneratedRuntimeTests",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            ISourceGenerator generator = new BJsonSourceGenerator().AsSourceGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Empty(updatedCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

            using var stream = new System.IO.MemoryStream();
            var emitResult = updatedCompilation.Emit(stream);
            Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics.Select(d => d.ToString())));
            stream.Position = 0;
            return Assembly.Load(stream.ToArray());
        }
    }
}
