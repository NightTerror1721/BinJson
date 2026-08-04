using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Krampus.BinJson;
using Krampus.BinJson.Binary;
using Krampus.BinJson.Serialization;
using Krampus.BinJson.Text;

const int warmupIterations = 24;
const int fastIterations = 5000;
const int mediumIterations = 1500;
const int advancedIterations = 320;

var smallObject = CreateSmallObject();
var repeatedStringsObject = CreateRepeatedStringsObject();
var packedNumbers = CreatePackedNumberArray(8192);
var binaryBlob = CreateBinaryBlob(64 * 1024);
var wideRootObject = CreateWideRootObject(128, 8);
var reflectionProfile = CreateReflectionProfile();
var generatedProfile = CreateGeneratedProfile();
var attributedReflectionProfile = CreateAttributedReflectionProfile();
var attributedGeneratedProfile = CreateAttributedGeneratedProfile();
var advancedReflectionProfile = CreateAdvancedReflectionProfile();
var advancedGeneratedProfile = CreateAdvancedGeneratedProfile();
var reflectionPolymorphicActor = CreateReflectionPolymorphicActor();
var generatedPolymorphicActor = CreateGeneratedPolymorphicActor();

var sprint7TempDirectory = Path.Combine(Path.GetTempPath(), "binjson-perf-sprint7-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sprint7TempDirectory);

var sprint7Context = new BJsonPreprocessorContext
{
    BasePath = sprint7TempDirectory
};
sprint7Context.SetVariable("Platform", "Desktop");
sprint7Context.SetVariable("ModeToken", "boost");

var sprint7Options = new BJsonSerializerOptions
{
    PreprocessorContext = sprint7Context
};

var sprint7WriteOptions = new BJsonSerializerOptions
{
    PreprocessorContext = new BJsonPreprocessorContext
    {
        BasePath = sprint7TempDirectory
    }
};

var externalStatePath = Path.Combine(sprint7TempDirectory, "state.bjson");
var fixedExternalStatePath = Path.Combine(sprint7TempDirectory, "state-fixed.bjson");
BJson.SerializeToFile(externalStatePath, CreateAdvancedExternalStateDocument());
BJson.SerializeToFile(fixedExternalStatePath, CreateAdvancedExternalStateDocument());

var advancedPreprocessorPayload = CreateAdvancedPreprocessorPayload(externalStatePath);
var advancedExternalFixedPathPayload = CreateAdvancedExternalFixedPathPayload();

var repeatedWithTableOptions = new BJsonBinaryWriterOptions { EnableStringTable = true, EnablePackedArrays = true };
var repeatedWithoutTableOptions = new BJsonBinaryWriterOptions { EnableStringTable = false, EnablePackedArrays = true };
var packedArrayOptions = new BJsonBinaryWriterOptions { EnableStringTable = false, EnablePackedArrays = true };
var binaryBlobOptions = new BJsonBinaryWriterOptions { EnableStringTable = false, EnablePackedArrays = false };

byte[] smallObjectBytes = BJsonBinaryWriter.Serialize(smallObject, BJsonBinaryWriterOptions.Default);
byte[] repeatedWithTableBytes = BJsonBinaryWriter.Serialize(repeatedStringsObject, repeatedWithTableOptions);
byte[] repeatedWithoutTableBytes = BJsonBinaryWriter.Serialize(repeatedStringsObject, repeatedWithoutTableOptions);
byte[] packedNumbersBytes = BJsonBinaryWriter.Serialize(packedNumbers, packedArrayOptions);
byte[] binaryBlobBytes = BJsonBinaryWriter.Serialize(binaryBlob, binaryBlobOptions);
byte[] wideRootObjectBytes = BJsonBinaryWriter.Serialize(wideRootObject, repeatedWithoutTableOptions);

string smallObjectJson = BJsonTextWriter.Serialize(smallObject);
string repeatedObjectJson = BJsonTextWriter.Serialize(repeatedStringsObject);
string wideRootObjectJson = BJsonTextWriter.Serialize(wideRootObject);

RunTextSerializeScenario(
    "Text small object serialize",
    smallObject,
    fastIterations,
    BJsonTextWriterOptions.Default);

RunTextParseScenario(
    "Text small object parse DOM",
    smallObjectJson,
    fastIterations,
    static json => BJsonTextReader.Deserialize(json).ObjectValue.Count);

RunTextVisitScenario(
    "Text small object visit no DOM",
    smallObjectJson,
    fastIterations);

RunTextSerializeScenario(
    "Text repeated object serialize compact",
    repeatedStringsObject,
    mediumIterations,
    BJsonTextWriterOptions.Default);

RunTextSerializeScenario(
    "Text repeated object serialize pretty",
    repeatedStringsObject,
    mediumIterations,
    BJsonTextWriterOptions.PrettyPrint);

RunTextParseScenario(
    "Text repeated object parse DOM",
    repeatedObjectJson,
    mediumIterations,
    static json => BJsonTextReader.Deserialize(json).ObjectValue.Count);

RunTextParseScenario(
    "Text wide root parse DOM",
    wideRootObjectJson,
    mediumIterations,
    static json => BJsonTextReader.Deserialize(json).ObjectValue.Count);

RunTextVisitScenario(
    "Text repeated object visit no DOM",
    repeatedObjectJson,
    mediumIterations);

RunTextSelectiveRootPropertyScenario(
    "Text wide root selective property read",
    wideRootObjectJson,
    mediumIterations,
    "section_127");

RunTextSelectiveRootPropertiesScenario(
    "Text wide root selective 8 properties (one pass)",
    wideRootObjectJson,
    mediumIterations,
    new[] { "section_0", "section_8", "section_16", "section_32", "section_48", "section_64", "section_96", "section_127" });

RunTextAsyncScenario(
    "Text async write to StringWriter",
    repeatedStringsObject,
    mediumIterations,
    BJsonTextWriterOptions.Default,
    static async (value, options) =>
    {
        using var sw = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        using var writer = new BJsonTextWriterAsync(sw, options, leaveOpen: true);
        await writer.WriteAsync(value).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
        return sw.GetStringBuilder().Length;
    }).GetAwaiter().GetResult();

RunTextAsyncScenario(
    "Text async write to MemoryStream UTF8",
    repeatedStringsObject,
    mediumIterations,
    BJsonTextWriterOptions.Default,
    static async (value, options) =>
    {
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
        using var writer = new BJsonTextWriterAsync(sw, options, leaveOpen: true);
        await writer.WriteAsync(value).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
        await sw.FlushAsync().ConfigureAwait(false);
        return (int)ms.Length;
    }).GetAwaiter().GetResult();

RunTextAsyncReadScenario(
    "Text async parse from stream",
    repeatedObjectJson,
    mediumIterations,
    static async json =>
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        using var ms = new MemoryStream(bytes);
        var value = await BJsonTextReaderAsync.DeserializeAsync(ms).ConfigureAwait(false);
        return value.ObjectValue.Count;
    }).GetAwaiter().GetResult();

RunSerializeScenario(
    "Small object serialize",
    smallObject,
    fastIterations,
    BJsonBinaryWriterOptions.Default);

RunDeserializeScenario(
    "Small object deserialize DOM",
    smallObjectBytes,
    fastIterations,
    static data => BJsonBinaryReader.Deserialize(data).ObjectValue.Count);

RunVisitScenario(
    "Small object visit no DOM",
    smallObjectBytes,
    fastIterations);

RunSerializeScenario(
    "Repeated strings serialize with string table",
    repeatedStringsObject,
    mediumIterations,
    repeatedWithTableOptions);

RunDeserializeScenario(
    "Repeated strings deserialize DOM with string table",
    repeatedWithTableBytes,
    mediumIterations,
    static data => BJsonBinaryReader.Deserialize(data).ObjectValue.Count);

RunVisitScenario(
    "Repeated strings visit no DOM with string table",
    repeatedWithTableBytes,
    mediumIterations);

RunSerializeScenario(
    "Repeated strings serialize without string table",
    repeatedStringsObject,
    mediumIterations,
    repeatedWithoutTableOptions);

RunDeserializeScenario(
    "Repeated strings deserialize DOM without string table",
    repeatedWithoutTableBytes,
    mediumIterations,
    static data => BJsonBinaryReader.Deserialize(data).ObjectValue.Count);

RunVisitScenario(
    "Repeated strings visit no DOM without string table",
    repeatedWithoutTableBytes,
    mediumIterations);

RunSerializeScenario(
    "Packed numeric array serialize",
    packedNumbers,
    mediumIterations,
    packedArrayOptions);

RunDeserializeScenario(
    "Packed numeric array deserialize DOM",
    packedNumbersBytes,
    mediumIterations,
    static data => BJsonBinaryReader.Deserialize(data).ArrayValue.Count);

RunVisitScenario(
    "Packed numeric array visit no DOM",
    packedNumbersBytes,
    mediumIterations);

RunSerializeScenario(
    "Large binary payload serialize",
    binaryBlob,
    mediumIterations,
    binaryBlobOptions);

RunDeserializeScenario(
    "Large binary payload deserialize DOM",
    binaryBlobBytes,
    mediumIterations,
    static data => BJsonBinaryReader.Deserialize(data).BinaryValue.Count);

RunVisitScenario(
    "Large binary payload visit no DOM",
    binaryBlobBytes,
    mediumIterations);

RunDeserializeScenario(
    "Wide root object deserialize DOM",
    wideRootObjectBytes,
    mediumIterations,
    static data => BJsonBinaryReader.Deserialize(data).ObjectValue.Count);

RunSelectiveRootPropertyScenario(
    "Wide root object selective property read",
    wideRootObjectBytes,
    mediumIterations,
    "section_127");

RunSelectiveRootPropertiesScenario(
    "Wide root object selective 8 properties (one pass)",
    wideRootObjectBytes,
    mediumIterations,
    new[] { "section_0", "section_8", "section_16", "section_32", "section_48", "section_64", "section_96", "section_127" });

RunAsyncScenario(
    "Async stream write serialize to MemoryStream",
    repeatedStringsObject,
    mediumIterations,
    repeatedWithTableOptions,
    static async (value, writeOptions) =>
    {
        using var stream = new MemoryStream();
        using var writer = new BJsonBinaryWriterAsync(stream, leaveOpen: true, writeOptions);
        await writer.WriteAsync(value).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
        return (int)stream.Length;
    }).GetAwaiter().GetResult();

RunAsyncScenario(
    "Async read from ReadOnlyMemory to DOM",
    repeatedStringsObject,
    mediumIterations,
    repeatedWithTableOptions,
    static async (value, writeOptions) =>
    {
        byte[] data = BJsonBinaryWriter.Serialize(value, writeOptions);
        _ = await BJsonBinaryReaderAsync.DeserializeAsync(data.AsMemory()).ConfigureAwait(false);
        return data.Length;
    }).GetAwaiter().GetResult();

RunBinaryAsyncReadScenario(
    "Async read from stream to DOM",
    repeatedWithTableBytes,
    mediumIterations,
    static async payload =>
    {
        using var ms = new MemoryStream(payload, writable: false);
        var value = await BJsonBinaryReaderAsync.DeserializeAsync(ms).ConfigureAwait(false);
        return value.ObjectValue.Count;
    }).GetAwaiter().GetResult();

RunClrSerializeScenario(
    "CLR reflection serialize",
    reflectionProfile,
    typeof(ReflectionProfile),
    mediumIterations);

RunClrSerializeScenario(
    "CLR generated serialize",
    generatedProfile,
    typeof(GeneratedProfile),
    mediumIterations);

var reflectionSerialized = BJson.Serialize(reflectionProfile, typeof(ReflectionProfile));
var generatedSerialized = BJson.Serialize(generatedProfile, typeof(GeneratedProfile));

RunClrDeserializeScenario(
    "CLR reflection deserialize",
    reflectionSerialized,
    typeof(ReflectionProfile),
    mediumIterations);

RunClrDeserializeScenario(
    "CLR generated deserialize",
    generatedSerialized,
    typeof(GeneratedProfile),
    mediumIterations);

RunClrSerializeScenario(
    "CLR attributed reflection serialize",
    attributedReflectionProfile,
    typeof(AttributedReflectionProfile),
    mediumIterations);

RunClrSerializeScenario(
    "CLR attributed generated serialize",
    attributedGeneratedProfile,
    typeof(AttributedGeneratedProfile),
    mediumIterations);

var attributedPayloadMissingDefaults = CreateAttributedPayloadWithoutDefaults();

RunClrDeserializeScenario(
    "CLR attributed reflection deserialize missing defaults",
    attributedPayloadMissingDefaults,
    typeof(AttributedReflectionProfile),
    mediumIterations);

RunClrDeserializeScenario(
    "CLR attributed generated deserialize missing defaults",
    attributedPayloadMissingDefaults,
    typeof(AttributedGeneratedProfile),
    mediumIterations);

RunClrSerializeScenario(
    "CLR advanced mapper/default reflection serialize",
    advancedReflectionProfile,
    typeof(AdvancedReflectionProfile),
    mediumIterations);

RunClrSerializeScenario(
    "CLR advanced mapper/default generated serialize",
    advancedGeneratedProfile,
    typeof(AdvancedGeneratedProfile),
    mediumIterations);

var advancedReflectionSerialized = BJson.Serialize(advancedReflectionProfile, typeof(AdvancedReflectionProfile));
var advancedGeneratedSerialized = BJson.Serialize(advancedGeneratedProfile, typeof(AdvancedGeneratedProfile));

RunClrDeserializeScenario(
    "CLR advanced mapper/default reflection deserialize",
    advancedReflectionSerialized,
    typeof(AdvancedReflectionProfile),
    mediumIterations);

RunClrDeserializeScenario(
    "CLR advanced mapper/default generated deserialize",
    advancedGeneratedSerialized,
    typeof(AdvancedGeneratedProfile),
    mediumIterations);

RunClrDeserializeScenarioWithOptions(
    "CLR advanced preprocess reflection deserialize",
    advancedPreprocessorPayload,
    typeof(AdvancedReflectionConfig),
    advancedIterations,
    sprint7Options);

RunClrDeserializeScenarioWithOptions(
    "CLR advanced preprocess generated deserialize",
    advancedPreprocessorPayload,
    typeof(AdvancedGeneratedConfig),
    advancedIterations,
    sprint7Options);

RunClrDeserializeScenarioWithOptions(
    "CLR advanced external-ref fixed reflection deserialize",
    advancedExternalFixedPathPayload,
    typeof(AdvancedReflectionExternalRefConfig),
    advancedIterations,
    sprint7Options);

RunClrDeserializeScenarioWithOptions(
    "CLR advanced external-ref fixed generated deserialize",
    advancedExternalFixedPathPayload,
    typeof(AdvancedGeneratedExternalRefConfig),
    advancedIterations,
    sprint7Options);

RunClrSerializeScenarioWithOptions(
    "CLR advanced external-ref fixed reflection serialize",
    new AdvancedReflectionExternalRefConfig { State = new AdvancedExternalState { Flags = "steady", Power = 17 } },
    typeof(AdvancedReflectionExternalRefConfig),
    advancedIterations,
    sprint7WriteOptions);

RunClrSerializeScenarioWithOptions(
    "CLR advanced external-ref fixed generated serialize",
    new AdvancedGeneratedExternalRefConfig { State = new AdvancedExternalState { Flags = "steady", Power = 17 } },
    typeof(AdvancedGeneratedExternalRefConfig),
    advancedIterations,
    sprint7WriteOptions);

RunClrSerializeScenario(
    "CLR advanced polymorphic reflection serialize",
    reflectionPolymorphicActor,
    typeof(ReflectionActorBase),
    mediumIterations);

RunClrSerializeScenario(
    "CLR advanced polymorphic generated serialize",
    generatedPolymorphicActor,
    typeof(GeneratedActorBase),
    mediumIterations);

var reflectionPolymorphicSerialized = BJson.Serialize(reflectionPolymorphicActor, typeof(ReflectionActorBase));
var generatedPolymorphicSerialized = BJson.Serialize(generatedPolymorphicActor, typeof(GeneratedActorBase));

RunClrDeserializeScenario(
    "CLR advanced polymorphic reflection deserialize",
    reflectionPolymorphicSerialized,
    typeof(ReflectionActorBase),
    mediumIterations);

RunClrDeserializeScenario(
    "CLR advanced polymorphic generated deserialize",
    generatedPolymorphicSerialized,
    typeof(GeneratedActorBase),
    mediumIterations);

PrintComparisonSummary(BenchmarkStore.Results);

if (Directory.Exists(sprint7TempDirectory))
    Directory.Delete(sprint7TempDirectory, recursive: true);

static void RunSerializeScenario(
    string name,
    BJsonValue value,
    int iterations,
    BJsonBinaryWriterOptions writeOptions)
{
    Warmup(warmupIterations, () => BJsonBinaryWriter.Serialize(value, writeOptions));

    int payloadSize = BJsonBinaryWriter.Serialize(value, writeOptions).Length;
    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        _ = BJsonBinaryWriter.Serialize(value, writeOptions);
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        _ = BJsonBinaryWriter.Serialize(value, writeOptions);
    });

    PrintResult(name, iterations, payloadSize, elapsed, allocatedBytes);
}

static void RunTextSerializeScenario(string name, BJsonValue value, int iterations, BJsonTextWriterOptions options)
{
    Warmup(warmupIterations, () => BJsonTextWriter.Serialize(value, options));

    int payloadSize = BJsonTextWriter.Serialize(value, options).Length;
    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        _ = BJsonTextWriter.Serialize(value, options);
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        _ = BJsonTextWriter.Serialize(value, options);
    });

    PrintResult(name, iterations, payloadSize, elapsed, allocatedBytes);
}

static void RunTextParseScenario(string name, string json, int iterations, Func<string, int> action)
{
    Warmup(warmupIterations, () => action(json));

    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        _ = action(json);
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        _ = action(json);
    });

    PrintResult(name, iterations, json.Length, elapsed, allocatedBytes);
}

static void RunTextVisitScenario(string name, string json, int iterations)
{
    Warmup(warmupIterations, () =>
    {
        var visitor = new CountingTextVisitor();
        BJsonTextReader.Visit(json, visitor);
        _ = visitor.ScalarCount;
    });

    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        var visitor = new CountingTextVisitor();
        BJsonTextReader.Visit(json, visitor);
        _ = visitor.ScalarCount;
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        var visitor = new CountingTextVisitor();
        BJsonTextReader.Visit(json, visitor);
        _ = visitor.ScalarCount;
    });

    PrintResult(name, iterations, json.Length, elapsed, allocatedBytes);
}

static void RunTextSelectiveRootPropertyScenario(string name, string json, int iterations, string propertyName)
{
    Warmup(warmupIterations, () =>
    {
        _ = BJsonTextReader.TryReadRootObjectProperty(json, propertyName, out var value);
        _ = value.Type;
    });

    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        _ = BJsonTextReader.TryReadRootObjectProperty(json, propertyName, out var value);
        _ = value.Type;
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        _ = BJsonTextReader.TryReadRootObjectProperty(json, propertyName, out var value);
        _ = value.Type;
    });

    PrintResult(name, iterations, json.Length, elapsed, allocatedBytes);
}

static void RunTextSelectiveRootPropertiesScenario(string name, string json, int iterations, string[] propertyNames)
{
    Warmup(warmupIterations, () =>
    {
        BJsonObject selected = BJsonTextReader.ReadRootObjectProperties(json, propertyNames);
        _ = selected.Count;
    });

    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        BJsonObject selected = BJsonTextReader.ReadRootObjectProperties(json, propertyNames);
        _ = selected.Count;
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        BJsonObject selected = BJsonTextReader.ReadRootObjectProperties(json, propertyNames);
        _ = selected.Count;
    });

    PrintResult(name, iterations, json.Length, elapsed, allocatedBytes);
}

static async Task RunTextAsyncScenario(
    string name,
    BJsonValue value,
    int iterations,
    BJsonTextWriterOptions options,
    Func<BJsonValue, BJsonTextWriterOptions, Task<int>> action)
{
    await WarmupAsync(warmupIterations, () => action(value, options)).ConfigureAwait(false);

    int payloadSize = await action(value, options).ConfigureAwait(false);
    long allocatedBytes = await MeasureAllocatedBytesAsync(iterations, () => action(value, options)).ConfigureAwait(false);
    TimeSpan elapsed = await MeasureElapsedAsync(iterations, () => action(value, options)).ConfigureAwait(false);

    PrintResult(name, iterations, payloadSize, elapsed, allocatedBytes);
}

static async Task RunTextAsyncReadScenario(string name, string json, int iterations, Func<string, Task<int>> action)
{
    await WarmupAsync(warmupIterations, () => action(json)).ConfigureAwait(false);

    long allocatedBytes = await MeasureAllocatedBytesAsync(iterations, () => action(json)).ConfigureAwait(false);
    TimeSpan elapsed = await MeasureElapsedAsync(iterations, () => action(json)).ConfigureAwait(false);

    PrintResult(name, iterations, json.Length, elapsed, allocatedBytes);
}

static async Task RunBinaryAsyncReadScenario(string name, byte[] payload, int iterations, Func<byte[], Task<int>> action)
{
    await WarmupAsync(warmupIterations, () => action(payload)).ConfigureAwait(false);

    long allocatedBytes = await MeasureAllocatedBytesAsync(iterations, () => action(payload)).ConfigureAwait(false);
    TimeSpan elapsed = await MeasureElapsedAsync(iterations, () => action(payload)).ConfigureAwait(false);

    PrintResult(name, iterations, payload.Length, elapsed, allocatedBytes);
}

static void RunClrSerializeScenario(string name, object value, Type declaredType, int iterations)
{
    Warmup(warmupIterations, () => BJson.Serialize(value, declaredType));

    int payloadSize = BJsonBinaryWriter.Serialize(BJson.Serialize(value, declaredType)).Length;
    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        _ = BJson.Serialize(value, declaredType);
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        _ = BJson.Serialize(value, declaredType);
    });

    PrintResult(name, iterations, payloadSize, elapsed, allocatedBytes);
}

static void RunClrSerializeScenarioWithOptions(string name, object value, Type declaredType, int iterations, BJsonSerializerOptions options)
{
    Warmup(warmupIterations, () => BJson.Serialize(value, declaredType, options));

    int payloadSize = BJsonBinaryWriter.Serialize(BJson.Serialize(value, declaredType, options)).Length;
    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        _ = BJson.Serialize(value, declaredType, options);
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        _ = BJson.Serialize(value, declaredType, options);
    });

    PrintResult(name, iterations, payloadSize, elapsed, allocatedBytes);
}

static void RunClrDeserializeScenario(string name, BJsonValue payload, Type targetType, int iterations)
{
    Warmup(warmupIterations, () => BJson.Deserialize(payload, targetType));

    int payloadSize = BJsonBinaryWriter.Serialize(payload).Length;
    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        _ = BJson.Deserialize(payload, targetType);
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        _ = BJson.Deserialize(payload, targetType);
    });

    PrintResult(name, iterations, payloadSize, elapsed, allocatedBytes);
}

static void RunClrDeserializeScenarioWithOptions(string name, BJsonValue payload, Type targetType, int iterations, BJsonSerializerOptions options)
{
    Warmup(warmupIterations, () => BJson.Deserialize(payload, targetType, options));

    int payloadSize = BJsonBinaryWriter.Serialize(payload).Length;
    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        _ = BJson.Deserialize(payload, targetType, options);
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        _ = BJson.Deserialize(payload, targetType, options);
    });

    PrintResult(name, iterations, payloadSize, elapsed, allocatedBytes);
}

static void RunDeserializeScenario(
    string name,
    byte[] payload,
    int iterations,
    Func<ReadOnlyMemory<byte>, int> action)
{
    ReadOnlyMemory<byte> memory = payload.AsMemory();
    Warmup(warmupIterations, () => action(memory));

    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        _ = action(memory);
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        _ = action(memory);
    });

    PrintResult(name, iterations, payload.Length, elapsed, allocatedBytes);
}

static void RunVisitScenario(string name, byte[] payload, int iterations)
{
    ReadOnlyMemory<byte> memory = payload.AsMemory();
    Warmup(warmupIterations, () =>
    {
        var visitor = new CountingBinaryVisitor();
        BJsonBinaryReader.Visit(memory, visitor);
        _ = visitor.ScalarCount;
    });

    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        var visitor = new CountingBinaryVisitor();
        BJsonBinaryReader.Visit(memory, visitor);
        _ = visitor.ScalarCount;
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        var visitor = new CountingBinaryVisitor();
        BJsonBinaryReader.Visit(memory, visitor);
        _ = visitor.ScalarCount;
    });

    PrintResult(name, iterations, payload.Length, elapsed, allocatedBytes);
}

static void RunSelectiveRootPropertyScenario(string name, byte[] payload, int iterations, string propertyName)
{
    ReadOnlyMemory<byte> memory = payload.AsMemory();
    Warmup(warmupIterations, () =>
    {
        _ = BJsonBinaryReader.TryReadRootObjectProperty(memory, propertyName, out var value);
        _ = value.Type;
    });

    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        _ = BJsonBinaryReader.TryReadRootObjectProperty(memory, propertyName, out var value);
        _ = value.Type;
    });
    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        _ = BJsonBinaryReader.TryReadRootObjectProperty(memory, propertyName, out var value);
        _ = value.Type;
    });

    PrintResult(name, iterations, payload.Length, elapsed, allocatedBytes);
}

static void RunSelectiveRootPropertiesScenario(string name, byte[] payload, int iterations, string[] propertyNames)
{
    ReadOnlyMemory<byte> memory = payload.AsMemory();
    Warmup(warmupIterations, () =>
    {
        BJsonObject selected = BJsonBinaryReader.ReadRootObjectProperties(memory, propertyNames);
        _ = selected.Count;
    });

    long allocatedBytes = MeasureAllocatedBytes(iterations, () =>
    {
        BJsonObject selected = BJsonBinaryReader.ReadRootObjectProperties(memory, propertyNames);
        _ = selected.Count;
    });

    TimeSpan elapsed = MeasureElapsed(iterations, () =>
    {
        BJsonObject selected = BJsonBinaryReader.ReadRootObjectProperties(memory, propertyNames);
        _ = selected.Count;
    });

    PrintResult(name, iterations, payload.Length, elapsed, allocatedBytes);
}

static async Task RunAsyncScenario(
    string name,
    BJsonValue value,
    int iterations,
    BJsonBinaryWriterOptions writeOptions,
    Func<BJsonValue, BJsonBinaryWriterOptions, Task<int>> action)
{
    await WarmupAsync(warmupIterations, () => action(value, writeOptions)).ConfigureAwait(false);

    int payloadSize = await action(value, writeOptions).ConfigureAwait(false);
    long allocatedBytes = await MeasureAllocatedBytesAsync(iterations, () => action(value, writeOptions)).ConfigureAwait(false);
    TimeSpan elapsed = await MeasureElapsedAsync(iterations, () => action(value, writeOptions)).ConfigureAwait(false);

    PrintResult(name, iterations, payloadSize, elapsed, allocatedBytes);
}

static void PrintResult(string name, int iterations, int payloadSize, TimeSpan elapsed, long allocatedBytes)
{
    double opsPerSecond = iterations / elapsed.TotalSeconds;
    double bytesPerOp = allocatedBytes / (double)iterations;

    BenchmarkStore.Results[name] = (name, iterations, payloadSize, elapsed, allocatedBytes, opsPerSecond, bytesPerOp);

    Console.WriteLine(name);
    Console.WriteLine($"  Payload size: {payloadSize:N0} bytes");
    Console.WriteLine($"  Time: {elapsed.TotalMilliseconds:N2} ms for {iterations:N0} iterations");
    Console.WriteLine($"  Throughput: {opsPerSecond:N0} ops/s");
    Console.WriteLine($"  Allocations: {allocatedBytes:N0} bytes total ({bytesPerOp:N1} B/op)");
    Console.WriteLine();

    string safeName = name.Replace("|", "/", StringComparison.Ordinal);
    Console.WriteLine(
        string.Format(
            CultureInfo.InvariantCulture,
            "RESULT|{0}|{1}|{2}|{3:F6}|{4:F6}|{5:F6}",
            safeName,
            iterations,
            payloadSize,
            elapsed.TotalMilliseconds,
            opsPerSecond,
            bytesPerOp));
}

static void PrintComparisonSummary(Dictionary<string, (string Name, int Iterations, int PayloadSize, TimeSpan Elapsed, long AllocatedBytes, double ThroughputOpsPerSecond, double BytesPerOp)> results)
{
    Console.WriteLine("==================== COMPARISON SUMMARY ====================");
    Console.WriteLine();

    Console.WriteLine("Text mode comparisons");
    PrintComparison(results, "Text small parse DOM -> visit", "Text small object parse DOM", "Text small object visit no DOM");
    PrintComparison(results, "Text repeated parse DOM -> visit", "Text repeated object parse DOM", "Text repeated object visit no DOM");
    PrintComparison(results, "Text wide parse DOM -> selective 1", "Text wide root parse DOM", "Text wide root selective property read");
    PrintComparison(results, "Text wide parse DOM -> selective 8", "Text wide root parse DOM", "Text wide root selective 8 properties (one pass)");
    PrintComparison(results, "Text repeated sync parse DOM -> async parse", "Text repeated object parse DOM", "Text async parse from stream");
    PrintComparison(results, "Text repeated sync serialize -> async write", "Text repeated object serialize compact", "Text async write to StringWriter");
    PrintComparison(results, "Text async write StringWriter -> MemoryStream", "Text async write to StringWriter", "Text async write to MemoryStream UTF8");
    Console.WriteLine();

    Console.WriteLine("Binary mode comparisons");
    PrintComparison(results, "Binary small DOM -> visit", "Small object deserialize DOM", "Small object visit no DOM");
    PrintComparison(results, "Binary repeated (table) DOM -> visit", "Repeated strings deserialize DOM with string table", "Repeated strings visit no DOM with string table");
    PrintComparison(results, "Binary repeated (no table) DOM -> visit", "Repeated strings deserialize DOM without string table", "Repeated strings visit no DOM without string table");
    PrintComparison(results, "Binary packed DOM -> visit", "Packed numeric array deserialize DOM", "Packed numeric array visit no DOM");
    PrintComparison(results, "Binary large payload DOM -> visit", "Large binary payload deserialize DOM", "Large binary payload visit no DOM");
    PrintComparison(results, "Binary wide DOM -> selective 1", "Wide root object deserialize DOM", "Wide root object selective property read");
    PrintComparison(results, "Binary wide DOM -> selective 8", "Wide root object deserialize DOM", "Wide root object selective 8 properties (one pass)");
    PrintComparison(results, "Binary repeated sync read DOM -> async read DOM", "Repeated strings deserialize DOM with string table", "Async read from ReadOnlyMemory to DOM");
    PrintComparison(results, "Binary repeated sync read DOM -> async read stream", "Repeated strings deserialize DOM with string table", "Async read from stream to DOM");
    PrintComparison(results, "Binary repeated sync serialize -> async write", "Repeated strings serialize with string table", "Async stream write serialize to MemoryStream");
    Console.WriteLine();

    Console.WriteLine("Text vs Binary comparisons");
    PrintComparison(results, "Small serialize: Text vs Binary", "Text small object serialize", "Small object serialize");
    PrintComparison(results, "Small parse DOM: Text vs Binary", "Text small object parse DOM", "Small object deserialize DOM");
    PrintComparison(results, "Small visit no DOM: Text vs Binary", "Text small object visit no DOM", "Small object visit no DOM");
    PrintComparison(results, "Repeated serialize: Text compact vs Binary with table", "Text repeated object serialize compact", "Repeated strings serialize with string table");
    PrintComparison(results, "Repeated serialize: Text compact vs Binary without table", "Text repeated object serialize compact", "Repeated strings serialize without string table");
    PrintComparison(results, "Repeated parse DOM: Text vs Binary with table", "Text repeated object parse DOM", "Repeated strings deserialize DOM with string table");
    PrintComparison(results, "Repeated parse DOM: Text vs Binary without table", "Text repeated object parse DOM", "Repeated strings deserialize DOM without string table");
    PrintComparison(results, "Repeated visit no DOM: Text vs Binary with table", "Text repeated object visit no DOM", "Repeated strings visit no DOM with string table");
    PrintComparison(results, "Repeated visit no DOM: Text vs Binary without table", "Text repeated object visit no DOM", "Repeated strings visit no DOM without string table");
    PrintComparison(results, "Wide selective 1: Text vs Binary", "Text wide root selective property read", "Wide root object selective property read");
    PrintComparison(results, "Wide selective 8: Text vs Binary", "Text wide root selective 8 properties (one pass)", "Wide root object selective 8 properties (one pass)");
    PrintComparison(results, "Async write: Text vs Binary", "Text async write to StringWriter", "Async stream write serialize to MemoryStream");
    PrintComparison(results, "Async write stream parity: Text vs Binary", "Text async write to MemoryStream UTF8", "Async stream write serialize to MemoryStream");
    PrintComparison(results, "Async parse/read DOM: Text vs Binary", "Text async parse from stream", "Async read from ReadOnlyMemory to DOM");
    PrintComparison(results, "Async parse/read stream parity: Text vs Binary", "Text async parse from stream", "Async read from stream to DOM");
    Console.WriteLine();

    Console.WriteLine("CLR serializer comparisons");
    PrintComparison(results, "CLR serialize: generated vs reflection", "CLR reflection serialize", "CLR generated serialize");
    PrintComparison(results, "CLR deserialize: generated vs reflection", "CLR reflection deserialize", "CLR generated deserialize");
    PrintComparison(results, "CLR attributed serialize: generated vs reflection", "CLR attributed reflection serialize", "CLR attributed generated serialize");
    PrintComparison(results, "CLR attributed deserialize missing defaults: generated vs reflection", "CLR attributed reflection deserialize missing defaults", "CLR attributed generated deserialize missing defaults");
    PrintComparison(results, "CLR advanced mapper/default serialize: generated vs reflection", "CLR advanced mapper/default reflection serialize", "CLR advanced mapper/default generated serialize");
    PrintComparison(results, "CLR advanced mapper/default deserialize: generated vs reflection", "CLR advanced mapper/default reflection deserialize", "CLR advanced mapper/default generated deserialize");
    PrintComparison(results, "CLR advanced preprocessor deserialize: generated vs reflection", "CLR advanced preprocess reflection deserialize", "CLR advanced preprocess generated deserialize");
    PrintComparison(results, "CLR advanced external-ref fixed deserialize: generated vs reflection", "CLR advanced external-ref fixed reflection deserialize", "CLR advanced external-ref fixed generated deserialize");
    PrintComparison(results, "CLR advanced external-ref fixed serialize: generated vs reflection", "CLR advanced external-ref fixed reflection serialize", "CLR advanced external-ref fixed generated serialize");
    PrintComparison(results, "CLR advanced polymorphic serialize: generated vs reflection", "CLR advanced polymorphic reflection serialize", "CLR advanced polymorphic generated serialize");
    PrintComparison(results, "CLR advanced polymorphic deserialize: generated vs reflection", "CLR advanced polymorphic reflection deserialize", "CLR advanced polymorphic generated deserialize");
    Console.WriteLine();
}

static void PrintComparison(Dictionary<string, (string Name, int Iterations, int PayloadSize, TimeSpan Elapsed, long AllocatedBytes, double ThroughputOpsPerSecond, double BytesPerOp)> results, string label, string baselineName, string contenderName)
{
    if (!results.TryGetValue(baselineName, out var baseline) || !results.TryGetValue(contenderName, out var contender))
    {
        Console.WriteLine($"  {label}: skipped (missing scenario)");
        return;
    }

    double throughputGainPct = PercentDelta(contender.ThroughputOpsPerSecond, baseline.ThroughputOpsPerSecond);
    double timeGainPct = PercentReduction(contender.Elapsed.TotalMilliseconds, baseline.Elapsed.TotalMilliseconds);
    double allocGainPct = PercentReduction(contender.BytesPerOp, baseline.BytesPerOp);

    Console.WriteLine($"  {label}");
    Console.WriteLine($"    Baseline: {baseline.Name}");
    Console.WriteLine($"    Contender: {contender.Name}");
    Console.WriteLine($"    Throughput delta: {throughputGainPct:+0.0;-0.0;0.0}%");
    Console.WriteLine($"    Time reduction: {timeGainPct:+0.0;-0.0;0.0}%");
    Console.WriteLine($"    Allocation reduction: {allocGainPct:+0.0;-0.0;0.0}%");
}

static double PercentDelta(double contender, double baseline)
{
    if (baseline == 0)
        return 0;

    return ((contender - baseline) / baseline) * 100.0;
}

static double PercentReduction(double contender, double baseline)
{
    if (baseline == 0)
        return 0;

    return ((baseline - contender) / baseline) * 100.0;
}

static void Warmup(int iterations, Action action)
{
    for (int i = 0; i < iterations; i++)
        action();
}

static async Task WarmupAsync(int iterations, Func<Task<int>> action)
{
    for (int i = 0; i < iterations; i++)
        _ = await action().ConfigureAwait(false);
}

static TimeSpan MeasureElapsed(int iterations, Action action)
{
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
        action();

    stopwatch.Stop();
    return stopwatch.Elapsed;
}

static async Task<TimeSpan> MeasureElapsedAsync(int iterations, Func<Task<int>> action)
{
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
        _ = await action().ConfigureAwait(false);

    stopwatch.Stop();
    return stopwatch.Elapsed;
}

static long MeasureAllocatedBytes(int iterations, Action action)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    long start = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < iterations; i++)
        action();
    long end = GC.GetAllocatedBytesForCurrentThread();
    return end - start;
}

static async Task<long> MeasureAllocatedBytesAsync(int iterations, Func<Task<int>> action)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    long start = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < iterations; i++)
        _ = await action().ConfigureAwait(false);
    long end = GC.GetAllocatedBytesForCurrentThread();
    return end - start;
}

static BJsonValue CreateSmallObject()
{
    return BJsonValue.Create(new BJsonObject
    {
        ["id"] = 42,
        ["name"] = "runner",
        ["flags"] = new BJsonArray { true, false, true, true },
        ["meta"] = new BJsonObject
        {
            ["hp"] = 99,
            ["speed"] = 1.5,
            ["zone"] = "tutorial"
        }
    });
}

static BJsonValue CreateRepeatedStringsObject()
{
    var items = new BJsonArray(256);
    for (int i = 0; i < 256; i++)
    {
        items.Add(new BJsonObject
        {
            ["kind"] = "entity",
            ["zone"] = "overworld",
            ["state"] = "idle",
            ["name"] = $"npc_{i % 16}",
            ["owner"] = "system"
        });
    }

    return BJsonValue.Create(new BJsonObject
    {
        ["items"] = items,
        ["kind"] = "entity",
        ["zone"] = "overworld",
        ["state"] = "idle"
    });
}

static BJsonValue CreatePackedNumberArray(int count)
{
    var array = new BJsonArray(count);
    for (int i = 0; i < count; i++)
        array.Add(i % 251);

    return BJsonValue.Create(array);
}

static BJsonValue CreateBinaryBlob(int size)
{
    byte[] bytes = new byte[size];
    for (int i = 0; i < bytes.Length; i++)
        bytes[i] = (byte)(i * 31 + 17);

    return BJsonValue.Create(new BJsonBinary(bytes));
}

static BJsonValue CreateWideRootObject(int propertyCount, int arrayLength)
{
    var obj = new BJsonObject(propertyCount);
    for (int i = 0; i < propertyCount; i++)
    {
        var values = new BJsonArray(arrayLength);
        for (int j = 0; j < arrayLength; j++)
            values.Add($"item_{i}_{j}");

        obj.Add($"section_{i}", values);
    }

    return BJsonValue.Create(obj);
}

static ReflectionProfile CreateReflectionProfile()
{
    var values = new List<int>(32);
    for (int i = 0; i < 32; i++)
        values.Add(i * 3);

    return new ReflectionProfile
    {
        Id = 42,
        Name = "reflection_profile",
        Score = 91.75,
        Active = true,
        CreatedAtUnix = 1735603200,
        Values = values
    };
}

static GeneratedProfile CreateGeneratedProfile()
{
    var values = new List<int>(32);
    for (int i = 0; i < 32; i++)
        values.Add(i * 3);

    return new GeneratedProfile
    {
        Id = 42,
        Name = "generated_profile",
        Score = 91.75,
        Active = true,
        CreatedAtUnix = 1735603200,
        Values = values
    };
}

static AttributedReflectionProfile CreateAttributedReflectionProfile()
{
    return new AttributedReflectionProfile
    {
        Id = 7,
        Tag = "alpha",
        AuditTrail = "evt",
        Mode = "custom"
    };
}

static AttributedGeneratedProfile CreateAttributedGeneratedProfile()
{
    return new AttributedGeneratedProfile
    {
        Id = 7,
        Tag = "alpha",
        AuditTrail = "evt",
        Mode = "custom"
    };
}

static BJsonValue CreateAttributedPayloadWithoutDefaults()
{
    var obj = new BJsonObject
    {
        ["Id"] = BJsonValue.Create(7),
        ["Tag"] = BJsonValue.Create("beta"),
        ["AuditTrail"] = BJsonValue.Create("evt")
    };

    return BJsonValue.Create(obj);
}

static AdvancedReflectionProfile CreateAdvancedReflectionProfile()
{
    return new AdvancedReflectionProfile
    {
        Id = 11,
        Tag = "delta",
        AuditTrail = "evt",
        Mode = "boost",
        Segment = "north"
    };
}

static AdvancedGeneratedProfile CreateAdvancedGeneratedProfile()
{
    return new AdvancedGeneratedProfile
    {
        Id = 11,
        Tag = "delta",
        AuditTrail = "evt",
        Mode = "boost",
        Segment = "north"
    };
}

static ReflectionActorBase CreateReflectionPolymorphicActor()
{
    return new ReflectionMage
    {
        Id = 5,
        Alias = "rune",
        Mana = 120
    };
}

static GeneratedActorBase CreateGeneratedPolymorphicActor()
{
    return new GeneratedMage
    {
        Id = 5,
        Alias = "rune",
        Mana = 120
    };
}

static BJsonValue CreateAdvancedExternalStateDocument()
{
    return BJsonValue.Create(new BJsonObject
    {
        ["Power"] = BJsonValue.Create(17),
        ["Flags"] = BJsonValue.Create("steady")
    });
}

static BJsonValue CreateAdvancedPreprocessorPayload(string externalStatePath)
{
    return BJsonValue.Create(new BJsonObject
    {
        ["$branches"] = BJsonValue.Create(new BJsonArray
        {
            BJsonValue.Create(new BJsonObject
            {
                ["$if"] = BJsonValue.Create(new BJsonObject
                {
                    ["$var"] = BJsonValue.Create("Platform"),
                    ["$eq"] = BJsonValue.Create("Desktop")
                }),
                ["$then"] = BJsonValue.Create(new BJsonObject
                {
                    ["PrimaryColor"] = BJsonValue.Create("#22CC88"),
                    ["Display"] = BJsonValue.Create(new BJsonObject
                    {
                        ["$ref"] = BJsonValue.Create("primaryColor")
                    }),
                    ["Mode"] = BJsonValue.Create("{{ModeToken}}"),
                    ["State"] = BJsonValue.Create(externalStatePath),
                    ["AuditTrail"] = BJsonValue.Create("evt")
                })
            }),
            BJsonValue.Create(new BJsonObject
            {
                ["$else"] = BJsonValue.Create(new BJsonObject
                {
                    ["PrimaryColor"] = BJsonValue.Create("#999999"),
                    ["Display"] = BJsonValue.Create("fallback"),
                    ["Mode"] = BJsonValue.Create("safe"),
                    ["AuditTrail"] = BJsonValue.Create("evt")
                })
            })
        })
    });
}

static BJsonValue CreateAdvancedExternalFixedPathPayload()
{
    return BJsonValue.Create(new BJsonObject
    {
        ["State"] = BJsonValue.Create("ignored-on-fixed-path")
    });
}

static class BenchmarkStore
{
    public static readonly Dictionary<string, (string Name, int Iterations, int PayloadSize, TimeSpan Elapsed, long AllocatedBytes, double ThroughputOpsPerSecond, double BytesPerOp)> Results =
        new Dictionary<string, (string Name, int Iterations, int PayloadSize, TimeSpan Elapsed, long AllocatedBytes, double ThroughputOpsPerSecond, double BytesPerOp)>(StringComparer.Ordinal);
}

sealed class ReflectionProfile
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public double Score { get; set; }

    public bool Active { get; set; }

    public long CreatedAtUnix { get; set; }

    public List<int> Values { get; set; } = new List<int>();
}

[BJsonSerializable]
sealed class GeneratedProfile
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public double Score { get; set; }

    public bool Active { get; set; }

    public long CreatedAtUnix { get; set; }

    public List<int> Values { get; set; } = new List<int>();
}

sealed class AttributedReflectionProfile
{
    public int Id { get; set; }

    [BJsonValueMapper(nameof(MapTag))]
    public string Tag { get; set; } = string.Empty;

    [BJsonIgnoreWhen(nameof(NeverIgnore))]
    public string AuditTrail { get; set; } = string.Empty;

    [BJsonDefaultProvider(nameof(CreateDefaultMode))]
    public string Mode { get; set; } = string.Empty;

    internal static bool NeverIgnore(object? value, string propertyName, IComparable? version)
    {
        return false;
    }

    internal static BJsonValue MapTag(BJsonValue value, string propertyName, IComparable? version, bool isReading)
    {
        if (!value.TryGetString(out var text))
            return value;

        return BJsonValue.Create(isReading ? text.ToLowerInvariant() : text.ToUpperInvariant());
    }

    internal static string CreateDefaultMode()
    {
        return "standard";
    }
}

[BJsonSerializable]
sealed class AttributedGeneratedProfile
{
    public int Id { get; set; }

    [BJsonValueMapper(nameof(MapTag))]
    public string Tag { get; set; } = string.Empty;

    [BJsonIgnoreWhen(nameof(NeverIgnore))]
    public string AuditTrail { get; set; } = string.Empty;

    [BJsonDefaultProvider(nameof(CreateDefaultMode))]
    public string Mode { get; set; } = string.Empty;

    internal static bool NeverIgnore(object? value, string propertyName, IComparable? version)
    {
        return false;
    }

    internal static BJsonValue MapTag(BJsonValue value, string propertyName, IComparable? version, bool isReading)
    {
        if (!value.TryGetString(out var text))
            return value;

        return BJsonValue.Create(isReading ? text.ToLowerInvariant() : text.ToUpperInvariant());
    }

    internal static string CreateDefaultMode()
    {
        return "standard";
    }
}

[BJsonPreprocessor]
sealed class AdvancedReflectionConfig
{
    [BJsonAnchor("primaryColor")]
    public string PrimaryColor { get; set; } = string.Empty;

    public string Display { get; set; } = string.Empty;

    [BJsonValueMapper(nameof(MapMode))]
    public string Mode { get; set; } = string.Empty;

    [BJsonIgnoreWhen(nameof(ShouldIgnoreAudit))]
    public string AuditTrail { get; set; } = string.Empty;

    [BJsonDefaultProvider(nameof(CreateDefaultSegment))]
    public string Segment { get; set; } = string.Empty;

    [BJsonExternalRef(Optional = true)]
    public AdvancedExternalState? State { get; set; }

    internal static bool ShouldIgnoreAudit(object? value, string propertyName, IComparable? version)
    {
        return false;
    }

    internal static BJsonValue MapMode(BJsonValue value, string propertyName, IComparable? version, bool isReading)
    {
        if (!value.TryGetString(out var text))
            return value;

        return BJsonValue.Create(isReading ? text.ToLowerInvariant() : text.ToUpperInvariant());
    }

    internal static string CreateDefaultSegment()
    {
        return "core";
    }
}

[BJsonSerializable]
[BJsonPreprocessor]
sealed class AdvancedGeneratedConfig
{
    [BJsonAnchor("primaryColor")]
    public string PrimaryColor { get; set; } = string.Empty;

    public string Display { get; set; } = string.Empty;

    [BJsonValueMapper(nameof(MapMode))]
    public string Mode { get; set; } = string.Empty;

    [BJsonIgnoreWhen(nameof(ShouldIgnoreAudit))]
    public string AuditTrail { get; set; } = string.Empty;

    [BJsonDefaultProvider(nameof(CreateDefaultSegment))]
    public string Segment { get; set; } = string.Empty;

    [BJsonExternalRef(Optional = true)]
    public AdvancedExternalState? State { get; set; }

    internal static bool ShouldIgnoreAudit(object? value, string propertyName, IComparable? version)
    {
        return false;
    }

    internal static BJsonValue MapMode(BJsonValue value, string propertyName, IComparable? version, bool isReading)
    {
        if (!value.TryGetString(out var text))
            return value;

        return BJsonValue.Create(isReading ? text.ToLowerInvariant() : text.ToUpperInvariant());
    }

    internal static string CreateDefaultSegment()
    {
        return "core";
    }
}

sealed class AdvancedExternalState
{
    public int Power { get; set; }

    public string Flags { get; set; } = string.Empty;
}

[BJsonPreprocessor]
sealed class AdvancedReflectionExternalRefConfig
{
    [BJsonExternalRef(FixedPath = "state-fixed.bjson")]
    public AdvancedExternalState? State { get; set; }
}

[BJsonSerializable]
[BJsonPreprocessor]
sealed class AdvancedGeneratedExternalRefConfig
{
    [BJsonExternalRef(FixedPath = "state-fixed.bjson")]
    public AdvancedExternalState? State { get; set; }
}

sealed class AdvancedReflectionProfile
{
    public int Id { get; set; }

    [BJsonValueMapper(nameof(MapTag))]
    public string Tag { get; set; } = string.Empty;

    [BJsonIgnoreWhen(nameof(NeverIgnore))]
    public string AuditTrail { get; set; } = string.Empty;

    [BJsonDefaultProvider(nameof(CreateDefaultMode))]
    public string Mode { get; set; } = string.Empty;

    [BJsonDefaultValue("core")]
    public string Segment { get; set; } = string.Empty;

    internal static bool NeverIgnore(object? value, string propertyName, IComparable? version)
    {
        return false;
    }

    internal static BJsonValue MapTag(BJsonValue value, string propertyName, IComparable? version, bool isReading)
    {
        if (!value.TryGetString(out var text))
            return value;

        return BJsonValue.Create(isReading ? text.ToLowerInvariant() : text.ToUpperInvariant());
    }

    internal static string CreateDefaultMode()
    {
        return "standard";
    }
}

[BJsonSerializable]
sealed class AdvancedGeneratedProfile
{
    public int Id { get; set; }

    [BJsonValueMapper(nameof(MapTag))]
    public string Tag { get; set; } = string.Empty;

    [BJsonIgnoreWhen(nameof(NeverIgnore))]
    public string AuditTrail { get; set; } = string.Empty;

    [BJsonDefaultProvider(nameof(CreateDefaultMode))]
    public string Mode { get; set; } = string.Empty;

    [BJsonDefaultValue("core")]
    public string Segment { get; set; } = string.Empty;

    internal static bool NeverIgnore(object? value, string propertyName, IComparable? version)
    {
        return false;
    }

    internal static BJsonValue MapTag(BJsonValue value, string propertyName, IComparable? version, bool isReading)
    {
        if (!value.TryGetString(out var text))
            return value;

        return BJsonValue.Create(isReading ? text.ToLowerInvariant() : text.ToUpperInvariant());
    }

    internal static string CreateDefaultMode()
    {
        return "standard";
    }
}

[BJsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[BJsonDerivedType(typeof(ReflectionMage), TypeDiscriminator = "mage")]
abstract class ReflectionActorBase
{
    public int Id { get; set; }

    public string Alias { get; set; } = string.Empty;
}

sealed class ReflectionMage : ReflectionActorBase
{
    public int Mana { get; set; }
}

[BJsonSerializable]
[BJsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[BJsonDerivedType(typeof(GeneratedMage), TypeDiscriminator = "mage")]
abstract class GeneratedActorBase
{
    public int Id { get; set; }

    public string Alias { get; set; } = string.Empty;
}

[BJsonSerializable]
sealed class GeneratedMage : GeneratedActorBase
{
    public int Mana { get; set; }
}
