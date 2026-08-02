#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Enables BJson DOM pre-processing for a serializable type before typed deserialization occurs.
    /// <para>
    /// When present, the BJson engine runs the pre-processor pipeline on the raw DOM node before
    /// mapping it to the type's members. This enables:
    /// <list type="bullet">
    ///   <item><description>Conditional blocks: <c>$if / $then / $elif / $else</c></description></item>
    ///   <item><description>Anchor resolution: <c>{ "$ref": "anchorName" }</c> (see <see cref="BJsonAnchorAttribute"/>)</description></item>
    ///   <item><description>Variable substitution</description></item>
    ///   <item><description>External file inclusion (see <see cref="BJsonExternalRefAttribute"/>)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// When <see cref="PreprocessorType"/> is <c>null</c>, the built-in default pre-processor is used.
    /// To use a custom pre-processor, set this property to a type that implements
    /// <see cref="IBJsonPreprocessor"/> and has a public parameterless constructor.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// // Use the built-in pre-processor
    /// [BJsonSerializable]
    /// [BJsonPreprocessor]
    /// public class Config { ... }
    ///
    /// // Use a custom pre-processor
    /// [BJsonSerializable]
    /// [BJsonPreprocessor(PreprocessorType = typeof(MyCustomPreprocessor))]
    /// public class Config { ... }
    /// </code>
    ///
    /// Conditional block syntax in JSON documents:
    /// <code>
    /// {
    ///   "$if":   { "$var": "Platform", "$eq": "PC" },
    ///   "$then": { "GraphicsQuality": "Ultra" },
    ///   "$elif": { "$var": "Platform", "$eq": "Mobile" },
    ///   "$then": { "GraphicsQuality": "Low" },
    ///   "$else": { "GraphicsQuality": "Medium" }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class BJsonPreprocessorAttribute : Attribute
    {
        /// <summary>
        /// Custom pre-processor type. Must implement <see cref="IBJsonPreprocessor"/> and have
        /// a public parameterless constructor.
        /// When <c>null</c>, the built-in default pre-processor is used.
        /// </summary>
        public Type? PreprocessorType { get; set; }
    }
}
