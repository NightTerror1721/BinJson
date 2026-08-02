using System.Diagnostics;
using Krampus.BinJson;

const int iterations = 10000;

var reflectionModel = new PerformanceComparisonSamples.ReflectionProfile
{
    Id = 7,
    Name = "reflection",
    Score = 123.45
};

var generatedModel = new PerformanceComparisonSamples.GeneratedProfile
{
    Id = 7,
    Name = "generated",
    Score = 123.45
};

Warmup(reflectionModel, generatedModel);

var reflectionElapsed = Measure(iterations, () => BJson.Serialize(reflectionModel));
var generatedElapsed = Measure(iterations, () => BJson.Serialize(generatedModel));

Console.WriteLine($"Reflection model: {reflectionElapsed.TotalMilliseconds:N2} ms for {iterations} serializations");
Console.WriteLine($"Generated model: {generatedElapsed.TotalMilliseconds:N2} ms for {iterations} serializations");

static void Warmup(PerformanceComparisonSamples.ReflectionProfile reflectionModel, PerformanceComparisonSamples.GeneratedProfile generatedModel)
{
    _ = BJson.Serialize(reflectionModel);
    _ = BJson.Serialize(generatedModel);
}

static TimeSpan Measure(int iterations, Action action)
{
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
        action();

    stopwatch.Stop();
    return stopwatch.Elapsed;
}

namespace PerformanceComparisonSamples
{
    using Krampus.BinJson.Serialization;

    public sealed class ReflectionProfile
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public double Score { get; set; }
    }

    [BJsonSerializable]
    public sealed class GeneratedProfile
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public double Score { get; set; }
    }
}
