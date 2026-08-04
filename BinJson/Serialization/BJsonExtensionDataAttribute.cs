#nullable enable

using System;

namespace Krampus.BinJson.Serialization
{
    /// <summary>
    /// Captures unknown JSON object members into a dictionary during deserialization and writes them back during serialization.
    /// <para>
    /// The target member must be compatible with <c>IDictionary&lt;string, BJsonValue&gt;</c>.
    /// Only one extension-data member is allowed per type.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// [BJsonSerializable]
    /// public sealed class ConfigDocument
    /// {
    ///     public string Name { get; set; } = string.Empty;
    ///
    ///     [BJsonExtensionData]
    ///     public Dictionary&lt;string, BJsonValue&gt;? ExtraData { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BJsonExtensionDataAttribute : Attribute
    {
    }
}