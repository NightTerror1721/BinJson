#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Krampus.BinJson.SourceGenerators.Models
{
    /// <summary>
    /// Result of type analysis including model and diagnostics
    /// </summary>
    internal sealed class AnalysisResult
    {
        public GeneratedTypeModel? Model { get; }
        public List<Diagnostic> Diagnostics { get; }

        public AnalysisResult(GeneratedTypeModel? model, List<Diagnostic> diagnostics)
        {
            Model = model;
            Diagnostics = diagnostics;
        }

        public bool HasModel => Model != null;
    }
}
